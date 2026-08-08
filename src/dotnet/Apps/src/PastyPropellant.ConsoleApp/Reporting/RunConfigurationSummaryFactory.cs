using System.Globalization;
using ParametricCombustionModel.Computation.Models.KnownParams;
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
    private static List<RunConfigurationEntry> BuildModelEntries(ModelConfiguration model)
    {
        var entries = new List<RunConfigurationEntry>
        {
            new("Metal melting temperature, K", Format(model.MetalMeltingTemperatureKelvins)),
            // A bracket, not a handbook constant: the solve fails when no root lies inside it, so it acts as
            // a soft constraint and a reader who does not know it cannot tell a converged surface
            // temperature from one the bracket imposed.
            new("Surface-temperature search bracket, K",
                $"{Format(model.MinSurfaceTemperatureKelvins)} .. {Format(model.MaxSurfaceTemperatureKelvins)}"),
            new("Skeleton conduction contact factor",
                Math.Abs(model.SkeletonContactFactor - 1.0) < double.Epsilon
                    ? "1 (no correction — bulk Fourier conduction)"
                    : $"{Format(model.SkeletonContactFactor)} " +
                      $"(contact-area fraction {Format(1.0 / model.SkeletonContactFactor)}; fixed calibration, not fitted)")
        };

        // Which closure produced f_s has to be stated, not inferred. f_s weighs the metal and
        // skeleton-flame fluxes against the out-skeleton flux, so two runs under different closures are
        // two different models; a report that named only the constants would leave the reader unable to
        // tell them apart.
        var coverage = model.SkeletonSurfaceFraction;
        if (coverage.Mode == SkeletonSurfaceFractionMode.Polynomial)
        {
            entries.Add(new RunConfigurationEntry(
                "Skeleton surface fraction",
                "per-propellant polynomial fit from the propellants file (historical closure)"));

            return entries;
        }

        if (coverage.Mode == SkeletonSurfaceFractionMode.KineticCoverage)
        {
            var kinetic = coverage.Kinetic;

            entries.Add(new RunConfigurationEntry(
                "Skeleton surface fraction",
                "kinetic coverage: f_s = 1/(1 + a₀ + a₁·w_fine·(p/p_ref)^m) — accumulation against " +
                "burnout of the binder residue; independent of the layer thickness"));
            entries.Add(new RunConfigurationEntry(
                "    burnout channels",
                $"a₀ = {Format(kinetic.BinderChannel)} (binder), " +
                $"a₁ = {Format(kinetic.FineOxidiserChannel)} (fine oxidiser), " +
                $"m = {Format(kinetic.PressureOrder)} at p_ref = " +
                $"{Format(kinetic.ReferencePressurePascals / 1e6)} MPa"));
            // The calibration set is part of the result, not a footnote: these constants were fitted on
            // Bas_2/3/4 and overshoot Bas_0 and Bas_1 by up to 137 %, so a report that printed the
            // numbers without their provenance would invite exactly the quotation they do not support.
            entries.Add(new RunConfigurationEntry(
                "    calibration",
                "3 shared constants fitted on Bas_2/Bas_3/Bas_4 (one binder, one additive, AP dispersity " +
                "varied); 4.7–8.6 % RMS in-sample, 12.7–20.9 % leaving one composition out. " +
                "NOT calibrated for Bas_0 or Bas_1 — different binder"));

            return entries;
        }

        entries.Add(new RunConfigurationEntry(
            "Skeleton surface fraction",
            "equilibrium carbon: f_s = φ_Al + φ_C(s)(T_pore, p) by Delesse, " +
            "T_pore = (T_s + T_m)/2 — no fitted coefficients, no agglomeration data"));
        entries.Add(new RunConfigurationEntry("    coverage table", coverage.EquilibriumTableFile));

        return entries;
    }

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
