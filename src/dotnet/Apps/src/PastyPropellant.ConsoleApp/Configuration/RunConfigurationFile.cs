namespace PastyPropellant.ConsoleApp.Configuration;

/// <summary>
/// The on-disk shape of a run-configuration file: the same tree as <see cref="RunConfiguration"/> with
/// every member nullable.
///
/// <para>Nullable-everything is what makes <b>partial</b> configurations work. Each section is merged
/// onto the built-in defaults member by member, so a file may contain nothing but
/// <c>{"penalties": {"poreDiameterThreshold": 4.0}}</c> and inherit the other thirty-odd settings. An
/// absent member and an explicit <c>null</c> are treated identically — both mean "inherit" — because
/// none of these settings has a meaningful null value; the settings that genuinely are
/// strategy-dependent live in separate sub-records for exactly that reason (see
/// <see cref="DifferentialEvolutionConfiguration"/>).</para>
///
/// <para>Unknown member names are rejected by the deserialiser rather than ignored, so a typo or a
/// setting from a future schema fails the run instead of quietly doing nothing.</para>
/// </summary>
internal sealed record RunConfigurationFile
{
    public string? InputFileName { get; init; }

    public BoundsFileSection? Bounds { get; init; }

    public PenaltiesFileSection? Penalties { get; init; }

    public DifferentialEvolutionFileSection? DifferentialEvolution { get; init; }

    public NelderMeadFileSection? NelderMead { get; init; }

    /// <summary>Merges this file onto <paramref name="defaults"/>, member by member.</summary>
    public RunConfiguration ApplyTo(RunConfiguration defaults) => new()
    {
        InputFileName = InputFileName ?? defaults.InputFileName,
        Bounds = Bounds?.ApplyTo(defaults.Bounds) ?? defaults.Bounds,
        Penalties = Penalties?.ApplyTo(defaults.Penalties) ?? defaults.Penalties,
        DifferentialEvolution = DifferentialEvolution?.ApplyTo(defaults.DifferentialEvolution)
                                ?? defaults.DifferentialEvolution,
        NelderMead = NelderMead?.ApplyTo(defaults.NelderMead) ?? defaults.NelderMead
    };
}

/// <summary>Bound overrides, keyed by base-parameter name; unnamed parameters keep their default bound.</summary>
internal sealed record BoundsFileSection
{
    public Dictionary<string, double>? Lower { get; init; }

    public Dictionary<string, double>? Upper { get; init; }

    public BoundsConfiguration ApplyTo(BoundsConfiguration defaults) => new()
    {
        Lower = Overlay(defaults.Lower, Lower),
        Upper = Overlay(defaults.Upper, Upper)
    };

    /// <summary>
    /// Copies the defaults and overwrites only the named entries. Names are not filtered here: an
    /// unrecognised name survives into the merged map so that <see cref="BoundsProvider.ToBaseVector"/>
    /// can reject it by name during validation.
    /// </summary>
    private static IReadOnlyDictionary<string, double> Overlay(
        IReadOnlyDictionary<string, double> defaults,
        Dictionary<string, double>? overrides)
    {
        // Rebuilt in canonical parameter order so the resolved sidecar serialises deterministically.
        var merged = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var name in BoundsProvider.BaseParameterNames)
            if (defaults.TryGetValue(name, out var value))
                merged[name] = value;

        foreach (var pair in defaults)
            merged.TryAdd(pair.Key, pair.Value);

        if (overrides != null)
            foreach (var pair in overrides)
                merged[pair.Key] = pair.Value;

        return merged;
    }
}

/// <summary>Penalty-threshold overrides.</summary>
internal sealed record PenaltiesFileSection
{
    public double? PenaltyRate { get; init; }
    public double? HeatFluxRatioThreshold { get; init; }
    public double? PoreDiameterThreshold { get; init; }
    public double? LargeOxidizerParticleSizeThreshold { get; init; }
    public double? MaxInterPocketKineticFlameHeatFluxWattsPerSquareMeter { get; init; }
    public double? MaxSkeletonKineticFlameHeatFluxWattsPerSquareMeter { get; init; }
    public double? MaxOutSkeletonKineticFlameHeatFluxWattsPerSquareMeter { get; init; }

    public PenaltyConfiguration ApplyTo(PenaltyConfiguration d) => new()
    {
        PenaltyRate = PenaltyRate ?? d.PenaltyRate,
        HeatFluxRatioThreshold = HeatFluxRatioThreshold ?? d.HeatFluxRatioThreshold,
        PoreDiameterThreshold = PoreDiameterThreshold ?? d.PoreDiameterThreshold,
        LargeOxidizerParticleSizeThreshold =
            LargeOxidizerParticleSizeThreshold ?? d.LargeOxidizerParticleSizeThreshold,
        MaxInterPocketKineticFlameHeatFluxWattsPerSquareMeter =
            MaxInterPocketKineticFlameHeatFluxWattsPerSquareMeter
            ?? d.MaxInterPocketKineticFlameHeatFluxWattsPerSquareMeter,
        MaxSkeletonKineticFlameHeatFluxWattsPerSquareMeter =
            MaxSkeletonKineticFlameHeatFluxWattsPerSquareMeter
            ?? d.MaxSkeletonKineticFlameHeatFluxWattsPerSquareMeter,
        MaxOutSkeletonKineticFlameHeatFluxWattsPerSquareMeter =
            MaxOutSkeletonKineticFlameHeatFluxWattsPerSquareMeter
            ?? d.MaxOutSkeletonKineticFlameHeatFluxWattsPerSquareMeter
    };
}

/// <summary>Search-algorithm overrides.</summary>
internal sealed record DifferentialEvolutionFileSection
{
    public ParametricCombustionModel.Optimization.Settings.DifferentialEvolutionStrategy? Strategy { get; init; }
    public double? MutationForce { get; init; }
    public double? CrossoverProbability { get; init; }
    public double? SafetyTimeoutHours { get; init; }
    public int? Seed { get; init; }
    public FixedPopulationFileSection? FixedPopulation { get; init; }
    public LShadeFileSection? LShade { get; init; }

    public DifferentialEvolutionConfiguration ApplyTo(DifferentialEvolutionConfiguration d) => new()
    {
        Strategy = Strategy ?? d.Strategy,
        MutationForce = MutationForce ?? d.MutationForce,
        CrossoverProbability = CrossoverProbability ?? d.CrossoverProbability,
        SafetyTimeoutHours = SafetyTimeoutHours ?? d.SafetyTimeoutHours,
        Seed = Seed ?? d.Seed,
        FixedPopulation = FixedPopulation?.ApplyTo(d.FixedPopulation) ?? d.FixedPopulation,
        LShade = LShade?.ApplyTo(d.LShade) ?? d.LShade
    };
}

/// <summary>Fixed-population strategy overrides.</summary>
internal sealed record FixedPopulationFileSection
{
    public int? PopulationSizeMultiplier { get; init; }
    public int? MinProcessorsCount { get; init; }
    public int? MaxStagnationStreak { get; init; }
    public double? RelativeStagnationThreshold { get; init; }

    public FixedPopulationConfiguration ApplyTo(FixedPopulationConfiguration d) => new()
    {
        PopulationSizeMultiplier = PopulationSizeMultiplier ?? d.PopulationSizeMultiplier,
        MinProcessorsCount = MinProcessorsCount ?? d.MinProcessorsCount,
        MaxStagnationStreak = MaxStagnationStreak ?? d.MaxStagnationStreak,
        RelativeStagnationThreshold = RelativeStagnationThreshold ?? d.RelativeStagnationThreshold
    };
}

/// <summary>L-SHADE overrides.</summary>
internal sealed record LShadeFileSection
{
    public int? PopulationSizeMultiplier { get; init; }
    public long? MaxEvaluationNumber { get; init; }
    public double? PBestRate { get; init; }
    public double? ArchiveSizeRate { get; init; }
    public int? MemorySize { get; init; }

    public LShadeConfiguration ApplyTo(LShadeConfiguration d) => new()
    {
        PopulationSizeMultiplier = PopulationSizeMultiplier ?? d.PopulationSizeMultiplier,
        MaxEvaluationNumber = MaxEvaluationNumber ?? d.MaxEvaluationNumber,
        PBestRate = PBestRate ?? d.PBestRate,
        ArchiveSizeRate = ArchiveSizeRate ?? d.ArchiveSizeRate,
        MemorySize = MemorySize ?? d.MemorySize
    };
}

/// <summary>Nelder–Mead refinement overrides.</summary>
internal sealed record NelderMeadFileSection
{
    public bool? Enabled { get; init; }
    public bool? MemeticInLoop { get; init; }
    public int? EveryNGenerations { get; init; }
    public long? MemeticMaxEvaluationsPerCall { get; init; }
    public bool? FinalPolish { get; init; }
    public long? FinalPolishMaxEvaluations { get; init; }
    public bool? AdaptiveCoefficients { get; init; }
    public double? DomainTolerance { get; init; }
    public double? FunctionTolerance { get; init; }
    public int? Restarts { get; init; }

    public NelderMeadConfiguration ApplyTo(NelderMeadConfiguration d) => new()
    {
        Enabled = Enabled ?? d.Enabled,
        MemeticInLoop = MemeticInLoop ?? d.MemeticInLoop,
        EveryNGenerations = EveryNGenerations ?? d.EveryNGenerations,
        MemeticMaxEvaluationsPerCall = MemeticMaxEvaluationsPerCall ?? d.MemeticMaxEvaluationsPerCall,
        FinalPolish = FinalPolish ?? d.FinalPolish,
        FinalPolishMaxEvaluations = FinalPolishMaxEvaluations ?? d.FinalPolishMaxEvaluations,
        AdaptiveCoefficients = AdaptiveCoefficients ?? d.AdaptiveCoefficients,
        DomainTolerance = DomainTolerance ?? d.DomainTolerance,
        FunctionTolerance = FunctionTolerance ?? d.FunctionTolerance,
        Restarts = Restarts ?? d.Restarts
    };
}
