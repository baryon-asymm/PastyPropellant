using ParametricCombustionModel.Computation.Models.KnownParams;
using ParametricCombustionModel.Computation.Models.ProblemContexts;
using ParametricCombustionModel.Optimization.Settings;

namespace PastyPropellant.ConsoleApp.Configuration;

/// <summary>
/// The complete, fully-resolved numerical configuration of one run: which propellants file to read,
/// where the search box is, what the physical-constraint penalties are, how differential evolution is
/// parameterised and how Nelder–Mead refines its answer.
///
/// <para><b>The defaults are the contract.</b> Every property here is initialised to the literal that
/// was previously hardcoded in <c>Program.cs</c> / <see cref="GroupScenarioRunner"/>, so
/// <c>new RunConfiguration()</c> reproduces the historical run exactly. A configuration file only ever
/// <em>overrides</em> this object; when no file is present the run is byte-for-byte the pre-config run.
/// <c>RunConfigurationDefaultsTest</c> pins that property.</para>
///
/// <para>This type is also the payload of the resolved-run sidecar. The provenance argument for
/// hardcoding these values was that a recompile made every experiment traceable through git; a
/// configuration file breaks that unless the run records what it actually used, which is why this
/// record is serialised verbatim into <c>run_configuration.resolved.json</c> and summarised in the
/// PDF report.</para>
/// </summary>
public sealed record RunConfiguration
{
    /// <summary>
    /// Propellants set the run is configured against, resolved <em>relative to the process working
    /// directory</em> (the same convention the scenario settings builder and every output artefact use).
    /// </summary>
    public string InputFileName { get; init; } = "propellants.01234.json";

    /// <summary>The differential-evolution search box, expressed over the 18 base parameters.</summary>
    public BoundsConfiguration Bounds { get; init; } = new();

    /// <summary>Thresholds of the five physical-constraint penalty evaluators.</summary>
    public PenaltyConfiguration Penalties { get; init; } = new();

    /// <summary>Search-algorithm selection and its control parameters.</summary>
    public DifferentialEvolutionConfiguration DifferentialEvolution { get; init; } = new();

    /// <summary>Nelder–Mead refinement layered on the DE search.</summary>
    public NelderMeadConfiguration NelderMead { get; init; } = new();

    /// <summary>Physical constants of the model itself — not fitted, not per-propellant.</summary>
    public ModelConfiguration Model { get; init; } = new();

    /// <summary>The built-in configuration: exactly the values that used to be hardcoded.</summary>
    public static RunConfiguration Default => new();

    /// <summary>
    /// Throws when the resolved configuration would produce an invalid or nonsensical run. Called once
    /// at startup, before anything is constructed, so a bad file fails loudly rather than degrading to
    /// defaults.
    /// </summary>
    /// <exception cref="RunConfigurationException">The configuration is invalid.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(InputFileName))
            throw new RunConfigurationException("inputFileName must be a non-empty file name.");

        Bounds.Validate();
        Penalties.Validate();
        DifferentialEvolution.Validate();
        NelderMead.Validate();
        Model.Validate();
    }
}

/// <summary>
/// Physical constants of the combustion model: values that change every computed number, apply to every
/// composition, and are not fitted by the optimiser.
///
/// <para>They are configuration rather than source constants for one reason — <b>traceability</b>. The
/// metal melting temperature used to be a <c>const</c>, so a campaign at 2300 K required editing and
/// rebuilding, and none of the resulting reports recorded which value produced them; the only trace lived
/// in a diff file kept beside the outputs. Here they merge, validate and serialise like every other
/// setting, so <c>run_configuration.resolved.json</c> and the PDF header state them for every run.</para>
///
/// <para><b>These are not tuning knobs.</b> They are chosen once, by a stated rule, and frozen; scanning one
/// across runs is legitimate only as a declared sensitivity analysis.</para>
/// </summary>
public sealed record ModelConfiguration
{
    /// <summary>
    /// Melting temperature of the aluminium skeleton, in Kelvins. Default 1300 K — the historical value.
    /// The competing-flames campaign ran at 2300 K via a source patch; that is now this setting.
    /// </summary>
    public double MetalMeltingTemperatureKelvins { get; init; } =
        ParametricCombustionModel.Computation.Extensions.PropellantExtensions.MetalMeltingTemperatureKelvins;

    /// <summary>
    /// Which closure produces the skeleton surface fraction. Defaults to the historical per-propellant
    /// polynomial fit, so an unconfigured run is unchanged.
    /// </summary>
    public SkeletonSurfaceFractionConfiguration SkeletonSurfaceFraction { get; init; } = new();

    /// <summary>
    /// Bracket the condensed-phase solve bisects the surface temperature in, Kelvins. Defaults to the
    /// historical 600..900 K. It is a soft constraint, not a handbook constant: the solve reports failure
    /// when no root lies inside, and moving a bound can move the search onto a different root.
    /// </summary>
    public double MinSurfaceTemperatureKelvins { get; init; } = SurfaceTemperatureSearchBounds.MinKelvins;

    /// <inheritdoc cref="MinSurfaceTemperatureKelvins"/>
    public double MaxSurfaceTemperatureKelvins { get; init; } = SurfaceTemperatureSearchBounds.MaxKelvins;

    /// <summary>Projects onto the computation-layer type the context builders consume.</summary>
    public ModelConstants ToModelConstants() => new()
    {
        MetalMeltingTemperatureKelvins = MetalMeltingTemperatureKelvins,
        SkeletonSurfaceFraction = SkeletonSurfaceFraction.ToSettings(),
        MinSurfaceTemperatureKelvins = MinSurfaceTemperatureKelvins,
        MaxSurfaceTemperatureKelvins = MaxSurfaceTemperatureKelvins
    };

    internal void Validate()
    {
        if (!double.IsFinite(MetalMeltingTemperatureKelvins) || MetalMeltingTemperatureKelvins <= 0)
            throw new RunConfigurationException(
                $"model.metalMeltingTemperatureKelvins must be positive (got {MetalMeltingTemperatureKelvins}).");

        if (!double.IsFinite(MinSurfaceTemperatureKelvins) || MinSurfaceTemperatureKelvins <= 0)
            throw new RunConfigurationException(
                $"model.minSurfaceTemperatureKelvins must be positive (got {MinSurfaceTemperatureKelvins}).");

        if (!(MaxSurfaceTemperatureKelvins > MinSurfaceTemperatureKelvins))
            throw new RunConfigurationException(
                $"model.maxSurfaceTemperatureKelvins ({MaxSurfaceTemperatureKelvins}) must exceed " +
                $"model.minSurfaceTemperatureKelvins ({MinSurfaceTemperatureKelvins}).");

        SkeletonSurfaceFraction.Validate();
    }
}

/// <summary>
/// Configuration surface for the skeleton-coverage closure. Mirrors
/// <see cref="SkeletonSurfaceFractionSettings"/> — which carries the derivation and the reason these
/// numbers must not enter the search vector — so the serialised sidecar does not depend on the shape of
/// a computation-layer type.
/// </summary>
public sealed record SkeletonSurfaceFractionConfiguration
{
    /// <summary>
    /// <c>Polynomial</c> (default, historical), <c>EquilibriumCarbon</c> or <c>KineticCoverage</c>.
    /// </summary>
    public SkeletonSurfaceFractionMode Mode { get; init; } = SkeletonSurfaceFractionMode.Polynomial;

    /// <summary>
    /// Constants of the <c>KineticCoverage</c> closure. Defaults are the Bas_2/3/4 calibration described
    /// on <see cref="KineticCoverageSettings"/>; they are <em>calibrated</em>, not derived, and they do
    /// not describe Bas_0 or Bas_1. Read only when <see cref="Mode"/> is <c>KineticCoverage</c>, but
    /// validated always.
    /// </summary>
    public KineticCoverageConfiguration Kinetic { get; init; } = new();

    /// <summary>
    /// Where to read the equilibrium coverage table from, resolved against the process working directory
    /// like every other path in this file. Read only when <see cref="Mode"/> is
    /// <c>EquilibriumCarbon</c>.
    ///
    /// <para>The table is derived data, not configuration: it is a pure function of the recipe and of the
    /// thermodynamics, produced by <c>generate_skeleton_carbon_equilibrium.py</c>. It is a path rather
    /// than an inline block because it holds one curve per propellant per pressure, and because a run
    /// must be able to record <em>which</em> table it used — the file is regenerated whenever the
    /// propellants file changes.</para>
    /// </summary>
    public string EquilibriumTableFile { get; init; } = "skeleton_carbon_equilibrium.json";

    /// <summary>
    /// Projects onto the computation-layer settings type, loading the table when the mode needs it.
    /// </summary>
    public SkeletonSurfaceFractionSettings ToSettings() => new()
    {
        Mode = Mode,
        Kinetic = Kinetic.ToSettings(),
        EquilibriumTable = Mode == SkeletonSurfaceFractionMode.EquilibriumCarbon
            ? SkeletonCarbonEquilibriumTable.Load(EquilibriumTableFile)
            : null
    };

    internal void Validate()
    {
        if (!Enum.IsDefined(Mode))
            throw new RunConfigurationException(
                $"model.skeletonSurfaceFraction.mode '{Mode}' is not a known closure.");

        // Whichever mode is in force: a broken kinetic block must fail now, not on the day someone
        // switches the mode.
        try
        {
            Kinetic.ToSettings().Validate();
        }
        catch (ArgumentException ex)
        {
            throw new RunConfigurationException($"model.skeletonSurfaceFraction.kinetic: {ex.Message}", ex);
        }

        if (Mode != SkeletonSurfaceFractionMode.EquilibriumCarbon)
            return;

        if (string.IsNullOrWhiteSpace(EquilibriumTableFile))
            throw new RunConfigurationException(
                "model.skeletonSurfaceFraction.mode is EquilibriumCarbon but " +
                "equilibriumTableFile is empty.");

        // Load it now rather than at first use: a missing or malformed table must stop the run before the
        // search starts, not hours in when the first context is built.
        try
        {
            ToSettings().Validate();
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or IOException)
        {
            throw new RunConfigurationException($"model.skeletonSurfaceFraction: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Equality over the declared closure, not over the loaded table: two configurations naming the same
    /// file are the same configuration, and comparing them must not re-read it from disk.
    /// </summary>
    public bool Equals(SkeletonSurfaceFractionConfiguration? other) =>
        other is not null
        && Mode == other.Mode
        && Kinetic == other.Kinetic
        && string.Equals(EquilibriumTableFile, other.EquilibriumTableFile, StringComparison.Ordinal);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Mode, Kinetic, EquilibriumTableFile);
}

/// <summary>
/// Configuration surface for the kinetic-coverage constants. Mirrors
/// <see cref="KineticCoverageSettings"/>, which carries the derivation, the calibration set and the
/// compositions the calibration does <em>not</em> cover.
/// </summary>
public sealed record KineticCoverageConfiguration
{
    /// <summary><c>a₀</c>, the pressure-independent burnout channel of the binder.</summary>
    public double BinderChannel { get; init; } = KineticCoverageSettings.Default.BinderChannel;

    /// <summary><c>a₁</c>, the burnout channel of the fine oxidiser, per unit fine-AP mass fraction.</summary>
    public double FineOxidiserChannel { get; init; } = KineticCoverageSettings.Default.FineOxidiserChannel;

    /// <summary><c>m</c>, the pressure order of the fine-oxidiser channel.</summary>
    public double PressureOrder { get; init; } = KineticCoverageSettings.Default.PressureOrder;

    /// <summary><c>p_ref</c> in pascals, the pressure at which <see cref="FineOxidiserChannel"/> is quoted.</summary>
    public double ReferencePressurePascals { get; init; } =
        KineticCoverageSettings.Default.ReferencePressurePascals;

    /// <summary>Projects onto the computation-layer settings type.</summary>
    public KineticCoverageSettings ToSettings() => new()
    {
        BinderChannel = BinderChannel,
        FineOxidiserChannel = FineOxidiserChannel,
        PressureOrder = PressureOrder,
        ReferencePressurePascals = ReferencePressurePascals
    };
}

/// <summary>
/// The 18-parameter search box, keyed by parameter name rather than by index so that a partial
/// configuration can widen one bound without restating the other seventeen.
/// </summary>
public sealed record BoundsConfiguration
{
    /// <summary>Lower bound of each base parameter, keyed by <see cref="BoundsProvider.BaseParameterNames"/>.</summary>
    public IReadOnlyDictionary<string, double> Lower { get; init; } =
        BoundsProvider.ToNamedBound(BoundsProvider.GetBaseLowerBound());

    /// <summary>Upper bound of each base parameter, keyed by <see cref="BoundsProvider.BaseParameterNames"/>.</summary>
    public IReadOnlyDictionary<string, double> Upper { get; init; } =
        BoundsProvider.ToNamedBound(BoundsProvider.GetBaseUpperBound());

    /// <summary>The 18-element positional lower bound.</summary>
    public double[] ToBaseLowerBound() => BoundsProvider.ToBaseVector(Lower);

    /// <summary>The 18-element positional upper bound.</summary>
    public double[] ToBaseUpperBound() => BoundsProvider.ToBaseVector(Upper);

    /// <summary>The 32-element group lower bound actually handed to the optimiser.</summary>
    public double[] ToGroupLowerBound() => BoundsProvider.ExpandToGroupVector(ToBaseLowerBound());

    /// <summary>The 32-element group upper bound actually handed to the optimiser.</summary>
    public double[] ToGroupUpperBound() => BoundsProvider.ExpandToGroupVector(ToBaseUpperBound());

    /// <summary>
    /// Structural equality over the bound maps. The compiler-generated record equality compares the two
    /// dictionaries by reference, which would make every <see cref="BoundsConfiguration"/> — and hence
    /// every <see cref="RunConfiguration"/> — unequal to every other, including a freshly loaded
    /// default configuration and <see cref="RunConfiguration.Default"/>. Since this record is the thing
    /// two runs get compared by, the comparison has to be by value.
    /// </summary>
    public bool Equals(BoundsConfiguration? other) =>
        other is not null && SameBounds(Lower, other.Lower) && SameBounds(Upper, other.Upper);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var name in BoundsProvider.BaseParameterNames)
        {
            hash.Add(Lower.TryGetValue(name, out var lower) ? lower : double.NaN);
            hash.Add(Upper.TryGetValue(name, out var upper) ? upper : double.NaN);
        }

        return hash.ToHashCode();
    }

    private static bool SameBounds(
        IReadOnlyDictionary<string, double> left,
        IReadOnlyDictionary<string, double> right)
    {
        if (left.Count != right.Count)
            return false;

        foreach (var pair in left)
            if (!right.TryGetValue(pair.Key, out var value) || !value.Equals(pair.Value))
                return false;

        return true;
    }

    internal void Validate()
    {
        double[] lower;
        double[] upper;

        try
        {
            lower = ToBaseLowerBound();
            upper = ToBaseUpperBound();
        }
        catch (ArgumentException ex)
        {
            throw new RunConfigurationException($"bounds: {ex.Message}", ex);
        }

        for (int i = 0; i < BoundsProvider.BaseVectorLength; i++)
        {
            var name = BoundsProvider.BaseParameterNames[i];

            if (!double.IsFinite(lower[i]) || !double.IsFinite(upper[i]))
                throw new RunConfigurationException(
                    $"bounds: '{name}' must be finite (got [{lower[i]}, {upper[i]}]).");

            if (lower[i] > upper[i])
                throw new RunConfigurationException(
                    $"bounds: lower bound of '{name}' ({lower[i]}) exceeds its upper bound ({upper[i]}).");
        }
    }
}

/// <summary>
/// Thresholds of the five constraint-penalty evaluators, in the order the fitness aggregation applies
/// them. These are model configuration rather than tuning knobs; they are exposed so an experiment can
/// be run without a recompile, not because they are expected to move often.
/// </summary>
public sealed record PenaltyConfiguration
{
    /// <summary>Shared penalty rate applied by every evaluator.</summary>
    public double PenaltyRate { get; init; } = 0.01;

    /// <summary>Pocket heat-flux ratio competition threshold.</summary>
    public double HeatFluxRatioThreshold { get; init; } = 100.0;

    /// <summary>Pore diameter threshold.</summary>
    public double PoreDiameterThreshold { get; init; } = 3.0;

    /// <summary>Large oxidiser particle size threshold.</summary>
    public double LargeOxidizerParticleSizeThreshold { get; init; } = 1.0;

    /// <summary>Maximum inter-pocket kinetic-flame heat flux, W/m².</summary>
    public double MaxInterPocketKineticFlameHeatFluxWattsPerSquareMeter { get; init; } = 1e9;

    /// <summary>Maximum skeleton kinetic-flame heat flux, W/m².</summary>
    public double MaxSkeletonKineticFlameHeatFluxWattsPerSquareMeter { get; init; } = 1e8;

    /// <summary>Maximum out-skeleton kinetic-flame heat flux, W/m².</summary>
    public double MaxOutSkeletonKineticFlameHeatFluxWattsPerSquareMeter { get; init; } = 1e8;

    internal void Validate()
    {
        Require(PenaltyRate >= 0, nameof(PenaltyRate), PenaltyRate, "must be non-negative");
        Require(HeatFluxRatioThreshold > 0, nameof(HeatFluxRatioThreshold), HeatFluxRatioThreshold, "must be positive");
        Require(PoreDiameterThreshold > 0, nameof(PoreDiameterThreshold), PoreDiameterThreshold, "must be positive");
        Require(LargeOxidizerParticleSizeThreshold > 0, nameof(LargeOxidizerParticleSizeThreshold),
            LargeOxidizerParticleSizeThreshold, "must be positive");
        Require(MaxInterPocketKineticFlameHeatFluxWattsPerSquareMeter > 0,
            nameof(MaxInterPocketKineticFlameHeatFluxWattsPerSquareMeter),
            MaxInterPocketKineticFlameHeatFluxWattsPerSquareMeter, "must be positive");
        Require(MaxSkeletonKineticFlameHeatFluxWattsPerSquareMeter > 0,
            nameof(MaxSkeletonKineticFlameHeatFluxWattsPerSquareMeter),
            MaxSkeletonKineticFlameHeatFluxWattsPerSquareMeter, "must be positive");
        Require(MaxOutSkeletonKineticFlameHeatFluxWattsPerSquareMeter > 0,
            nameof(MaxOutSkeletonKineticFlameHeatFluxWattsPerSquareMeter),
            MaxOutSkeletonKineticFlameHeatFluxWattsPerSquareMeter, "must be positive");
    }

    private static void Require(bool condition, string name, double value, string requirement)
    {
        if (!condition || !double.IsFinite(value))
            throw new RunConfigurationException($"penalties.{name} {requirement} (got {value}).");
    }
}

/// <summary>
/// Search-algorithm configuration.
///
/// <para>The two strategy families are modelled as separate sub-records rather than as one flat set of
/// nullable knobs. That is deliberate: L-SHADE is budget-driven with a shrinking population while the
/// fixed-population strategies are stagnation-driven, so the values that matter differ. Flattening them
/// would make <c>null</c> mean both "inherit the default" and "not applicable to this strategy", and a
/// configuration file could not express the difference. Here every knob has a concrete default and only
/// the branch matching <see cref="Strategy"/> is read.</para>
/// </summary>
public sealed record DifferentialEvolutionConfiguration
{
    /// <summary>
    /// Search strategy. jDE is the branch default: its exploration reaches the clean basin, whereas
    /// current-to-pbest variants either trap in a penalised degenerate corner or converge prematurely.
    /// </summary>
    public DifferentialEvolutionStrategy Strategy { get; init; } = DifferentialEvolutionStrategy.Jde;

    /// <summary>Mutation force F; consumed only by the Classic and jDE strategies.</summary>
    public double MutationForce { get; init; } = 0.5;

    /// <summary>Crossover probability CR; consumed only by the Classic and jDE strategies.</summary>
    public double CrossoverProbability { get; init; } = 0.9;

    /// <summary>Wall-clock safety timeout that terminates any run, whichever strategy is selected.</summary>
    public double SafetyTimeoutHours { get; init; } = 9.0;

    /// <summary>
    /// Fixed RNG seed. Null by default, which leaves the search unseeded — the behaviour of every run in
    /// this project's history, and the reason repeated campaigns had to be compared statistically rather
    /// than exactly.
    ///
    /// <para>Set it and the run becomes bit-for-bit repeatable, but only together with the worker count:
    /// the library derives one stream per worker, so the same seed under a different
    /// <c>fixedPopulation.minProcessorsCount</c> — or simply on a machine with a different core count — is
    /// a different search. Both numbers are recorded in <c>run_configuration.resolved.json</c> for that
    /// reason; quoting a seed without the worker count does not identify a run.</para>
    /// </summary>
    public int? Seed { get; init; }

    /// <summary>Controls for the fixed-population strategies (Classic / jDE / JADE / SHADE).</summary>
    public FixedPopulationConfiguration FixedPopulation { get; init; } = new();

    /// <summary>Controls for the budget-driven L-SHADE variant.</summary>
    public LShadeConfiguration LShade { get; init; } = new();

    internal void Validate()
    {
        if (!Enum.IsDefined(Strategy))
            throw new RunConfigurationException($"differentialEvolution.strategy '{Strategy}' is not a known strategy.");

        if (!double.IsFinite(MutationForce) || MutationForce < 0 || MutationForce > 2)
            throw new RunConfigurationException(
                $"differentialEvolution.mutationForce must be between 0 and 2 (got {MutationForce}).");

        if (!double.IsFinite(CrossoverProbability) || CrossoverProbability < 0 || CrossoverProbability > 1)
            throw new RunConfigurationException(
                $"differentialEvolution.crossoverProbability must be between 0 and 1 (got {CrossoverProbability}).");

        if (!double.IsFinite(SafetyTimeoutHours) || SafetyTimeoutHours <= 0)
            throw new RunConfigurationException(
                $"differentialEvolution.safetyTimeoutHours must be positive (got {SafetyTimeoutHours}).");

        // Both branches are validated regardless of the selected strategy: a configuration that is
        // invalid for a strategy it does not currently select is still a broken file, and silently
        // accepting it would let the error surface only after someone flips `strategy`.
        FixedPopulation.Validate();
        LShade.Validate();
    }
}

/// <summary>Controls for the fixed-population strategies: sizing, worker count and stagnation termination.</summary>
public sealed record FixedPopulationConfiguration
{
    /// <summary>Population size as a multiple of the 32-dimension vector (32 × 12 = 384).</summary>
    public int PopulationSizeMultiplier { get; init; } = 12;

    /// <summary>
    /// Floor of the worker-count reduction rule: the host starts at <c>ProcessorCount − 1</c> and walks
    /// down while the count does not divide the population evenly, stopping at this value.
    /// </summary>
    public int MinProcessorsCount { get; init; } = 14;

    /// <summary>Generations without relative improvement before the run is considered stagnant.</summary>
    public int MaxStagnationStreak { get; init; } = 5_000;

    /// <summary>Relative improvement below which a generation counts as stagnant.</summary>
    public double RelativeStagnationThreshold { get; init; } = 1e-6;

    internal void Validate()
    {
        if (PopulationSizeMultiplier <= 0)
            throw new RunConfigurationException(
                $"differentialEvolution.fixedPopulation.populationSizeMultiplier must be positive (got {PopulationSizeMultiplier}).");

        if (MinProcessorsCount <= 0)
            throw new RunConfigurationException(
                $"differentialEvolution.fixedPopulation.minProcessorsCount must be positive (got {MinProcessorsCount}).");

        if (MaxStagnationStreak <= 0)
            throw new RunConfigurationException(
                $"differentialEvolution.fixedPopulation.maxStagnationStreak must be positive (got {MaxStagnationStreak}).");

        if (!double.IsFinite(RelativeStagnationThreshold) || RelativeStagnationThreshold < 0)
            throw new RunConfigurationException(
                $"differentialEvolution.fixedPopulation.relativeStagnationThreshold must be non-negative (got {RelativeStagnationThreshold}).");
    }
}

/// <summary>Canonical L-SHADE control parameters (Tanabe &amp; Fukunaga 2014) and its evaluation budget.</summary>
public sealed record LShadeConfiguration
{
    /// <summary>Initial population size as a multiple of the 32-dimension vector (32 × 18 = 576).</summary>
    public int PopulationSizeMultiplier { get; init; } = 18;

    /// <summary>Evaluation budget; both terminates the run and defines the linear population-reduction schedule.</summary>
    public long MaxEvaluationNumber { get; init; } = 5_000_000;

    /// <summary>p-best rate.</summary>
    public double PBestRate { get; init; } = 0.11;

    /// <summary>Archive size rate.</summary>
    public double ArchiveSizeRate { get; init; } = 2.6;

    /// <summary>Success-history memory size.</summary>
    public int MemorySize { get; init; } = 6;

    internal void Validate()
    {
        if (PopulationSizeMultiplier <= 0)
            throw new RunConfigurationException(
                $"differentialEvolution.lShade.populationSizeMultiplier must be positive (got {PopulationSizeMultiplier}).");

        if (MaxEvaluationNumber <= 0)
            throw new RunConfigurationException(
                $"differentialEvolution.lShade.maxEvaluationNumber must be positive (got {MaxEvaluationNumber}).");

        if (!double.IsFinite(PBestRate) || PBestRate <= 0 || PBestRate > 1)
            throw new RunConfigurationException(
                $"differentialEvolution.lShade.pBestRate must be in (0, 1] (got {PBestRate}).");

        if (!double.IsFinite(ArchiveSizeRate) || ArchiveSizeRate < 0)
            throw new RunConfigurationException(
                $"differentialEvolution.lShade.archiveSizeRate must be non-negative (got {ArchiveSizeRate}).");

        if (MemorySize <= 0)
            throw new RunConfigurationException(
                $"differentialEvolution.lShade.memorySize must be positive (got {MemorySize}).");
    }
}

/// <summary>
/// Nelder–Mead refinement configuration. Mirrors <see cref="NelderMeadRefinementSettings"/> but is a
/// configuration-file surface in its own right, so the serialised sidecar does not depend on the shape
/// of a library type.
/// </summary>
public sealed record NelderMeadConfiguration
{
    /// <summary>Master switch. When <c>false</c> the optimiser runs plain DE.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// In-loop memetic refiner. Off by default: tested with jDE it reached essentially the same point
    /// as final-polish-only but a hair worse and much earlier, trimming diversity for no gain.
    /// </summary>
    public bool MemeticInLoop { get; init; }

    /// <summary>Generation interval between memetic refiner calls.</summary>
    public int EveryNGenerations { get; init; } = 50;

    /// <summary>Per-call evaluation budget for the memetic refiner.</summary>
    public long MemeticMaxEvaluationsPerCall { get; init; } = 200;

    /// <summary>Final sequential polish of the converged best; guarded, so it can only improve the DE best.</summary>
    public bool FinalPolish { get; init; } = true;

    /// <summary>Total evaluation budget for the final polish.</summary>
    public long FinalPolishMaxEvaluations { get; init; } = 100_000;

    /// <summary>Use the dimension-adaptive (Gao–Han 2012) simplex coefficients.</summary>
    public bool AdaptiveCoefficients { get; init; } = true;

    /// <summary>Simplex domain-size convergence tolerance.</summary>
    public double DomainTolerance { get; init; } = 1e-8;

    /// <summary>Vertex value-spread convergence tolerance.</summary>
    public double FunctionTolerance { get; init; } = 1e-8;

    /// <summary>Maximum automatic simplex restarts on convergence before stopping.</summary>
    public int Restarts { get; init; } = 2;

    /// <summary>Projects onto the library settings type the optimiser consumes.</summary>
    public NelderMeadRefinementSettings ToRefinementSettings() => new()
    {
        Enabled = Enabled,
        MemeticInLoop = MemeticInLoop,
        EveryNGenerations = EveryNGenerations,
        MemeticMaxEvaluationsPerCall = MemeticMaxEvaluationsPerCall,
        FinalPolish = FinalPolish,
        FinalPolishMaxEvaluations = FinalPolishMaxEvaluations,
        AdaptiveCoefficients = AdaptiveCoefficients,
        DomainTolerance = DomainTolerance,
        FunctionTolerance = FunctionTolerance,
        Restarts = Restarts
    };

    internal void Validate()
    {
        // Reuse the library's own rules so the configuration surface cannot drift from what the
        // optimiser will accept; only the message is re-framed as a configuration error.
        try
        {
            ToRefinementSettings().Validate();
        }
        catch (InvalidOperationException ex)
        {
            throw new RunConfigurationException($"nelderMead: {ex.Message}", ex);
        }
    }
}
