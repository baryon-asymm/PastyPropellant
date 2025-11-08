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

    public double MutationForce { get; init; }

    public double CrossoverProbability { get; init; }
    
    public int ProcessorsCount { get; init; }

    private DifferentialEvolutionSettings(
        ReadOnlyCollection<double> lowerBound,
        ReadOnlyCollection<double> upperBound,
        int populationSize,
        ITerminationStrategy terminationStrategy,
        IPopulationUpdatedHandler? populationUpdatedHandler,
        double mutationForce,
        double crossoverProbability,
        int processorsCount)
    {
        LowerBound = lowerBound;
        UpperBound = upperBound;
        PopulationSize = populationSize;
        TerminationStrategy = terminationStrategy;
        PopulationUpdatedHandler = populationUpdatedHandler;
        MutationForce = mutationForce;
        CrossoverProbability = crossoverProbability;
        ProcessorsCount = processorsCount;
    }

    public static Builder CreateBuilder() => new();

    public sealed class Builder
    {
        private ReadOnlyCollection<double>? _lowerBound;
        private ReadOnlyCollection<double>? _upperBound;
        private int? _populationSize;
        private ITerminationStrategy? _terminationStrategy;
        private IPopulationUpdatedHandler? _populationUpdatedHandler;
        private double? _mutationForce;
        private double? _crossoverProbability;
        private int? _processorsCount;

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

        public Builder WithProcessorsCount(int processorsCount)
        {
            if (processorsCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(processorsCount), "Processors count must be positive.");

            _processorsCount = processorsCount;
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
                _mutationForce!.Value,
                _crossoverProbability!.Value,
                _processorsCount!.Value);
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
