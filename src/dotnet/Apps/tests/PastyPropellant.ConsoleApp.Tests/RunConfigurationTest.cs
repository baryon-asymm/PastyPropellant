using System.Text.Json;
using ParametricCombustionModel.Computation.Models.KnownParams;
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

        // Unseeded by default. The seed exists only because DotNetDifferentialEvolution 5.x can honour one;
        // defaulting it to a value would silently turn every campaign into a replay of the same search,
        // which is the opposite of what the historical configuration did.
        Assert.Null(de.Seed);

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

        // The model constants default to the historical hardcoded physics: 1300 K. A different default
        // here would silently change every number a configuration-free run produces, which is exactly what
        // this whole defaults contract exists to prevent.
        var model = configuration.Model;
        Assert.Equal(1300.0, model.MetalMeltingTemperatureKelvins);

        var constants = model.ToModelConstants();
        Assert.Equal(1300.0, constants.MetalMeltingTemperatureKelvins);

        // Skeleton coverage defaults to the shipped per-propellant polynomials. The kinetic closure is
        // opt-in for the same reason as everything else in this section: switching it silently would
        // change every computed number of a configuration-free run.
        var coverage = model.SkeletonSurfaceFraction;
        Assert.Equal(SkeletonSurfaceFractionMode.Polynomial, coverage.Mode);
        Assert.Equal(SkeletonSurfaceFractionMode.Polynomial, constants.SkeletonSurfaceFraction.Mode);
        Assert.Null(constants.SkeletonSurfaceFraction.EquilibriumTable);

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

    /// <summary>
    /// A configured seed must reach the optimiser and the provenance record, and it must be recorded
    /// alongside the effective worker count.
    ///
    /// <para>The pairing is the point. DotNetDifferentialEvolution 5.x derives one random stream per worker,
    /// so individual <i>i</i> draws from worker <i>i mod W</i>'s stream and the same seed under a different W
    /// is a different search. W here is machine-dependent (<c>ProcessorCount − 1</c>, reduced until it
    /// divides the population), so a record carrying the seed without the worker count would look like it
    /// pins the run while pinning only half of it.</para>
    /// </summary>
    [Fact]
    public void ConfiguredSeedFlowsThroughToTheOptimiserAndIsRecordedWithTheWorkerCount()
    {
        var propellants = PythonRuntime.ResolveRepositoryPath("data", "propellants.01234.json");
        var defaults = RunConfiguration.Default;

        var plan = GroupScenarioRunner.CreateOptimizationPlan(new LoadedRunConfiguration
        {
            Configuration = defaults with
            {
                InputFileName = propellants,
                DifferentialEvolution = defaults.DifferentialEvolution with { Seed = 20260729 }
            },
            SourceDescription = "test",
            FilePath = null
        });

        Assert.Equal(20260729, plan.Settings.DifferentialEvolutionSettings.Seed);
        Assert.Equal(20260729, plan.Resolved.Effective.Seed);
        Assert.Equal(plan.ProcessorsCount, plan.Resolved.Effective.ProcessorsCount);
    }

    /// <summary>An unseeded configuration must stay unseeded all the way down — no default seed anywhere.</summary>
    [Fact]
    public void AnUnseededConfigurationReachesTheOptimiserUnseeded()
    {
        var propellants = PythonRuntime.ResolveRepositoryPath("data", "propellants.01234.json");

        var plan = GroupScenarioRunner.CreateOptimizationPlan(new LoadedRunConfiguration
        {
            Configuration = RunConfiguration.Default with { InputFileName = propellants },
            SourceDescription = "test",
            FilePath = null
        });

        Assert.Null(plan.Settings.DifferentialEvolutionSettings.Seed);
        Assert.Null(plan.Resolved.Effective.Seed);
    }

    /// <summary>
    /// A kinetic skeleton-coverage configuration must survive the JSON round trip and arrive at the
    /// context builders intact.
    ///
    /// <para>The failure mode guarded here is a configuration that <em>parses</em> but does not
    /// <em>reach</em> the model: <c>f_s</c> weighs the metal and skeleton-flame fluxes against the
    /// out-skeleton flux, so a run that silently kept the polynomial while its sidecar advertised the
    /// kinetic closure would report a model it never solved.</para>
    /// </summary>
    [Fact]
    public void AnEquilibriumCoverageConfigurationReachesTheModelConstants()
    {
        var table = Path.Combine(_directory, "coverage.json");
        File.WriteAllText(table, """
        {
          "surfaceTemperaturesKelvins": [600.0, 900.0],
          "propellants": [
            { "name": "Bas_0", "frames": [ { "pressure": 1000000.0, "coverages": [0.25, 0.45] } ] }
          ]
        }
        """);

        var path = Path.Combine(_directory, RunConfigurationLoader.DefaultFileName);
        File.WriteAllText(path, $$"""
        {
          "model": {
            "skeletonSurfaceFraction": {
              "mode": "EquilibriumCarbon",
              "equilibriumTableFile": {{System.Text.Json.JsonSerializer.Serialize(table)}}
            }
          }
        }
        """);

        var loaded = RunConfigurationLoader.Load(baseDirectory: _directory);
        var coverage = loaded.Configuration.Model.SkeletonSurfaceFraction;

        Assert.Equal(SkeletonSurfaceFractionMode.EquilibriumCarbon, coverage.Mode);
        Assert.Equal(table, coverage.EquilibriumTableFile);

        var constants = loaded.Configuration.Model.ToModelConstants();
        Assert.Equal(SkeletonSurfaceFractionMode.EquilibriumCarbon, constants.SkeletonSurfaceFraction.Mode);

        // The table has to be loaded, not merely named: coverage is what weighs the metal and
        // skeleton-flame fluxes against the out-skeleton flux, so a run whose sidecar advertised the
        // equilibrium closure while the solver kept the polynomial would report a model it never solved.
        var curve = constants.SkeletonSurfaceFraction.EquilibriumTable!.CurveFor("Bas_0", 1e6);
        Assert.Equal(0.25, curve.At(600.0));
        Assert.Equal(0.45, curve.At(900.0));
        Assert.Equal(0.35, curve.At(750.0), 12);
    }

    /// <summary>
    /// Selecting the equilibrium closure without a readable table fails at startup rather than at the
    /// first context build, where it would surface as an opaque mid-run exception hours in.
    /// </summary>
    [Fact]
    public void EquilibriumCoverageWithoutATableIsRejected()
    {
        var path = Path.Combine(_directory, RunConfigurationLoader.DefaultFileName);
        File.WriteAllText(path, """
        {
          "model": {
            "skeletonSurfaceFraction": {
              "mode": "EquilibriumCarbon",
              "equilibriumTableFile": "definitely-not-here.json"
            }
          }
        }
        """);

        var error = Assert.Throws<RunConfigurationException>(
            () => RunConfigurationLoader.Load(baseDirectory: _directory));

        Assert.Contains("definitely-not-here.json", error.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// The kinetic closure can be selected with nothing else set, and its constants can be overridden one
    /// at a time.
    ///
    /// <para>Selecting it must need no companion file — unlike the equilibrium closure it reads only the
    /// recipe — because the first thing anyone will do is flip the mode and run.</para>
    /// </summary>
    [Fact]
    public void KineticCoverageIsSelectableAndPartiallyOverridable()
    {
        var path = Path.Combine(_directory, RunConfigurationLoader.DefaultFileName);
        File.WriteAllText(path, """
        {
          "model": {
            "skeletonSurfaceFraction": {
              "mode": "KineticCoverage",
              "kinetic": { "pressureOrder": 0.95 }
            }
          }
        }
        """);

        var loaded = RunConfigurationLoader.Load(baseDirectory: _directory);
        var coverage = loaded.Configuration.Model.SkeletonSurfaceFraction;

        Assert.Equal(SkeletonSurfaceFractionMode.KineticCoverage, coverage.Mode);
        Assert.Equal(0.95, coverage.Kinetic.PressureOrder);
        Assert.Equal(KineticCoverageSettings.Default.BinderChannel, coverage.Kinetic.BinderChannel);
        Assert.Equal(
            KineticCoverageSettings.Default.FineOxidiserChannel, coverage.Kinetic.FineOxidiserChannel);

        var constants = loaded.Configuration.Model.ToModelConstants();
        Assert.Equal(SkeletonSurfaceFractionMode.KineticCoverage, constants.SkeletonSurfaceFraction.Mode);
        Assert.Equal(0.95, constants.SkeletonSurfaceFraction.Kinetic.PressureOrder);
        Assert.Null(constants.SkeletonSurfaceFraction.EquilibriumTable);
    }

    /// <summary>
    /// A broken kinetic constant is rejected at startup even when another closure is in force, so it
    /// cannot lie dormant until someone switches the mode — the rule the DE strategy sub-records follow.
    /// </summary>
    [Fact]
    public void BrokenKineticConstantsAreRejectedUnderEveryMode()
    {
        var path = Path.Combine(_directory, RunConfigurationLoader.DefaultFileName);
        File.WriteAllText(path, """
        {
          "model": {
            "skeletonSurfaceFraction": {
              "mode": "Polynomial",
              "kinetic": { "fineOxidiserChannel": -2.0 }
            }
          }
        }
        """);

        var error = Assert.Throws<RunConfigurationException>(
            () => RunConfigurationLoader.Load(baseDirectory: _directory));

        Assert.Contains("fineOxidiserChannel", error.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Configured model constants must reach the optimiser — both the search path and the forward-eval
    /// replay, which have to score a point under the same physics or a replayed vector means nothing.
    ///
    /// <para>These constants used to be <c>const</c> in source. The failure mode this test guards is not a
    /// wrong number but a <b>silently unused</b> one: a configuration that says 2300 K while the run
    /// computes 1300 K would produce a report stating a temperature the model never used, which is worse
    /// than the hardcoded value it replaced.</para>
    /// </summary>
    [Fact]
    public void ConfiguredModelConstantsReachBothTheSearchAndTheForwardEvalPath()
    {
        var propellants = PythonRuntime.ResolveRepositoryPath("data", "propellants.01234.json");
        var defaults = RunConfiguration.Default;

        var loaded = new LoadedRunConfiguration
        {
            Configuration = defaults with
            {
                InputFileName = propellants,
                Model = defaults.Model with
                {
                    MetalMeltingTemperatureKelvins = 2300.0
                }
            },
            SourceDescription = "test",
            FilePath = null
        };

        var optimisation = GroupScenarioRunner.CreateOptimizationPlan(loaded);
        Assert.Equal(2300.0, optimisation.Settings.ModelConstants.MetalMeltingTemperatureKelvins);

        var forwardEval = GroupScenarioRunner.CreateForwardEvalPlan(loaded);
        Assert.Equal(2300.0, forwardEval.Settings.ModelConstants.MetalMeltingTemperatureKelvins);

        // And they must be in the provenance record, since that is the only reason they are configuration
        // rather than source constants.
        Assert.Equal(2300.0, optimisation.Resolved.Configuration.Model.MetalMeltingTemperatureKelvins);
    }

    /// <summary>A non-positive or non-finite melting temperature must be rejected at startup.</summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    public void InvalidModelConstantsAreRejected(double meltingTemperature)
    {
        var defaults = RunConfiguration.Default;
        var configuration = defaults with
        {
            Model = defaults.Model with
            {
                MetalMeltingTemperatureKelvins = meltingTemperature
            }
        };

        Assert.Throws<RunConfigurationException>(() => configuration.Validate());
    }

    /// <summary>
    /// The in-loop memetic refiner must be refused while the adapter package is built against
    /// DotNetDifferentialEvolution 4.0.0.
    ///
    /// <para>Without this guard the configuration is accepted, the search starts, and the run dies with a
    /// <c>MissingMethodException</c> on the <c>EveryNGenerations</c>-th generation — hours in, with nothing
    /// written. The failure has to happen here, while building the plan, or not at all.</para>
    /// </summary>
    [Fact]
    public void MemeticInLoopIsRejectedWhileTheAdapterTargetsTheOldLibrary()
    {
        var propellants = PythonRuntime.ResolveRepositoryPath("data", "propellants.01234.json");
        var defaults = RunConfiguration.Default;

        var exception = Assert.Throws<InvalidOperationException>(() =>
            GroupScenarioRunner.CreateOptimizationPlan(new LoadedRunConfiguration
            {
                Configuration = defaults with
                {
                    InputFileName = propellants,
                    NelderMead = defaults.NelderMead with { MemeticInLoop = true }
                },
                SourceDescription = "test",
                FilePath = null
            }));

        Assert.Contains("MemeticInLoop", exception.Message);
        Assert.Contains("DotNetNelderMead.DifferentialEvolution", exception.Message);
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
