using System.Reflection;
using DotNetDifferentialEvolution;
using DotNetDifferentialEvolution.Algorithms.Jde;
using DotNetDifferentialEvolution.ControlParameterProviders;
using DotNetDifferentialEvolution.Interfaces;
using DotNetDifferentialEvolution.MutationStrategies;
using DotNetDifferentialEvolution.TerminationStrategies;
using DotNetOptimization.Abstractions;

namespace ParametricCombustionModel.Optimization.Tests.DependencyContracts;

/// <summary>
/// Characterises what <b>DotNetDifferentialEvolution 4.0.0</b> guarantees about search quality and
/// reproducibility.
///
/// <b>Seed control exists</b>, contrary to what an earlier version of this file asserted. The seam is not
/// named "Seed" and is not on the builder: <c>MutationStrategy</c> takes a <see cref="BaseRandomProvider"/>
/// as its last constructor parameter, <see cref="BaseRandomProvider"/> is abstract, and
/// <c>IControlParameterProvider.GetControlParameters</c> receives the provider as a parameter rather than
/// owning one — so a seeded subclass determines the mutation draws <i>and</i> jDE's F/CR adaptation.
/// Initial population is separately fixable via <c>WithPopulationSampling</c>.
///
/// The practical catch, which is why this matters less than it first appears: <b>reproducibility holds only
/// at <c>UseProcessors(1)</c></b>. With several workers the same seed does not reproduce, because workers
/// draw from the shared provider in whatever order they interleave. The real group run uses
/// <c>ProcessorCount − 1</c>, so production runs remain irreproducible in practice. Both halves are pinned
/// below, since either could change on an upgrade and they have opposite implications.
///
/// Note also that production reaches jDE through <c>WithJde(F, CR)</c>, which bypasses
/// <c>WithMutationStrategy</c> entirely and so has no injection point. Seeding requires reconstructing jDE
/// as <c>WithMutationStrategy(new MutationStrategy(…, provider), new JdeStrategy(…))</c>, and
/// <c>JdeStrategy</c> takes five parameters that <c>WithJde</c> fills with internal defaults — those
/// defaults are not public, so an equivalent reconstruction is not guaranteed without checking them.
/// </summary>
public class DifferentialEvolutionDeterminismContractTests
{
    private const int GenomeSize = 5;
    private const int PopulationSize = 60;
    private const int Generations = 300;

    /// <summary>
    /// The library does actually minimise: on a sphere function with the optimum at the origin,
    /// a short jDE run gets close. Deliberately loose (1e-3) because this run is multi-worker and
    /// unseeded — the configuration production actually uses — so the result varies run to run.
    /// This pins "the optimiser works", not "the optimiser is exact".
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
    /// The randomness seam, pinned. <c>MutationStrategy</c> has a public constructor whose last
    /// parameter is a <see cref="BaseRandomProvider"/>, and <see cref="BaseRandomProvider"/> is abstract
    /// with just <c>Next(int)</c> and <c>NextDouble()</c> — so a seeded subclass can be injected.
    ///
    /// This is the seam an earlier version of this file missed, by searching only for members *named*
    /// "Seed" and only on builder-stage types. The mechanism is neither: it is a constructor parameter
    /// on a strategy. Recorded explicitly so the same search is not repeated.
    /// </summary>
    [Fact]
    public void MutationStrategy_AcceptsAnInjectedRandomProvider()
    {
        var seamConstructor = typeof(MutationStrategy)
            .GetConstructors()
            .SingleOrDefault(c => c.GetParameters()
                .Any(p => typeof(BaseRandomProvider).IsAssignableFrom(p.ParameterType)));

        Assert.True(
            seamConstructor is not null,
            "MutationStrategy no longer has a public constructor accepting a BaseRandomProvider. That "
            + "constructor is the only supported way to make a DE run reproducible — without it, seeding is "
            + "impossible and every strategy-comparison claim reverts to being confounded by seed luck.");

        Assert.True(
            typeof(BaseRandomProvider).IsAbstract,
            "BaseRandomProvider is no longer abstract, so a seeded subclass may no longer be substitutable.");
    }

    /// <summary>
    /// The reason the seam reaches jDE's own adaptation and not merely the mutation draw:
    /// <c>IControlParameterProvider.GetControlParameters</c> takes the provider as a <b>parameter</b>.
    /// The adaptive strategies do not own their randomness — they are handed it — so injecting a seeded
    /// provider into the mutation strategy also determines the F/CR adaptation sequence.
    ///
    /// This is what makes seeding meaningful rather than partial, so it is pinned separately: an upgrade
    /// that gave the adaptive strategies their own internal provider would silently reintroduce
    /// irreproducibility while leaving <see cref="MutationStrategy_AcceptsAnInjectedRandomProvider"/> green.
    /// </summary>
    [Fact]
    public void AdaptiveStrategies_ReceiveTheirRandomProviderExternally()
    {
        var getControlParameters = typeof(IControlParameterProvider).GetMethod("GetControlParameters");

        Assert.True(
            getControlParameters is not null
            && getControlParameters.GetParameters()
                .Any(p => typeof(BaseRandomProvider).IsAssignableFrom(p.ParameterType)),
            "IControlParameterProvider.GetControlParameters no longer receives a BaseRandomProvider. If the "
            + "adaptive strategies now own their randomness internally, seeding the mutation strategy no longer "
            + "makes jDE's F/CR adaptation reproducible, and fixed-seed runs are only partially deterministic.");
    }

    /// <summary>
    /// The contract that actually matters, verified end to end rather than by reflection: with a seeded
    /// provider and a deterministic initial-population sampler, a single-worker jDE run is reproducible —
    /// the same seed gives a bit-identical best fitness, and a different seed does not.
    ///
    /// The second half is what stops this being vacuous. A test that only asserted "same seed, same answer"
    /// would also pass if the run ignored the seed entirely and converged to a constant.
    /// </summary>
    [Fact]
    public async Task SeededSingleWorkerRun_IsReproducible()
    {
        var first = await RunSeededSphereAsync(seed: 42, workerCount: 1);
        var second = await RunSeededSphereAsync(seed: 42, workerCount: 1);
        var different = await RunSeededSphereAsync(seed: 99, workerCount: 1);

        Assert.True(
            first.Fitness.Equals(second.Fitness),
            $"Two single-worker runs with seed 42 disagreed ({first.Fitness:R} vs {second.Fitness:R}). Seeding "
            + "via an injected BaseRandomProvider no longer makes a run reproducible, which removes the only "
            + "basis this project has for controlled optimiser comparisons.");

        Assert.False(
            first.Fitness.Equals(different.Fitness),
            $"Seeds 42 and 99 produced the identical fitness {first.Fitness:R}. The run is not actually consuming "
            + "the injected provider, so the reproducibility above is meaningless — it would hold even if the "
            + "seed were ignored.");
    }

    /// <summary>
    /// The limitation, pinned deliberately: seeding buys reproducibility only at <c>UseProcessors(1)</c>.
    ///
    /// With several workers the same seed does <b>not</b> reproduce, because the workers draw from the shared
    /// provider in whatever order they interleave. This is the practically important half for this project:
    /// the real group run uses <c>ProcessorCount − 1</c> workers, so production runs stay irreproducible even
    /// though the seam exists. Anyone wanting reproducible campaign results must either drop to one worker
    /// (far slower) or partition randomness per worker, which the library does not offer.
    ///
    /// If this test starts failing, the library has made multi-worker runs deterministic — good news worth
    /// acting on, since it would make full-speed reproducible campaigns possible for the first time.
    /// </summary>
    [Fact]
    public async Task SeededMultiWorkerRun_IsNotReproducible()
    {
        var results = new List<double>();
        for (var i = 0; i < 3; i++)
            results.Add((await RunSeededSphereAsync(seed: 42, workerCount: 4)).Fitness);

        Assert.True(
            results.Distinct().Count() > 1,
            $"Three 4-worker runs with seed 42 all returned {results[0]:R}. Multi-worker runs have become "
            + "deterministic under a seeded provider. That is an improvement, not a defect: update this test "
            + "and note that reproducible full-speed campaigns are now possible.");
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

    /// <summary>
    /// A jDE run with every randomness source pinned: a seeded provider driving the mutation strategy (and,
    /// through it, jDE's control-parameter adaptation), plus a seeded initial-population sampler.
    ///
    /// jDE is reconstructed manually rather than via <c>WithJde</c>, because that shortcut skips
    /// <c>WithMutationStrategy</c> and therefore offers nowhere to inject the provider. The
    /// <c>JdeStrategy</c> arguments here are this test's own, not a claim about what <c>WithJde</c> uses
    /// internally — these tests characterise the seam, not an equivalence with the shortcut.
    /// </summary>
    private static async Task<(double Fitness, double[] Genes)> RunSeededSphereAsync(int seed, int workerCount)
    {
        var lowerBound = Enumerable.Repeat(-5.0, GenomeSize).ToArray();
        var upperBound = Enumerable.Repeat(5.0, GenomeSize).ToArray();

        var mutationStrategy = new MutationStrategy(
            mutationForce: 0.5,
            crossoverProbability: 0.9,
            populationSize: PopulationSize,
            lowerBound: lowerBound,
            upperBound: upperBound,
            randomProvider: new SeededRandomProvider(seed));

        var jde = new JdeStrategy(
            populationSize: PopulationSize,
            initialMutationForce: 0.5,
            initialCrossoverProbability: 0.9,
            fAdaptationProbability: 0.1,
            crAdaptationProbability: 0.1,
            minMutationForce: 0.1,
            mutationForceRange: 0.9);

        using var de = DifferentialEvolutionBuilder
            .ForFunction(new RecordingSphereEvaluator())
            .WithBounds(lowerBound, upperBound)
            .WithPopulationSize(PopulationSize)
            .WithPopulationSampling(new SeededPopulationSampler(seed, -5.0, 5.0))
            .WithMutationStrategy(mutationStrategy, jde)
            .WithDefaultSelectionStrategy()
            .WithTerminationCondition(new LimitGenerationNumberTerminationStrategy(Generations))
            .UseProcessors(workerCount)
            .Build();

        var population = await de.RunAsync();
        population.MoveCursorToBestIndividual();

        return (population.IndividualCursor.FitnessFunctionValue,
                population.IndividualCursor.Genes.ToArray());
    }

    /// <summary>
    /// A <see cref="BaseRandomProvider"/> backed by a seeded <see cref="Random"/>. Locked because the
    /// library shares one provider across workers — without the lock, multi-worker runs would corrupt
    /// <see cref="Random"/>'s internal state rather than merely being non-reproducible, which would confuse
    /// the limitation this file pins with an outright defect.
    /// </summary>
    private sealed class SeededRandomProvider(int seed) : BaseRandomProvider
    {
        private readonly Random _random = new(seed);

        public override int Next(int maxValue)
        {
            lock (_random) return _random.Next(maxValue);
        }

        public override double NextDouble()
        {
            lock (_random) return _random.NextDouble();
        }
    }

    /// <summary>Fills the initial population from a seeded generator, so it is not a second uncontrolled source.</summary>
    private sealed class SeededPopulationSampler(int seed, double lowerBound, double upperBound)
        : IPopulationSamplingMaker
    {
        private readonly Random _random = new(seed);

        public void SamplePopulation(Span<double> population)
        {
            lock (_random)
            {
                for (var i = 0; i < population.Length; i++)
                    population[i] = lowerBound + _random.NextDouble() * (upperBound - lowerBound);
            }
        }
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
