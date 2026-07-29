using System.Globalization;
using ParametricCombustionModel.Optimization.Settings;
using ParametricCombustionModel.ReportMaking.Models;
using PastyPropellant.ConsoleApp.Configuration;
using PastyPropellant.ConsoleApp.Io;

namespace PastyPropellant.ConsoleApp.Reporting;

/// <summary>
/// Formats a <see cref="ResolvedRunRecord"/> into the sections the PDF report prints.
///
/// <para>This is the human-readable projection of the same object the JSON sidecar serialises, so the
/// two halves of a run's provenance cannot disagree: both are produced from one record, built once, by
/// the runner that configured the run.</para>
///
/// <para>Where a value is strategy-dependent this shows only the branch that was actually taken —
/// printing L-SHADE's budget next to a jDE run would invite exactly the misreading the record exists to
/// prevent.</para>
/// </summary>
public static class RunConfigurationSummaryFactory
{
    /// <summary>Builds the report sections describing <paramref name="record"/>.</summary>
    public static IReadOnlyList<RunConfigurationSection> Create(ResolvedRunRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        var configuration = record.Configuration;
        var effective = record.Effective;
        var deConfiguration = configuration.DifferentialEvolution;

        return
        [
            new RunConfigurationSection("Provenance",
            [
                new RunConfigurationEntry("Mode", record.Mode),
                new RunConfigurationEntry("Configuration source", record.ConfigurationSource),
                new RunConfigurationEntry("Working directory", record.WorkingDirectory),
                new RunConfigurationEntry("Input propellants file", configuration.InputFileName),
                new RunConfigurationEntry("Resolved sidecar", ResolvedRunRecordService.FileName),
                new RunConfigurationEntry("Recorded at (UTC)",
                    record.GeneratedAtUtc.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture))
            ]),

            // Printed before the search settings on purpose: these constants change every computed number
            // in the report that follows, and a reader who does not know them cannot interpret any of it.
            new RunConfigurationSection("Model constants", BuildModelEntries(configuration.Model)),

            new RunConfigurationSection("Search", BuildSearchEntries(deConfiguration, effective)),

            new RunConfigurationSection("Nelder-Mead refinement", BuildNelderMeadEntries(configuration.NelderMead)),

            new RunConfigurationSection("Penalty thresholds", BuildPenaltyEntries(configuration.Penalties)),

            new RunConfigurationSection("Parameter bounds (18 base parameters)",
                BuildBoundsEntries(configuration.Bounds))
        ];
    }

    /// <summary>
    /// The physical constants of the model. The contact factor is spelled out as a contact-area fraction
    /// as well as a divisor, because the divisor alone reads as an arbitrary number while the fraction is
    /// the quantity a reader can weigh against the literature.
    /// </summary>
    private static List<RunConfigurationEntry> BuildModelEntries(ModelConfiguration model) =>
    [
        new("Metal melting temperature, K", Format(model.MetalMeltingTemperatureKelvins)),
        new("Skeleton conduction contact factor",
            Math.Abs(model.SkeletonContactFactor - 1.0) < double.Epsilon
                ? "1 (no correction — bulk Fourier conduction)"
                : $"{Format(model.SkeletonContactFactor)} " +
                  $"(contact-area fraction {Format(1.0 / model.SkeletonContactFactor)}; fixed calibration, not fitted)")
    ];

    private static List<RunConfigurationEntry> BuildSearchEntries(
        DifferentialEvolutionConfiguration deConfiguration,
        EffectiveRunValues effective)
    {
        var entries = new List<RunConfigurationEntry>
        {
            new("Strategy", deConfiguration.Strategy.ToString()),
            new("Parameter vector dimension", Format(effective.Dimensions)),
            new("Population size (effective)", Format(effective.PopulationSize)),
            new("Worker processors (effective)",
                $"{Format(effective.ProcessorsCount)} of {Format(effective.MachineProcessorCount)} machine cores"),
            // Printed next to the worker count on purpose: a seed reproduces a run only at the worker count
            // it ran on, so the two are one fact and quoting either alone would overstate what is repeatable.
            new("RNG seed", effective.Seed is { } seed
                ? $"{Format(seed)} (reproducible at {Format(effective.ProcessorsCount)} workers)"
                : "unseeded (run not exactly reproducible)"),
            new("Termination", effective.TerminationStrategy)
        };

        // F/CR are only consumed by the Classic and jDE strategies; the adaptive variants self-tune them,
        // so showing the configured value for those would misrepresent what the run did.
        if (deConfiguration.Strategy is DifferentialEvolutionStrategy.Classic or DifferentialEvolutionStrategy.Jde)
        {
            entries.Add(new RunConfigurationEntry("Mutation force F", Format(deConfiguration.MutationForce)));
            entries.Add(new RunConfigurationEntry("Crossover probability CR", Format(deConfiguration.CrossoverProbability)));
        }
        else
        {
            entries.Add(new RunConfigurationEntry("Mutation force F / crossover CR", "self-tuned by the strategy"));
        }

        if (effective.MaxEvaluationNumber.HasValue)
            entries.Add(new RunConfigurationEntry("Evaluation budget",
                effective.MaxEvaluationNumber.Value.ToString("N0", CultureInfo.InvariantCulture)));

        if (effective is { PBestRate: { } pBest, ArchiveSizeRate: { } archive, MemorySize: { } memory })
            entries.Add(new RunConfigurationEntry("L-SHADE control",
                $"p-best {Format(pBest)}, archive rate {Format(archive)}, memory {Format(memory)}"));

        return entries;
    }

    private static List<RunConfigurationEntry> BuildNelderMeadEntries(NelderMeadConfiguration nelderMead)
    {
        if (!nelderMead.Enabled)
            return [new RunConfigurationEntry("Enabled", "no (plain differential evolution)")];

        var stages = new List<string>();
        if (nelderMead.MemeticInLoop)
            stages.Add($"memetic every {Format(nelderMead.EveryNGenerations)} generations " +
                       $"({nelderMead.MemeticMaxEvaluationsPerCall:N0} evals/call)");
        if (nelderMead.FinalPolish)
            stages.Add($"final polish ({nelderMead.FinalPolishMaxEvaluations:N0} evals)");

        return
        [
            new RunConfigurationEntry("Enabled", "yes"),
            new RunConfigurationEntry("Stages", string.Join(" + ", stages)),
            new RunConfigurationEntry("Adaptive coefficients", nelderMead.AdaptiveCoefficients ? "yes" : "no"),
            new RunConfigurationEntry("Domain tolerance", Format(nelderMead.DomainTolerance)),
            new RunConfigurationEntry("Function tolerance", Format(nelderMead.FunctionTolerance)),
            new RunConfigurationEntry("Restarts", Format(nelderMead.Restarts))
        ];
    }

    private static List<RunConfigurationEntry> BuildPenaltyEntries(PenaltyConfiguration penalties) =>
    [
        new("Penalty rate", Format(penalties.PenaltyRate)),
        new("Pocket heat-flux ratio threshold", Format(penalties.HeatFluxRatioThreshold)),
        new("Pore diameter threshold", Format(penalties.PoreDiameterThreshold)),
        new("Large oxidiser particle size threshold", Format(penalties.LargeOxidizerParticleSizeThreshold)),
        new("Max inter-pocket kinetic-flame heat flux, W/m2",
            Format(penalties.MaxInterPocketKineticFlameHeatFluxWattsPerSquareMeter)),
        new("Max skeleton kinetic-flame heat flux, W/m2",
            Format(penalties.MaxSkeletonKineticFlameHeatFluxWattsPerSquareMeter)),
        new("Max out-skeleton kinetic-flame heat flux, W/m2",
            Format(penalties.MaxOutSkeletonKineticFlameHeatFluxWattsPerSquareMeter))
    ];

    private static List<RunConfigurationEntry> BuildBoundsEntries(BoundsConfiguration bounds)
    {
        var lower = bounds.ToBaseLowerBound();
        var upper = bounds.ToBaseUpperBound();

        var entries = new List<RunConfigurationEntry>(BoundsProvider.BaseVectorLength);
        for (int i = 0; i < BoundsProvider.BaseVectorLength; i++)
            entries.Add(new RunConfigurationEntry(
                BoundsProvider.BaseParameterNames[i],
                $"[{Format(lower[i])}, {Format(upper[i])}]"));

        return entries;
    }

    private static string Format(double value) => value.ToString("G6", CultureInfo.InvariantCulture);

    private static string Format(int value) => value.ToString(CultureInfo.InvariantCulture);
}
