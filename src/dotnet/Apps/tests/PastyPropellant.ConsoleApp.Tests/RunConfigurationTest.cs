using System.Text.Json;
using ParametricCombustionModel.Optimization.Settings;
using PastyPropellant.Interop;
using PastyPropellant.ConsoleApp.Configuration;
using PastyPropellant.ConsoleApp.Io;
using PastyPropellant.ConsoleApp.Runners;

namespace PastyPropellant.ConsoleApp.Tests;

/// <summary>
/// Pins the optional run-configuration surface.
///
/// <para>The load-bearing test here is <see cref="DefaultsReproduceTheHardcodedConfiguration"/>. Making
/// these values configurable is only safe if <em>absence</em> of a configuration file is indistinguishable
/// from the previous hardcoded host, so the expected values below are written out as literals rather
/// than read back from <see cref="BoundsProvider"/> or the configuration records. Asserting a value
/// against the code that produces it would pass no matter what that code said; these literals are the
/// independent record of what the run used to be, and a diff here means the default behaviour moved.</para>
///
/// <para>The remaining tests cover the parts that must fail loudly: an unknown setting, an unknown
/// parameter name, an inverted bound and a missing explicitly-requested file are all configuration
/// errors, never reasons to fall back to a default.</para>
/// </summary>
public class RunConfigurationTest : IDisposable
{
    private readonly string _directory =
        Path.Combine(Path.GetTempPath(), $"pastypropellant_config_{Guid.NewGuid():N}");

    public RunConfigurationTest() => Directory.CreateDirectory(_directory);

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// The 18 base-parameter bounds as they were hardcoded in the host, in base-vector order:
    /// name, lower, upper.
    /// </summary>
    public static TheoryData<int, string, double, double> ExpectedBounds => new()
    {
        { 0,  "ADecompose",                       1.0,   1e13 },
        { 1,  "EDecompose",                       5e4,   3e5 },
        { 2,  "AKineticFlameInterPocket",         1e5,   1e13 },
        { 3,  "EKineticFlameInterPocket",         5e4,   2.5e5 },
        { 4,  "AKineticFlamePocketOutSkeleton",   1e5,   1e13 },
        { 5,  "EKineticFlamePocketOutSkeleton",   5e4,   2.5e5 },
        { 6,  "AKineticFlamePocketSkeleton",      1e5,   1e13 },
        { 7,  "EKineticFlamePocketSkeleton",      5e4,   2.5e5 },
        { 8,  "NuInterPocket",                    0.0,   2.5 },
        { 9,  "NuPocketOutSkeleton",              0.0,   2.5 },
        { 10, "NuPocketSkeleton",                 0.0,   2.5 },
        { 11, "AMetalBurningConstant",            1e-10, 1e-3 },
        { 12, "BMetalBurningConstant",            1e-12, 1e-5 },
        { 13, "DeltaH",                           -1e7,  1e7 },
        { 14, "KDiffusionHeight",                 1e-3,  1e1 },
        { 15, "APowOrder",                        0.0,   3.0 },
        { 16, "BPowOrder",                        0.0,   3.0 },
        { 17, "KCoefficientRadiationTemperature", 0.0,   1.0 }
    };

    [Fact]
    public void DefaultsReproduceTheHardcodedConfiguration()
    {
        var configuration = RunConfiguration.Default;

        Assert.Equal("propellants.01234.json", configuration.InputFileName);

        var penalties = configuration.Penalties;
        Assert.Equal(0.01, penalties.PenaltyRate);
        Assert.Equal(100.0, penalties.HeatFluxRatioThreshold);
        Assert.Equal(3.0, penalties.PoreDiameterThreshold);
        Assert.Equal(1.0, penalties.LargeOxidizerParticleSizeThreshold);
        Assert.Equal(1e9, penalties.MaxInterPocketKineticFlameHeatFluxWattsPerSquareMeter);
        Assert.Equal(1e8, penalties.MaxSkeletonKineticFlameHeatFluxWattsPerSquareMeter);
        Assert.Equal(1e8, penalties.MaxOutSkeletonKineticFlameHeatFluxWattsPerSquareMeter);

        var de = configuration.DifferentialEvolution;
        Assert.Equal(DifferentialEvolutionStrategy.Jde, de.Strategy);
        Assert.Equal(0.5, de.MutationForce);
        Assert.Equal(0.9, de.CrossoverProbability);
        Assert.Equal(9.0, de.SafetyTimeoutHours);

        Assert.Equal(12, de.FixedPopulation.PopulationSizeMultiplier);
        Assert.Equal(14, de.FixedPopulation.MinProcessorsCount);
        Assert.Equal(5_000, de.FixedPopulation.MaxStagnationStreak);
        Assert.Equal(1e-6, de.FixedPopulation.RelativeStagnationThreshold);

        Assert.Equal(18, de.LShade.PopulationSizeMultiplier);
        Assert.Equal(5_000_000L, de.LShade.MaxEvaluationNumber);
        Assert.Equal(0.11, de.LShade.PBestRate);
        Assert.Equal(2.6, de.LShade.ArchiveSizeRate);
        Assert.Equal(6, de.LShade.MemorySize);

        var nelderMead = configuration.NelderMead;
        Assert.True(nelderMead.Enabled);
        Assert.False(nelderMead.MemeticInLoop);
        Assert.Equal(50, nelderMead.EveryNGenerations);
        Assert.Equal(200L, nelderMead.MemeticMaxEvaluationsPerCall);
        Assert.True(nelderMead.FinalPolish);
        Assert.Equal(100_000L, nelderMead.FinalPolishMaxEvaluations);
        Assert.True(nelderMead.AdaptiveCoefficients);
        Assert.Equal(1e-8, nelderMead.DomainTolerance);
        Assert.Equal(1e-8, nelderMead.FunctionTolerance);
        Assert.Equal(2, nelderMead.Restarts);

        // The Nelder-Mead projection must hand the optimiser exactly these values, not a library default.
        var refinement = nelderMead.ToRefinementSettings();
        Assert.True(refinement.Enabled);
        Assert.False(refinement.MemeticInLoop);
        Assert.True(refinement.FinalPolish);
        Assert.Equal(100_000L, refinement.FinalPolishMaxEvaluations);
        Assert.Equal(2, refinement.Restarts);
    }

    [Theory]
    [MemberData(nameof(ExpectedBounds))]
    public void DefaultBoundsReproduceTheHardcodedSearchBox(int index, string name, double lower, double upper)
    {
        var bounds = RunConfiguration.Default.Bounds;

        Assert.Equal(name, BoundsProvider.BaseParameterNames[index]);
        Assert.Equal(lower, bounds.Lower[name]);
        Assert.Equal(upper, bounds.Upper[name]);
        Assert.Equal(lower, bounds.ToBaseLowerBound()[index]);
        Assert.Equal(upper, bounds.ToBaseUpperBound()[index]);
    }

    [Fact]
    public void DefaultBoundsExpandToTheSameGroupVectorTheHostUsedToBuild()
    {
        var bounds = RunConfiguration.Default.Bounds;

        Assert.Equal(BoundsProvider.GetGroupLowerBound(), bounds.ToGroupLowerBound());
        Assert.Equal(BoundsProvider.GetGroupUpperBound(), bounds.ToGroupUpperBound());
        Assert.Equal(BoundsProvider.GroupVectorLength, bounds.ToGroupLowerBound().Length);
    }

    [Fact]
    public void LoadWithNoFilePresentReturnsTheDefaults()
    {
        var loaded = RunConfigurationLoader.Load(baseDirectory: _directory);

        Assert.Null(loaded.FilePath);
        Assert.Equal(RunConfiguration.Default, loaded.Configuration);
        Assert.Contains("built-in defaults", loaded.SourceDescription);
    }

    [Fact]
    public void LoadWithAnEmptyObjectFileReturnsTheDefaults()
    {
        var path = WriteConfig("{}");

        var loaded = RunConfigurationLoader.Load(path);

        Assert.Equal(RunConfiguration.Default, loaded.Configuration);
        Assert.Equal(path, loaded.FilePath);
    }

    [Fact]
    public void PartialConfigOverridesOnlyWhatItNames()
    {
        var path = WriteConfig("""
            {
              "penalties": { "poreDiameterThreshold": 4.5 },
              "differentialEvolution": { "fixedPopulation": { "populationSizeMultiplier": 8 } },
              "bounds": { "upper": { "ADecompose": 1e9 } }
            }
            """);

        var configuration = RunConfigurationLoader.Load(path).Configuration;
        var defaults = RunConfiguration.Default;

        // Overridden.
        Assert.Equal(4.5, configuration.Penalties.PoreDiameterThreshold);
        Assert.Equal(8, configuration.DifferentialEvolution.FixedPopulation.PopulationSizeMultiplier);
        Assert.Equal(1e9, configuration.Bounds.Upper["ADecompose"]);

        // Inherited: siblings within a touched section, and untouched sections entirely.
        Assert.Equal(defaults.Penalties.PenaltyRate, configuration.Penalties.PenaltyRate);
        Assert.Equal(defaults.Penalties.HeatFluxRatioThreshold, configuration.Penalties.HeatFluxRatioThreshold);
        Assert.Equal(defaults.DifferentialEvolution.FixedPopulation.MaxStagnationStreak,
            configuration.DifferentialEvolution.FixedPopulation.MaxStagnationStreak);
        Assert.Equal(defaults.DifferentialEvolution.Strategy, configuration.DifferentialEvolution.Strategy);
        Assert.Equal(defaults.NelderMead, configuration.NelderMead);
        Assert.Equal(defaults.InputFileName, configuration.InputFileName);

        // Every bound the file did not name keeps its default, including the other side of the one it did.
        Assert.Equal(defaults.Bounds.Lower["ADecompose"], configuration.Bounds.Lower["ADecompose"]);
        Assert.Equal(defaults.Bounds.Upper["DeltaH"], configuration.Bounds.Upper["DeltaH"]);
        Assert.Equal(BoundsProvider.BaseVectorLength, configuration.Bounds.Upper.Count);
    }

    [Fact]
    public void StrategyIsReadAsAName()
    {
        var path = WriteConfig("""{"differentialEvolution": {"strategy": "LShade"}}""");

        Assert.Equal(DifferentialEvolutionStrategy.LShade,
            RunConfigurationLoader.Load(path).Configuration.DifferentialEvolution.Strategy);
    }

    [Fact]
    public void UnknownSettingNameIsRejected()
    {
        var path = WriteConfig("""{"penalties": {"poreDiameterThreshhold": 4.0}}""");

        var exception = Assert.Throws<RunConfigurationException>(() => RunConfigurationLoader.Load(path));
        Assert.Contains("poreDiameterThreshhold", exception.Message);
    }

    [Fact]
    public void UnknownBoundParameterNameIsRejected()
    {
        var path = WriteConfig("""{"bounds": {"lower": {"ADecomposeTypo": 1.0}}}""");

        var exception = Assert.Throws<RunConfigurationException>(() => RunConfigurationLoader.Load(path));
        Assert.Contains("ADecomposeTypo", exception.Message);
    }

    [Fact]
    public void InvertedBoundIsRejected()
    {
        var path = WriteConfig("""{"bounds": {"lower": {"NuInterPocket": 9.0}}}""");

        var exception = Assert.Throws<RunConfigurationException>(() => RunConfigurationLoader.Load(path));
        Assert.Contains("NuInterPocket", exception.Message);
    }

    [Fact]
    public void OutOfRangeValueIsRejected()
    {
        var path = WriteConfig("""{"differentialEvolution": {"crossoverProbability": 1.5}}""");

        Assert.Throws<RunConfigurationException>(() => RunConfigurationLoader.Load(path));
    }

    [Fact]
    public void NelderMeadEnabledWithNoStageIsRejected()
    {
        var path = WriteConfig("""{"nelderMead": {"enabled": true, "finalPolish": false}}""");

        Assert.Throws<RunConfigurationException>(() => RunConfigurationLoader.Load(path));
    }

    [Fact]
    public void MalformedJsonIsRejected()
    {
        var path = WriteConfig("{ not json");

        Assert.Throws<RunConfigurationException>(() => RunConfigurationLoader.Load(path));
    }

    [Fact]
    public void ExplicitlyRequestedFileThatIsMissingIsAnErrorRatherThanAFallback()
    {
        var missing = Path.Combine(_directory, "absent.json");

        var exception = Assert.Throws<RunConfigurationException>(() => RunConfigurationLoader.Load(missing));
        Assert.Contains("absent.json", exception.Message);
    }

    [Fact]
    public void DefaultFileNameIsPickedUpFromTheBaseDirectory()
    {
        var path = Path.Combine(_directory, RunConfigurationLoader.DefaultFileName);
        File.WriteAllText(path, """{"inputFileName": "propellants.0.json"}""");

        var loaded = RunConfigurationLoader.Load(baseDirectory: _directory);

        Assert.Equal(path, loaded.FilePath);
        Assert.Equal("propellants.0.json", loaded.Configuration.InputFileName);
    }

    [Fact]
    public void ResolvedRecordSerialisesTheEffectiveValuesNotTheRequestedOnes()
    {
        var configuration = RunConfiguration.Default;
        var record = new ResolvedRunRecord
        {
            Mode = "optimization",
            ConfigurationSource = "built-in defaults",
            WorkingDirectory = _directory,
            Configuration = configuration,
            Effective = new EffectiveRunValues
            {
                // Deliberately not 32 x 12 and not ProcessorCount - 1: the sidecar must report what the
                // run derived, so a value that cannot be recomputed from the configuration must survive.
                PopulationSize = 384,
                ProcessorsCount = 16,
                MachineProcessorCount = 24,
                Dimensions = BoundsProvider.GroupVectorLength,
                TerminationStrategy = "stagnation streak 5,000 @ rel 1E-06 OR 9 h safety timeout",
                GroupLowerBound = configuration.Bounds.ToGroupLowerBound(),
                GroupUpperBound = configuration.Bounds.ToGroupUpperBound()
            }
        };

        using var document = JsonDocument.Parse(ResolvedRunRecordService.Serialize(record));
        var root = document.RootElement;

        Assert.Equal(1, root.GetProperty("schemaVersion").GetInt32());
        Assert.Equal("optimization", root.GetProperty("mode").GetString());

        var effective = root.GetProperty("effective");
        Assert.Equal(384, effective.GetProperty("populationSize").GetInt32());
        Assert.Equal(16, effective.GetProperty("processorsCount").GetInt32());
        Assert.Equal(24, effective.GetProperty("machineProcessorCount").GetInt32());
        Assert.Equal(BoundsProvider.GroupVectorLength, effective.GetProperty("groupLowerBound").GetArrayLength());

        // The full configuration travels with it, strategy as a readable name.
        var serialised = root.GetProperty("configuration");
        Assert.Equal("propellants.01234.json", serialised.GetProperty("inputFileName").GetString());
        Assert.Equal("Jde", serialised.GetProperty("differentialEvolution").GetProperty("strategy").GetString());
        Assert.Equal(3.0, serialised.GetProperty("penalties").GetProperty("poreDiameterThreshold").GetDouble());
        Assert.Equal(1.0,
            serialised.GetProperty("bounds").GetProperty("lower").GetProperty("ADecompose").GetDouble());
    }

    /// <summary>
    /// The provenance record has to reflect what the optimiser was handed, and the worker count is the
    /// one value that cannot be recomputed from the configuration file alone — it depends on the
    /// machine's core count and on the divisibility rule that walks it down. Building a real plan is the
    /// only way to assert that the recorded number went through that rule.
    /// </summary>
    [Fact]
    public void ResolvedRecordCapturesTheProcessorCountAfterTheReductionRule()
    {
        var propellants = PythonRuntime.ResolveRepositoryPath("data", "propellants.01234.json");
        Assert.True(File.Exists(propellants), $"Test input missing: {propellants}");

        var defaults = RunConfiguration.Default;
        var loaded = new LoadedRunConfiguration
        {
            Configuration = defaults with { InputFileName = propellants },
            SourceDescription = "test",
            FilePath = null
        };

        var plan = GroupScenarioRunner.CreateOptimizationPlan(loaded);
        var effective = plan.Resolved.Effective;

        var expectedPopulation =
            BoundsProvider.GroupVectorLength * defaults.DifferentialEvolution.FixedPopulation.PopulationSizeMultiplier;
        Assert.Equal(384, expectedPopulation);
        Assert.Equal(expectedPopulation, effective.PopulationSize);

        // Recompute the rule independently and require the record to agree with it.
        var expectedProcessors = Math.Max(1, Environment.ProcessorCount - 1);
        for (; expectedProcessors >= defaults.DifferentialEvolution.FixedPopulation.MinProcessorsCount;
             expectedProcessors--)
            if (expectedPopulation % expectedProcessors == 0)
                break;

        Assert.Equal(expectedProcessors, effective.ProcessorsCount);
        Assert.Equal(Environment.ProcessorCount, effective.MachineProcessorCount);

        // The record must agree with what the plan actually handed the optimiser, not merely with the rule.
        Assert.Equal(plan.ProcessorsCount, effective.ProcessorsCount);
        Assert.Equal(plan.PopulationSize, effective.PopulationSize);
        Assert.Equal(plan.Settings.DifferentialEvolutionSettings.PopulationSize, effective.PopulationSize);

        // jDE is a fixed-population strategy, so the L-SHADE-only knobs must be recorded as unused
        // rather than as their configured values.
        Assert.Null(effective.MaxEvaluationNumber);
        Assert.Null(effective.PBestRate);
        Assert.Null(effective.ArchiveSizeRate);
        Assert.Null(effective.MemorySize);

        Assert.Equal("optimization", plan.Resolved.Mode);
        Assert.Equal(BoundsProvider.GetGroupLowerBound(), effective.GroupLowerBound);
    }

    /// <summary>A configured population multiplier must reach both the optimiser and the record.</summary>
    [Fact]
    public void ConfiguredPopulationSizeFlowsThroughToTheOptimiserAndTheRecord()
    {
        var propellants = PythonRuntime.ResolveRepositoryPath("data", "propellants.01234.json");
        var defaults = RunConfiguration.Default;

        var configuration = defaults with
        {
            InputFileName = propellants,
            DifferentialEvolution = defaults.DifferentialEvolution with
            {
                FixedPopulation = defaults.DifferentialEvolution.FixedPopulation with
                {
                    PopulationSizeMultiplier = 4,
                    MinProcessorsCount = 1
                }
            }
        };

        var plan = GroupScenarioRunner.CreateOptimizationPlan(new LoadedRunConfiguration
        {
            Configuration = configuration,
            SourceDescription = "test",
            FilePath = null
        });

        Assert.Equal(BoundsProvider.GroupVectorLength * 4, plan.PopulationSize);
        Assert.Equal(plan.PopulationSize, plan.Resolved.Effective.PopulationSize);
        Assert.Equal(plan.PopulationSize, plan.Settings.DifferentialEvolutionSettings.PopulationSize);
    }

    [Fact]
    public void PenaltyEvaluatorsAreBuiltFromTheConfiguration()
    {
        // The parameterless overload and the explicit-defaults overload must stay the same set, since
        // the optimisation and forward-eval paths must score a point identically.
        var fromDefaults = GroupPenaltyEvaluatorFactory.Build();
        var fromExplicitDefaults = GroupPenaltyEvaluatorFactory.Build(new PenaltyConfiguration());

        Assert.Equal(5, fromDefaults.Length);
        Assert.Equal(
            fromDefaults.Select(evaluator => evaluator.GetType()),
            fromExplicitDefaults.Select(evaluator => evaluator.GetType()));
    }

    private string WriteConfig(string json)
    {
        var path = Path.Combine(_directory, $"config_{Guid.NewGuid():N}.json");
        File.WriteAllText(path, json);

        return path;
    }
}
