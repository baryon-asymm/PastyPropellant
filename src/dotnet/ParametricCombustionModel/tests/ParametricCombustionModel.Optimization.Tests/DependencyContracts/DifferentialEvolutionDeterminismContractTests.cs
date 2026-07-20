using System.Reflection;
using DotNetDifferentialEvolution;
using DotNetDifferentialEvolution.Interfaces;
using DotNetDifferentialEvolution.TerminationStrategies;
using DotNetOptimization.Abstractions;

namespace ParametricCombustionModel.Optimization.Tests.DependencyContracts;

/// <summary>
/// Characterises what <b>DotNetDifferentialEvolution 4.0.0</b> guarantees about search quality and
/// reproducibility.
///
/// The headline finding, recorded here so it is not rediscovered by accident: <b>the library exposes
/// no seed control.</b> There is no <c>WithSeed</c>, no way to inject a <c>BaseRandomProvider</c>
/// through the builder, and <c>RandomProvider</c> has only a parameterless constructor. A DE run is
/// therefore <b>not reproducible</b>, and any claim comparing strategies (jDE vs JADE vs SHADE) on
/// single runs is confounded by seed variation. See <see cref="Library_ExposesNoSeedControl"/>.
/// </summary>
public class DifferentialEvolutionDeterminismContractTests
{
    private const int GenomeSize = 5;
    private const int PopulationSize = 60;
    private const int Generations = 300;

    /// <summary>
    /// The library does actually minimise: on a sphere function with the optimum at the origin,
    /// a short jDE run gets close. Deliberately loose (1e-3) because without seed control the
    /// result varies run to run — this pins "the optimiser works", not "the optimiser is exact".
    /// </summary>
    [Fact]
    public async Task Jde_MinimisesASphereFunctionTowardsItsKnownOptimum()
    {
        var (fitness, genes) = await RunSphereAsync(workerCount: 2);

        Assert.True(
            fitness < 1e-3,
            $"jDE failed to minimise a 5-D sphere function to within 1e-3 after {Generations} generations "
            + $"(best fitness {fitness:E3}). The sphere is unimodal, separable and trivially easy; if the library "
            + "cannot solve it, the search itself is broken and no result from the real combustion optimisation "
            + "can be trusted. Check for a regression in the mutation/selection strategies before upgrading.");

        Assert.All(
            genes,
            g => Assert.True(
                Math.Abs(g) < 0.05,
                $"A converged sphere solution should sit near the origin, but a gene was {g:F4}. "
                + "A low fitness with far-from-optimal genes would suggest the reported best individual and the "
                + "reported best fitness have come out of step inside the library."));
    }

    /// <summary>
    /// Tripwire, not a behavioural requirement. Asserts that the library still has no seed API.
    ///
    /// If this test FAILS, that is good news: an upgrade has added seed control, and the project
    /// should now build the fixed-seed harness it has never had — which would let optimiser-comparison
    /// claims (jDE beating SHADE/JADE on the group run) rest on controlled repeats instead of single
    /// unseeded runs. Delete this test and write that harness.
    /// </summary>
    [Fact]
    public void Library_ExposesNoSeedControl()
    {
        var deAssembly = typeof(DifferentialEvolutionBuilder).Assembly;
        var abstractionsAssembly = typeof(BaseRandomProvider).Assembly;

        var seedMembers = new[] { deAssembly, abstractionsAssembly }
            .SelectMany(a => a.GetExportedTypes())
            .SelectMany(t => t.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            .Where(m => m.Name.Contains("Seed", StringComparison.OrdinalIgnoreCase))
            .Select(m => $"{m.DeclaringType?.FullName}.{m.Name}")
            .Distinct()
            .ToArray();

        Assert.True(
            seedMembers.Length == 0,
            "DotNetDifferentialEvolution now exposes seed-related member(s): "
            + string.Join(", ", seedMembers)
            + ".\n\nThis test failing is GOOD NEWS. Until now the library had no seed control, so DE runs were "
            + "irreproducible and strategy-comparison results could not be separated from seed luck. Replace this "
            + "tripwire with a real fixed-seed harness: same seed -> identical best genes and fitness across runs.");

        // The concrete provider is unseedable: parameterless constructor only.
        var providerConstructors = typeof(RandomProvider).GetConstructors();
        Assert.True(
            providerConstructors.All(c => c.GetParameters().Length == 0),
            "DotNetOptimization.Abstractions.RandomProvider gained a parameterised constructor — most likely a "
            + "seed. See the note above: build the fixed-seed harness.");

        // And there is no builder stage that accepts a random provider.
        var builderStages = deAssembly.GetExportedTypes()
            .Where(t => t.Name.Contains("Builder", StringComparison.Ordinal)
                        || (t.IsInterface && t.Name.EndsWith("Required", StringComparison.Ordinal)));

        var providerInjectionPoints = builderStages
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            .Where(m => m.GetParameters().Any(p => typeof(BaseRandomProvider).IsAssignableFrom(p.ParameterType)))
            .Select(m => $"{m.DeclaringType?.Name}.{m.Name}")
            .Distinct()
            .ToArray();

        Assert.True(
            providerInjectionPoints.Length == 0,
            "The DE builder now accepts a BaseRandomProvider via: "
            + string.Join(", ", providerInjectionPoints)
            + ". A custom provider can carry a seed, so reproducible runs are now possible — build the fixed-seed "
            + "harness described above.");
    }

    /// <summary>
    /// The one deterministic hook the library does offer:
    /// <c>WithPopulationSampling(IPopulationSamplingMaker)</c> lets us supply the initial population
    /// ourselves. Pinned because it is the only foothold any future reproducibility work has —
    /// it fixes the search's starting point even though mutation/crossover randomness stays uncontrolled.
    /// </summary>
    [Fact]
    public async Task WithPopulationSampling_LetsTheCallerSupplyTheInitialPopulationDeterministically()
    {
        var sampler = new ConstantSamplingMaker(value: 1.5);
        var evaluator = new RecordingSphereEvaluator();

        var lowerBound = Enumerable.Repeat(-5.0, GenomeSize).ToArray();
        var upperBound = Enumerable.Repeat(5.0, GenomeSize).ToArray();

        using var de = DifferentialEvolutionBuilder
            .ForFunction(evaluator)
            .WithBounds(lowerBound, upperBound)
            .WithPopulationSize(PopulationSize)
            .WithPopulationSampling(sampler)
            .WithJde(0.1, 0.1)
            .WithTerminationCondition(new LimitGenerationNumberTerminationStrategy(1))
            .UseProcessors(2)
            .Build();

        await de.RunAsync();

        Assert.True(
            sampler.SampleCallCount == 1,
            $"Expected the library to call IPopulationSamplingMaker.SamplePopulation exactly once, but it was "
            + $"called {sampler.SampleCallCount} time(s). This hook is the only deterministic entry point the "
            + "library offers; if the library stops honouring it, there is no way at all to control a run's "
            + "starting state.");

        Assert.Equal(PopulationSize * GenomeSize, sampler.LastBufferLength);

        // Every individual started at the constant we supplied, so the first-generation evaluations
        // must have seen exactly that value: sphere(1.5 * ones(5)) = 5 * 2.25 = 11.25.
        Assert.Contains(
            11.25,
            evaluator.ObservedFitnessValues);
    }

    private static async Task<(double Fitness, double[] Genes)> RunSphereAsync(int workerCount)
    {
        var lowerBound = Enumerable.Repeat(-5.0, GenomeSize).ToArray();
        var upperBound = Enumerable.Repeat(5.0, GenomeSize).ToArray();

        using var de = DifferentialEvolutionBuilder
            .ForFunction(new RecordingSphereEvaluator())
            .WithBounds(lowerBound, upperBound)
            .WithPopulationSize(PopulationSize)
            .WithUniformPopulationSampling()
            .WithJde(0.1, 0.1)
            .WithTerminationCondition(new LimitGenerationNumberTerminationStrategy(Generations))
            .UseProcessors(workerCount)
            .Build();

        var population = await de.RunAsync();
        population.MoveCursorToBestIndividual();

        return (population.IndividualCursor.FitnessFunctionValue,
                population.IndividualCursor.Genes.ToArray());
    }

    private sealed class RecordingSphereEvaluator : IFitnessFunctionEvaluator
    {
        private readonly System.Collections.Concurrent.ConcurrentBag<double> _observed = [];

        public IReadOnlyCollection<double> ObservedFitnessValues => _observed;

        public double Evaluate(ReadOnlySpan<double> genes) => Record(genes);

        public double Evaluate(int workerIndex, ReadOnlySpan<double> genes) => Record(genes);

        private double Record(ReadOnlySpan<double> genes)
        {
            var sum = 0.0;
            foreach (var g in genes)
                sum += g * g;
            _observed.Add(sum);
            return sum;
        }
    }

    private sealed class ConstantSamplingMaker(double value) : IPopulationSamplingMaker
    {
        public int SampleCallCount { get; private set; }

        public int LastBufferLength { get; private set; }

        public void SamplePopulation(Span<double> population)
        {
            SampleCallCount++;
            LastBufferLength = population.Length;
            population.Fill(value);
        }
    }
}
