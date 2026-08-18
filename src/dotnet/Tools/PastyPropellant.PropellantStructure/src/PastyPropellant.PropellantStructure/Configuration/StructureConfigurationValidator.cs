using PastyPropellant.PropellantStructure.Sampling;
using UnitsNet;

namespace PastyPropellant.PropellantStructure.Configuration;

/// <summary>
/// Rejects a configuration that cannot start a run, before anything is built.
/// </summary>
/// <remarks>
/// Two rules shape everything here. The check is <b>complete</b> — every field is
/// examined, including branches this particular run will never take, so a broken
/// setting cannot lie dormant until someone switches to it. And it <b>substitutes
/// nothing</b>: an invalid value is an exception, never a quietly applied default.
/// <para>
/// What it does not check is the original's own normalisations. <c>KXX &lt;= 1</c>
/// is legal input here and becomes one pass inside the kernel, because that
/// substitution belongs to the model being reproduced rather than to this port.
/// </para>
/// </remarks>
public static class StructureConfigurationValidator
{
    /// <summary>
    /// Throws <see cref="StructureConfigurationException"/> listing every problem
    /// found; returns silently when there are none.
    /// </summary>
    public static void Validate(StructureRunConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var problems = new List<string>();

        CheckRecipe(configuration, problems);
        CheckFractions(configuration, problems);
        CheckSampleSize(configuration, problems);
        CheckGrids(configuration, problems);
        CheckCriteria(configuration.Criteria, problems);
        CheckCoefficients(configuration.Coefficients, problems);
        CheckEnums(configuration, problems);

        if (problems.Count > 0)
        {
            throw new StructureConfigurationException(problems);
        }
    }

    private static void CheckRecipe(StructureRunConfiguration configuration, List<string> problems)
    {
        RequirePositive(configuration.OxidiserDensity, nameof(configuration.OxidiserDensity), problems);
        RequirePositive(configuration.BinderMetalDensity, nameof(configuration.BinderMetalDensity), problems);

        RequireShare(
            configuration.OxidiserMassFraction,
            nameof(configuration.OxidiserMassFraction),
            problems,
            allowZero: false);
        RequireShare(
            configuration.MetalMassFraction,
            nameof(configuration.MetalMassFraction),
            problems,
            allowZero: true);
        RequireShare(
            configuration.AgglomeratedOxideShare,
            nameof(configuration.AgglomeratedOxideShare),
            problems,
            allowZero: true);

        var solids = configuration.OxidiserMassFraction + configuration.MetalMassFraction;
        if (double.IsFinite(solids) && solids >= 1.0)
        {
            problems.Add(
                $"{nameof(configuration.OxidiserMassFraction)} + {nameof(configuration.MetalMassFraction)} "
                + $"= {solids:R} leaves no binder. The remainder of the propellant mass is the binder, so "
                + "the two solid shares must come to less than one.");
        }
    }

    private static void CheckFractions(StructureRunConfiguration configuration, List<string> problems)
    {
        if (configuration.Fractions.Count == 0)
        {
            problems.Add($"{nameof(configuration.Fractions)} is empty: a run needs at least one oxidiser fraction.");
            return;
        }

        for (var index = 0; index < configuration.Fractions.Count; index++)
        {
            var fraction = configuration.Fractions[index];
            var label = $"{nameof(configuration.Fractions)}[{index}]";

            if (!double.IsFinite(fraction.MassFraction) || fraction.MassFraction <= 0.0)
            {
                problems.Add($"{label}.{nameof(fraction.MassFraction)} = {fraction.MassFraction:R}: must be above zero.");
            }

            RequirePositive(fraction.MinSize, $"{label}.{nameof(fraction.MinSize)}", problems);
            RequirePositive(fraction.MaxSize, $"{label}.{nameof(fraction.MaxSize)}", problems);

            if (fraction.MinSize >= fraction.MaxSize)
            {
                problems.Add(
                    $"{label}: {nameof(fraction.MinSize)} = {Micrometres(fraction.MinSize)} is not below "
                    + $"{nameof(fraction.MaxSize)} = {Micrometres(fraction.MaxSize)}. The size inversion draws "
                    + "from the open interval between the two bounds, so an inverted or empty one has no draw.");
            }
        }

        // The sum is deliberately NOT checked here: four archived runs come to 1.1
        // and 0.9885, the model normalises internally, and rejecting them would
        // shrink the reference. The deviation is reported as a warning on the
        // resolved record instead - see ResolvedRunRecord.
        if (configuration.Fractions.All(fraction => !fraction.FormsPockets))
        {
            problems.Add(
                $"Every entry of {nameof(configuration.Fractions)} has "
                + $"{nameof(OxidiserFraction.FormsPockets)} = false, so no fraction can form a pocket and the "
                + "run has nothing to measure.");
        }
    }

    private static void CheckSampleSize(StructureRunConfiguration configuration, List<string> problems)
    {
        if (configuration.BaseParticles <= 0)
        {
            problems.Add($"{nameof(configuration.BaseParticles)} = {configuration.BaseParticles}: must be above zero.");
        }

        // Cycles <= 1 is legal - it is what every archived .dat carries, and the
        // kernel turns it into one pass. Negative is not: no original input ever
        // held one, and it would be a request nothing in the model answers.
        if (configuration.Cycles < 0)
        {
            problems.Add(
                $"{nameof(configuration.Cycles)} = {configuration.Cycles}: a negative pass count has no meaning. "
                + "Zero and one are both legal and both mean a single pass (the original's lines 141-143).");
        }
    }

    private static void CheckGrids(StructureRunConfiguration configuration, List<string> problems)
    {
        RequirePositive(configuration.MinimumParticleSize, nameof(configuration.MinimumParticleSize), problems);
        RequirePositive(configuration.ParticleHistogramStep, nameof(configuration.ParticleHistogramStep), problems);
        RequirePositive(configuration.PocketHistogramStep, nameof(configuration.PocketHistogramStep), problems);

        if (configuration.MinimumParticleSize > configuration.ParticleHistogramStep)
        {
            problems.Add(
                $"{nameof(configuration.MinimumParticleSize)} = "
                + $"{Micrometres(configuration.MinimumParticleSize)} is above "
                + $"{nameof(configuration.ParticleHistogramStep)} = "
                + $"{Micrometres(configuration.ParticleHistogramStep)}. The share of particles below the cutoff "
                + "is held per histogram cell, so the cutoff is only ever resolved to one cell; above one cell "
                + "whole cells sit entirely below it. The reference has all three sizes equal at 10 µm.");
        }
    }

    private static void CheckCriteria(GeometricCriteria criteria, List<string> problems)
    {
        RequirePositive(criteria.Ak1, nameof(criteria.Ak1), problems);
        RequirePositive(criteria.Ak2, nameof(criteria.Ak2), problems);
        RequirePositive(criteria.Ak3, nameof(criteria.Ak3), problems);
        RequirePositive(criteria.Ak4, nameof(criteria.Ak4), problems);

        if (double.IsFinite(criteria.Ak1) && double.IsFinite(criteria.Ak2) && criteria.Ak1 >= criteria.Ak2)
        {
            problems.Add(
                $"{nameof(criteria.Ak1)} = {criteria.Ak1:R} is not below {nameof(criteria.Ak2)} = {criteria.Ak2:R}: "
                + "the two are the ends of the size-ratio window a neighbour must fall in, so an inverted window "
                + "admits nothing.");
        }

        if (double.IsFinite(criteria.Ak3) && double.IsFinite(criteria.Ak4) && criteria.Ak3 >= criteria.Ak4)
        {
            problems.Add(
                $"{nameof(criteria.Ak3)} = {criteria.Ak3:R} is not below {nameof(criteria.Ak4)} = {criteria.Ak4:R}: "
                + "a gap wide enough to be discarded must be wider than one counted as a bridge, or every pocket "
                + "is thrown away before it is classified.");
        }
    }

    private static void CheckCoefficients(ModelCoefficients coefficients, List<string> problems)
    {
        RequirePositive(coefficients.SurroundingVolumeShare, nameof(coefficients.SurroundingVolumeShare), problems);
        RequirePositive(coefficients.PocketInPocket, nameof(coefficients.PocketInPocket), problems);
        RequirePositive(coefficients.PocketInBridge, nameof(coefficients.PocketInBridge), problems);
        RequirePositive(coefficients.Accuracy, nameof(coefficients.Accuracy), problems);

        RequireShare(
            coefficients.StatisticalSignificance,
            nameof(coefficients.StatisticalSignificance),
            problems,
            allowZero: true);
        RequireShare(
            coefficients.HomogenisedOxidiser,
            nameof(coefficients.HomogenisedOxidiser),
            problems,
            allowZero: true);

        RequirePositive(coefficients.PocketBridgeRatioMin, nameof(coefficients.PocketBridgeRatioMin), problems);
        RequirePositive(coefficients.PocketBridgeRatioMax, nameof(coefficients.PocketBridgeRatioMax), problems);

        if (double.IsFinite(coefficients.PocketBridgeRatioMin)
            && double.IsFinite(coefficients.PocketBridgeRatioMax)
            && coefficients.PocketBridgeRatioMin >= coefficients.PocketBridgeRatioMax)
        {
            problems.Add(
                $"{nameof(coefficients.PocketBridgeRatioMin)} = {coefficients.PocketBridgeRatioMin:R} is not below "
                + $"{nameof(coefficients.PocketBridgeRatioMax)} = {coefficients.PocketBridgeRatioMax:R}: the two "
                + "bound the same ratio, so an inverted window accepts no realisation.");
        }
    }

    private static void CheckEnums(StructureRunConfiguration configuration, List<string> problems)
    {
        if (!Enum.IsDefined(configuration.Generator))
        {
            problems.Add(
                $"{nameof(configuration.Generator)} = {(int)configuration.Generator} is not one of "
                + $"{string.Join(", ", Enum.GetNames<GeneratorSelection>())}.");
        }

        if (!Enum.IsDefined(configuration.Law))
        {
            problems.Add(
                $"{nameof(configuration.Law)} = {(int)configuration.Law} is not one of "
                + $"{string.Join(", ", Enum.GetNames<SizeDistributionLaw>())}.");
        }

        // CalculationVariant is NOT range-checked: the original tests ivar for zero
        // and treats every other value alike (line 549), so there is no invalid
        // value to reject - only an unverified one, which the resolved record says.

        // ⚠ Checked only where it is read. Under Random2 the original reads NNZ from the
        // file and never looks at it again, and 43 archived runs carry a value that has
        // never done anything - rejecting it here would reject the whole archive.
        if (configuration.Generator == GeneratorSelection.Toy && configuration.GeneratorWarmup < 0)
        {
            problems.Add(
                $"{nameof(configuration.GeneratorWarmup)} = {configuration.GeneratorWarmup}: under "
                + $"{GeneratorSelection.Toy} this is the spacing between the six generator states, and a "
                + "negative spacing has no meaning. The original's own value is 6000000.");
        }
    }

    private static void RequirePositive(Length value, string name, List<string> problems)
    {
        var metres = value.Meters;
        if (!double.IsFinite(metres) || metres <= 0.0)
        {
            problems.Add($"{name} = {metres:R} m: must be a finite length above zero.");
        }
    }

    private static void RequirePositive(Density value, string name, List<string> problems)
    {
        var kilogramsPerCubicMetre = value.KilogramsPerCubicMeter;
        if (!double.IsFinite(kilogramsPerCubicMetre) || kilogramsPerCubicMetre <= 0.0)
        {
            problems.Add($"{name} = {kilogramsPerCubicMetre:R} kg/m³: must be a finite density above zero.");
        }
    }

    private static void RequirePositive(double value, string name, List<string> problems)
    {
        if (!double.IsFinite(value) || value <= 0.0)
        {
            problems.Add($"{name} = {value:R}: must be a finite number above zero.");
        }
    }

    private static void RequireShare(double value, string name, List<string> problems, bool allowZero)
    {
        var lowerBoundHolds = allowZero ? value >= 0.0 : value > 0.0;
        if (!double.IsFinite(value) || !lowerBoundHolds || value > 1.0)
        {
            var lower = allowZero ? "0" : "above 0";
            problems.Add($"{name} = {value:R}: must be a finite share between {lower} and 1.");
        }
    }

    private static string Micrometres(Length value) =>
        $"{value.Micrometers:R} µm";
}
