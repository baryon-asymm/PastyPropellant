using System.Reflection;
using DotNetDifferentialEvolution;
using DotNetDifferentialEvolution.Interfaces;
using DotNetDifferentialEvolution.MutationStrategies;
using DotNetDifferentialEvolution.TerminationStrategies;
using DotNetOptimization.Abstractions;

namespace ParametricCombustionModel.Optimization.Tests.DependencyContracts;

/// <summary>
/// Characterises what <b>DotNetDifferentialEvolution 5.1.0</b> guarantees about search quality and
/// reproducibility.
///
/// <b>Full-speed reproducibility now exists.</b> Under 4.0.0 the only seeding seam was a
/// <c>BaseRandomProvider</c> injected into <c>MutationStrategy</c>'s constructor, and it bought
/// reproducibility only at <c>UseProcessors(1)</c> — every worker drew from that one shared generator, so a
/// multi-worker run depended on how the threads interleaved. The production group run uses
/// <c>ProcessorCount − 1</c> workers, so in practice nothing this project ran was ever exactly repeatable.
///
/// 5.x replaces that with <c>DifferentialEvolutionBuilder.WithSeed(int)</c>: the engine derives one
/// generator per worker from the seed, and each individual is built, evaluated and selected end to end by
/// one worker, so nothing depends on interleaving. A seeded run is bit-for-bit repeatable at any worker
/// count. The old constructor still compiles but is <c>[Obsolete]</c> and no longer seeds anything — which
/// is how this upgrade was detected, and why the tests that pinned it are gone rather than adjusted.
///
/// The catch that replaces the old one, and the reason <c>run_configuration.resolved.json</c> records the
/// seed next to the effective worker count: individual <i>i</i> draws from worker <i>i mod W</i>'s stream,
/// so W is part of the seed's meaning. A seed reproduces a run <b>only at the worker count it ran on</b>,
/// and that count is derived from <c>Environment.ProcessorCount</c> — so a seed quoted on its own does not
/// identify a run.
/// </summary>
public class DifferentialEvolutionDeterminismContractTests
{
    private const int GenomeSize = 5;
    private const int PopulationSize = 60;
    private const int Generations = 300;

    /// <summary>Worker counts to exercise. <see cref="PopulationSize"/> is divisible by each.</summary>
    public static TheoryData<int> WorkerCounts => new() { 1, 2, 4 };

    /// <summary>
    /// The library does actually minimise: on a sphere function with the optimum at the origin,
    /// a short jDE run gets close. Deliberately loose (1e-3) because this run is multi-worker and
    /// unseeded — the configuration production actually uses — so the result varies run to run.
    /// This pins "the optimiser works", not "the optimiser is exact".
    /// </summary>
    [Fact]
    public async Task Jde_MinimisesASphereFunctionTowardsItsKnownOptimum()
    {
        var (fitness, genes) = await RunSphereAsync(workerCount: 2, seed: null);

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
    /// The contract this upgrade was made for: <c>WithSeed</c> makes a run bit-for-bit repeatable at
    /// <b>every</b> worker count, not just at one.
    ///
    /// <para>The second assertion is what stops this being vacuous — a test that only checked "same seed,
    /// same answer" would also pass if the run ignored the seed and converged to a constant. The runs go
    /// through <c>WithJde</c>, so this also covers the adaptive machinery: jDE self-adapts F and CR per
    /// individual, and a bit-identical result means that adaptation sequence is seeded too, not merely the
    /// mutation draw.</para>
    ///
    /// <para>If this starts failing, exactly-replayable campaign results are gone and every comparison
    /// between runs reverts to being confounded by seed luck. Check whether the library changed how it
    /// consumes randomness — it documents that a seed is reproducible only within a minor version.</para>
    /// </summary>
    [Theory]
    [MemberData(nameof(WorkerCounts))]
    public async Task SeededRun_IsReproducibleAtEveryWorkerCount(int workerCount)
    {
        var first = await RunSphereAsync(workerCount, seed: 42);
        var second = await RunSphereAsync(workerCount, seed: 42);
        var different = await RunSphereAsync(workerCount, seed: 99);

        Assert.True(
            first.Fitness.Equals(second.Fitness) && first.Genes.SequenceEqual(second.Genes),
            $"Two {workerCount}-worker runs with seed 42 disagreed ({first.Fitness:R} vs {second.Fitness:R}). "
            + "WithSeed no longer makes a run reproducible, which removes the basis for both controlled "
            + "optimiser comparisons and exactly-replayable campaign results.");

        Assert.False(
            first.Fitness.Equals(different.Fitness),
            $"Seeds 42 and 99 produced the identical fitness {first.Fitness:R} at {workerCount} worker(s). The run "
            + "is not actually consuming the seed, so the reproducibility above is meaningless — it would hold "
            + "even if the seed were ignored entirely.");
    }

    /// <summary>
    /// The limitation that replaces 4.0.0's: a seeded run is reproducible but <b>not portable across worker
    /// counts</b>. Individual <i>i</i> draws from worker <i>i mod W</i>'s stream, so W is part of what the
    /// seed means.
    ///
    /// <para>Pinned because the production worker count comes from <c>Environment.ProcessorCount</c> and is
    /// then reduced until it divides the population — it is machine-dependent. This is the whole reason
    /// <c>EffectiveRunValues</c> records <c>Seed</c> and <c>ProcessorsCount</c> together; if this test ever
    /// goes green, that pairing has become unnecessary and a seed alone would identify a run.</para>
    /// </summary>
    [Fact]
    public async Task SeededRun_IsNotPortableAcrossWorkerCounts()
    {
        var twoWorkers = await RunSphereAsync(workerCount: 2, seed: 42);
        var fourWorkers = await RunSphereAsync(workerCount: 4, seed: 42);

        Assert.False(
            twoWorkers.Fitness.Equals(fourWorkers.Fitness),
            $"Seed 42 gave the identical fitness {twoWorkers.Fitness:R} at 2 and at 4 workers. The library has made "
            + "a seeded run independent of the worker count. That is an improvement, not a defect: a seed would "
            + "then identify a run on its own, and the resolved run record would no longer need to pair it with "
            + "the effective processor count.");
    }

    /// <summary>
    /// The default is still stochastic: without <c>WithSeed</c> two runs must differ.
    ///
    /// <para>Guards against the mirror image of everything above. A library that quietly acquired a fixed
    /// default seed would make every run in a repeated campaign identical, which reads as spectacular
    /// convergence stability rather than as a defect.</para>
    /// </summary>
    [Fact]
    public async Task UnseededRuns_AreNotReproducible()
    {
        var first = await RunSphereAsync(workerCount: 4, seed: null);
        var second = await RunSphereAsync(workerCount: 4, seed: null);

        Assert.False(
            first.Fitness.Equals(second.Fitness),
            $"Two unseeded 4-worker runs both returned {first.Fitness:R}. An unseeded run has become "
            + "deterministic, so repeated campaigns no longer sample different searches — any spread measured "
            + "across them would be an artefact rather than a property of the optimiser.");
    }

    /// <summary>
    /// The 4.0.0 seeding seam is dead, pinned so nobody restores it from this file's own history.
    ///
    /// <para><c>MutationStrategy</c>'s <c>BaseRandomProvider</c> constructor still compiles, so code written
    /// against 4.0.0 keeps building — but the engine now supplies one provider per worker through
    /// <c>MutationContext</c> and the injected instance is not consulted. Code that seeds through it does
    /// not fail to compile; it silently stops being a seeded run.</para>
    /// </summary>
    [Fact]
    public void TheLegacyRandomProviderConstructor_IsObsoleteAndNoLongerSeedsARun()
    {
        var legacySeam = typeof(MutationStrategy)
            .GetConstructors()
            .SingleOrDefault(c => c.GetParameters()
                .Any(p => typeof(BaseRandomProvider).IsAssignableFrom(p.ParameterType)));

        if (legacySeam is null)
            return; // Removed outright, which is unambiguous. The successor seam is covered above.

        Assert.True(
            legacySeam.GetCustomAttribute<ObsoleteAttribute>() is not null,
            "MutationStrategy's BaseRandomProvider constructor is no longer marked [Obsolete]. Under 5.x the "
            + "engine supplies a provider per worker and this parameter is ignored, so an un-obsoleted "
            + "constructor invites seeding through a seam that silently does nothing. Establish what the library "
            + "now does with it before trusting any run seeded that way — and prefer WithSeed either way.");
    }

    /// <summary>
    /// <c>WithPopulationSampling(IPopulationSamplingMaker)</c> still lets us supply the initial population
    /// ourselves. Less load-bearing than it was under 4.0.0 — <c>WithSeed</c> now covers the initial
    /// population too — but it remains the only way to start a search from a chosen point rather than from
    /// a random one.
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
            + $"called {sampler.SampleCallCount} time(s). This hook is how a run's starting state is chosen; if the "
            + "library stops honouring it, a search can only ever start from a random population.");

        Assert.Equal(PopulationSize * GenomeSize, sampler.LastBufferLength);

        // Every individual started at the constant we supplied, so the first-generation evaluations
        // must have seen exactly that value: sphere(1.5 * ones(5)) = 5 * 2.25 = 11.25.
        Assert.Contains(
            11.25,
            evaluator.ObservedFitnessValues);
    }

    /// <summary>
    /// One jDE sphere run. <paramref name="seed"/> is the only difference between a reproducible run and
    /// the historical stochastic one — deliberately the single switch, so the tests above differ from each
    /// other in nothing else.
    /// </summary>
    private static async Task<(double Fitness, double[] Genes)> RunSphereAsync(int workerCount, int? seed)
    {
        var lowerBound = Enumerable.Repeat(-5.0, GenomeSize).ToArray();
        var upperBound = Enumerable.Repeat(5.0, GenomeSize).ToArray();

        var builder = DifferentialEvolutionBuilder
            .ForFunction(new RecordingSphereEvaluator())
            .WithBounds(lowerBound, upperBound)
            .WithPopulationSize(PopulationSize)
            .WithUniformPopulationSampling()
            .WithJde(0.1, 0.1)
            .WithTerminationCondition(new LimitGenerationNumberTerminationStrategy(Generations))
            .UseProcessors(workerCount);

        if (seed.HasValue)
            builder = builder.WithSeed(seed.Value);

        using var de = builder.Build();

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
