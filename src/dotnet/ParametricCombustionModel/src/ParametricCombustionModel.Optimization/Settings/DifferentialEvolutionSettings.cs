using System.Collections.ObjectModel;
using DotNetDifferentialEvolution.Interfaces;
using DotNetDifferentialEvolution.TerminationStrategies.Interfaces;

namespace ParametricCombustionModel.Optimization.Settings;

public record DifferentialEvolutionSettings
{
    public ReadOnlyCollection<double> LowerBound { get; init; }

    public ReadOnlyCollection<double> UpperBound { get; init; }

    public int PopulationSize { get; init; }

    public ITerminationStrategy TerminationStrategy { get; init; }

    public IPopulationUpdatedHandler? PopulationUpdatedHandler { get; init; }

    /// <summary>Selected DE variant. Defaults to <see cref="DifferentialEvolutionStrategy.Shade"/>.</summary>
    public DifferentialEvolutionStrategy Strategy { get; init; }

    /// <summary>Constant F (Classic) or initial F (jDE). Ignored by JADE/SHADE/L-SHADE.</summary>
    public double MutationForce { get; init; }

    /// <summary>Constant CR (Classic) or initial CR (jDE). Ignored by JADE/SHADE/L-SHADE.</summary>
    public double CrossoverProbability { get; init; }

    /// <summary>p in current-to-pbest/1, used by JADE/SHADE.</summary>
    public double PBestRate { get; init; }

    /// <summary>Archive size as a multiple of population size, used by JADE/SHADE.</summary>
    public double ArchiveSizeRate { get; init; }

    /// <summary>Success-history memory length, used by SHADE.</summary>
    public int MemorySize { get; init; }

    /// <summary>F/CR adaptation rate (c), used by JADE.</summary>
    public double JadeAdaptationRate { get; init; }

    /// <summary>Evaluation budget required by L-SHADE's linear population-size reduction.</summary>
    public long? MaxEvaluationNumber { get; init; }

    public int ProcessorsCount { get; init; }

    /// <summary>
    /// Fixed RNG seed, or <see langword="null"/> to let the library seed itself (the historical
    /// behaviour: every run explores a different sequence).
    ///
    /// <para>Set, it makes the whole search bit-for-bit repeatable — initial population, mutation,
    /// crossover, control-parameter sampling and archive eviction — because each worker draws from its own
    /// stream derived from this seed. That also means the seed alone does not identify a run:
    /// individual <i>i</i> draws from worker <i>i mod</i> <see cref="ProcessorsCount"/>'s stream, so the
    /// same seed under a different worker count is a different search. Both numbers have to travel
    /// together, which is why the resolved run record carries them side by side.</para>
    /// </summary>
    public int? Seed { get; init; }

    /// <summary>
    /// Optional Nelder–Mead local-search refinement layered on top of the DE search.
    /// Defaults to <see cref="NelderMeadRefinementSettings.Disabled"/> (plain DE).
    /// </summary>
    public NelderMeadRefinementSettings NelderMead { get; init; }

    private DifferentialEvolutionSettings(
        ReadOnlyCollection<double> lowerBound,
        ReadOnlyCollection<double> upperBound,
        int populationSize,
        ITerminationStrategy terminationStrategy,
        IPopulationUpdatedHandler? populationUpdatedHandler,
        DifferentialEvolutionStrategy strategy,
        double mutationForce,
        double crossoverProbability,
        double pBestRate,
        double archiveSizeRate,
        int memorySize,
        double jadeAdaptationRate,
        long? maxEvaluationNumber,
        int processorsCount,
        int? seed,
        NelderMeadRefinementSettings nelderMead)
    {
        LowerBound = lowerBound;
        UpperBound = upperBound;
        PopulationSize = populationSize;
        TerminationStrategy = terminationStrategy;
        PopulationUpdatedHandler = populationUpdatedHandler;
        Strategy = strategy;
        MutationForce = mutationForce;
        CrossoverProbability = crossoverProbability;
        PBestRate = pBestRate;
        ArchiveSizeRate = archiveSizeRate;
        MemorySize = memorySize;
        JadeAdaptationRate = jadeAdaptationRate;
        MaxEvaluationNumber = maxEvaluationNumber;
        ProcessorsCount = processorsCount;
        Seed = seed;
        NelderMead = nelderMead;
    }

    public static Builder CreateBuilder() => new();

    public sealed class Builder
    {
        // Adaptive-variant defaults mirror DotNetDifferentialEvolution 5.x (Tanabe & Fukunaga 2013).
        private const double DefaultPBestRate = 0.1;
        private const double DefaultArchiveSizeRate = 1.0;
        private const int DefaultMemorySize = 100;
        private const double DefaultJadeAdaptationRate = 0.1;

        private ReadOnlyCollection<double>? _lowerBound;
        private ReadOnlyCollection<double>? _upperBound;
        private int? _populationSize;
        private ITerminationStrategy? _terminationStrategy;
        private IPopulationUpdatedHandler? _populationUpdatedHandler;
        private DifferentialEvolutionStrategy _strategy = DifferentialEvolutionStrategy.Shade;
        private double? _mutationForce;
        private double? _crossoverProbability;
        private double _pBestRate = DefaultPBestRate;
        private double _archiveSizeRate = DefaultArchiveSizeRate;
        private int _memorySize = DefaultMemorySize;
        private double _jadeAdaptationRate = DefaultJadeAdaptationRate;
        private long? _maxEvaluationNumber;
        private int? _processorsCount;
        private int? _seed;
        private NelderMeadRefinementSettings _nelderMead = NelderMeadRefinementSettings.Disabled;

        internal Builder() { }

        public Builder WithPopulationSize(int populationSize)
        {
            if (populationSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(populationSize), "Population size must be positive.");

            _populationSize = populationSize;
            return this;
        }

        public Builder WithLowerBound(IEnumerable<double> lowerBound)
        {
            if (lowerBound == null)
                throw new ArgumentNullException(nameof(lowerBound));

            var lowerBoundList = lowerBound.ToList();
            if (lowerBoundList.Count == 0)
                throw new ArgumentException("Lower bound must contain at least one value.", nameof(lowerBound));

            _lowerBound = new ReadOnlyCollection<double>(lowerBoundList);
            return this;
        }

        public Builder WithUpperBound(IEnumerable<double> upperBound)
        {
            if (upperBound == null)
                throw new ArgumentNullException(nameof(upperBound));

            var upperBoundList = upperBound.ToList();
            if (upperBoundList.Count == 0)
                throw new ArgumentException("Upper bound must contain at least one value.", nameof(upperBound));

            _upperBound = new ReadOnlyCollection<double>(upperBoundList);
            return this;
        }

        public Builder WithTerminationStrategy(ITerminationStrategy terminationStrategy)
        {
            if (terminationStrategy == null)
                throw new ArgumentNullException(nameof(terminationStrategy));

            _terminationStrategy = terminationStrategy;
            return this;
        }

        public Builder WithPopulationUpdatedHandler(IPopulationUpdatedHandler? populationUpdatedHandler)
        {
            _populationUpdatedHandler = populationUpdatedHandler;
            return this;
        }

        public Builder WithStrategy(DifferentialEvolutionStrategy strategy)
        {
            _strategy = strategy;
            return this;
        }

        public Builder WithMutationForce(double mutationForce)
        {
            if (mutationForce < 0 || mutationForce > 2)
                throw new ArgumentOutOfRangeException(nameof(mutationForce), "Mutation force must be between 0 and 2.");

            _mutationForce = mutationForce;
            return this;
        }

        public Builder WithCrossoverProbability(double crossoverProbability)
        {
            if (crossoverProbability < 0 || crossoverProbability > 1)
                throw new ArgumentOutOfRangeException(nameof(crossoverProbability), "Crossover probability must be between 0 and 1.");

            _crossoverProbability = crossoverProbability;
            return this;
        }

        /// <summary>Overrides the JADE/SHADE control parameters. Defaults match the library.</summary>
        public Builder WithShadeParameters(double pBestRate, double archiveSizeRate, int memorySize)
        {
            if (pBestRate is <= 0 or > 1)
                throw new ArgumentOutOfRangeException(nameof(pBestRate), "pBestRate must be in (0, 1].");
            if (archiveSizeRate < 0)
                throw new ArgumentOutOfRangeException(nameof(archiveSizeRate), "archiveSizeRate must be non-negative.");
            if (memorySize <= 0)
                throw new ArgumentOutOfRangeException(nameof(memorySize), "memorySize must be positive.");

            _pBestRate = pBestRate;
            _archiveSizeRate = archiveSizeRate;
            _memorySize = memorySize;
            return this;
        }

        /// <summary>Evaluation budget for the L-SHADE strategy.</summary>
        public Builder WithMaxEvaluationNumber(long maxEvaluationNumber)
        {
            if (maxEvaluationNumber <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxEvaluationNumber), "Max evaluation number must be positive.");

            _maxEvaluationNumber = maxEvaluationNumber;
            return this;
        }

        public Builder WithProcessorsCount(int processorsCount)
        {
            if (processorsCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(processorsCount), "Processors count must be positive.");

            _processorsCount = processorsCount;
            return this;
        }

        /// <summary>
        /// Fixes the RNG seed, making the search bit-for-bit repeatable at this worker count.
        /// Pass <see langword="null"/> to leave the run unseeded.
        /// </summary>
        public Builder WithSeed(int? seed)
        {
            _seed = seed;
            return this;
        }

        /// <summary>Layers an optional Nelder–Mead local-search refinement on top of the DE search.</summary>
        public Builder WithNelderMeadRefinement(NelderMeadRefinementSettings nelderMead)
        {
            if (nelderMead == null)
                throw new ArgumentNullException(nameof(nelderMead));

            nelderMead.Validate();
            _nelderMead = nelderMead;
            return this;
        }

        public DifferentialEvolutionSettings Build()
        {
            ValidateRequiredFields();
            ValidateBounds();

            return new DifferentialEvolutionSettings(
                _lowerBound!,
                _upperBound!,
                _populationSize!.Value,
                _terminationStrategy!,
                _populationUpdatedHandler,
                _strategy,
                _mutationForce!.Value,
                _crossoverProbability!.Value,
                _pBestRate,
                _archiveSizeRate,
                _memorySize,
                _jadeAdaptationRate,
                _maxEvaluationNumber,
                _processorsCount!.Value,
                _seed,
                _nelderMead);
        }

        private void ValidateRequiredFields()
        {
            if (_lowerBound == null)
                throw new InvalidOperationException("Lower bound must be set.");

            if (_upperBound == null)
                throw new InvalidOperationException("Upper bound must be set.");

            if (!_populationSize.HasValue)
                throw new InvalidOperationException("Population size must be set.");

            if (_terminationStrategy == null)
                throw new InvalidOperationException("Termination strategy must be set.");

            if (!_mutationForce.HasValue)
                throw new InvalidOperationException("Mutation force must be set.");

            if (!_crossoverProbability.HasValue)
                throw new InvalidOperationException("Crossover probability must be set.");

            if (!_processorsCount.HasValue)
                throw new InvalidOperationException("Processors count must be set.");

            if (_strategy == DifferentialEvolutionStrategy.LShade && !_maxEvaluationNumber.HasValue)
                throw new InvalidOperationException("Max evaluation number must be set when using the L-SHADE strategy.");
        }

        private void ValidateBounds()
        {
            if (_lowerBound!.Count != _upperBound!.Count)
                throw new InvalidOperationException("Lower and upper bounds must have the same length.");

            EnsureValidBounds(_lowerBound, _upperBound);
        }

        private static void EnsureValidBounds(
            ReadOnlyCollection<double> lowerBound,
            ReadOnlyCollection<double> upperBound)
        {
            if (lowerBound.Count != upperBound.Count)
                throw new ArgumentException("Lower and upper bounds must have the same length");

            for (int i = 0; i < lowerBound.Count; i++)
            {
                if (lowerBound[i] > upperBound[i])
                    throw new ArgumentException($"Lower bound at index {i} must be less than upper bound.");
            }
        }
    }
}
