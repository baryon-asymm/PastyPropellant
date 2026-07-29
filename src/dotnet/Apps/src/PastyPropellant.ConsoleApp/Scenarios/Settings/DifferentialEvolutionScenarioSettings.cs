using System.Collections.ObjectModel;
using System.Diagnostics.Metrics;
using System.Text.Json;
using DotNetDifferentialEvolution.Interfaces;
using DotNetDifferentialEvolution.Models;
using DotNetDifferentialEvolution.TerminationStrategies.Interfaces;
using ParametricCombustionModel.Computation.Models.KnownParams;
using ParametricCombustionModel.Core.Models;
using ParametricCombustionModel.Optimization.ConstraintPenaltyEvaluators.Interfaces;
using ParametricCombustionModel.Optimization.Optimizers;
using ParametricCombustionModel.Optimization.Settings;
using ParametricCombustionModel.Telemetry;
using PastyPropellant.Core.Models.Events.Logs;
using PastyPropellant.Core.Utils;

namespace PastyPropellant.ConsoleApp.Scenarios.Settings;

public record DifferentialEvolutionScenarioSettings
{
    public DifferentialEvolutionSettings DifferentialEvolutionSettings { get; init; }

    public PerformanceMeter Meter { get; init; }

    public ReadOnlyCollection<Propellant> Propellants { get; init; }

    public ReadOnlyCollection<IPenaltyEvaluator> PenaltyEvaluators { get; init; }

    /// <summary>
    /// Run-wide physical constants of the model (metal melting temperature, skeleton contact factor).
    /// Defaults to <see cref="ModelConstants.Default"/> — the historical hardcoded values.
    /// </summary>
    public ModelConstants ModelConstants { get; init; }

    private DifferentialEvolutionScenarioSettings(
        DifferentialEvolutionSettings differentialEvolutionSettings,
        PerformanceMeter meter,
        ReadOnlyCollection<Propellant> propellants,
        ReadOnlyCollection<IPenaltyEvaluator> penaltyEvaluators,
        ModelConstants modelConstants)
    {
        DifferentialEvolutionSettings = differentialEvolutionSettings;
        Meter = meter;
        Propellants = propellants;
        PenaltyEvaluators = penaltyEvaluators;
        ModelConstants = modelConstants;
    }

    public static Builder CreateBuilder() => new();

    public sealed class Builder
    {
        private int? _populationSize;
        private PerformanceMeter? _meter;
        private ReadOnlyCollection<Propellant>? _propellants;
        private ReadOnlyCollection<double>? _lowerBound;
        private ReadOnlyCollection<double>? _upperBound;
        private ITerminationStrategy? _terminationStrategy;
        private DifferentialEvolutionStrategy _strategy = DifferentialEvolutionStrategy.Shade;
        private double? _mutationForce;
        private double? _crossoverProbability;
        private long? _maxEvaluationNumber;
        private int? _processorsCount;
        private int? _seed;
        private ModelConstants _modelConstants = ModelConstants.Default;
        private double? _pBestRate;
        private double? _archiveSizeRate;
        private int? _memorySize;
        private NelderMeadRefinementSettings _nelderMead = NelderMeadRefinementSettings.Disabled;
        private readonly List<IPenaltyEvaluator> _penaltyEvaluators = [];

        internal Builder() { }

        public Builder WithMeter(PerformanceMeter meter)
        {
            if (meter == null)
                throw new ArgumentNullException(nameof(meter));

            _meter = meter;
            return this;
        }

        public Builder WithPopulationSize(int populationSize)
        {
            if (populationSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(populationSize), "Population size must be positive.");
            
            _populationSize = populationSize;
            return this;
        }

        public Builder WithTerminationStrategy(ITerminationStrategy terminationStrategy)
        {
            if (terminationStrategy == null)
                throw new ArgumentNullException(nameof(terminationStrategy));

            _terminationStrategy = terminationStrategy;
            return this;
        }

        public Builder WithStrategy(DifferentialEvolutionStrategy strategy)
        {
            _strategy = strategy;
            return this;
        }

        public Builder WithMaxEvaluationNumber(long maxEvaluationNumber)
        {
            if (maxEvaluationNumber <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxEvaluationNumber), "Max evaluation number must be positive.");

            _maxEvaluationNumber = maxEvaluationNumber;
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

        public Builder WithNelderMeadRefinement(NelderMeadRefinementSettings nelderMead)
        {
            _nelderMead = nelderMead ?? throw new ArgumentNullException(nameof(nelderMead));
            return this;
        }

        /// <summary>
        /// Fixes the RNG seed, making the search bit-for-bit repeatable at the configured worker count.
        /// <see langword="null"/> leaves the run unseeded, which is what every run before the
        /// DotNetDifferentialEvolution 5.x upgrade did.
        /// </summary>
        public Builder WithSeed(int? seed)
        {
            _seed = seed;
            return this;
        }

        /// <summary>
        /// Sets the run-wide model constants. Omitted, the historical hardcoded values apply, so a run
        /// that does not mention them computes what it always computed.
        /// </summary>
        public Builder WithModelConstants(ModelConstants modelConstants)
        {
            ArgumentNullException.ThrowIfNull(modelConstants);
            modelConstants.Validate();
            _modelConstants = modelConstants;
            return this;
        }

        /// <summary>
        /// Overrides the JADE/SHADE/L-SHADE control parameters (otherwise the library defaults apply).
        /// For canonical L-SHADE (Tanabe &amp; Fukunaga 2014) use p=0.11, archiveRate=2.6, memory=6.
        /// </summary>
        public Builder WithShadeParameters(double pBestRate, double archiveSizeRate, int memorySize)
        {
            _pBestRate = pBestRate;
            _archiveSizeRate = archiveSizeRate;
            _memorySize = memorySize;
            return this;
        }

        public Builder WithPropellantsFromFile(string propellantsFilePath)
        {
            if (string.IsNullOrWhiteSpace(propellantsFilePath))
                throw new ArgumentException("Propellants file path must be provided.", nameof(propellantsFilePath));

            if (!File.Exists(propellantsFilePath))
                throw new FileNotFoundException($"Propellants file not found: {propellantsFilePath}");

            try
            {
                var jsonContent = File.ReadAllText(propellantsFilePath);
                var propellants = JsonSerializer.Deserialize<List<Propellant>>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (propellants == null || propellants.Count == 0)
                    throw new ArgumentException("Propellants file must contain at least one propellant.", nameof(propellantsFilePath));

                _propellants = new ReadOnlyCollection<Propellant>(propellants);
            }
            catch (JsonException ex)
            {
                throw new ArgumentException($"Invalid JSON format in propellants file: {ex.Message}", nameof(propellantsFilePath), ex);
            }

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

        public Builder AddPenaltyEvaluator(IPenaltyEvaluator penaltyEvaluator)
        {
            if (penaltyEvaluator == null)
                throw new ArgumentNullException(nameof(penaltyEvaluator));

            _penaltyEvaluators.Add(penaltyEvaluator);
            return this;
        }

        public Builder AddPenaltyEvaluators(IEnumerable<IPenaltyEvaluator> penaltyEvaluators)
        {
            if (penaltyEvaluators == null)
                throw new ArgumentNullException(nameof(penaltyEvaluators));

            foreach (var evaluator in penaltyEvaluators)
            {
                if (evaluator == null)
                    throw new ArgumentException("Penalty evaluators collection cannot contain null elements.", nameof(penaltyEvaluators));
                
                _penaltyEvaluators.Add(evaluator);
            }

            return this;
        }

        public DifferentialEvolutionScenarioSettings Build()
        {
            ValidateRequiredFields();
            ValidateBounds();

            var baseSettingsBuilder = DifferentialEvolutionSettings.CreateBuilder()
                .WithLowerBound(_lowerBound!)
                .WithUpperBound(_upperBound!)
                .WithPopulationSize(_populationSize!.Value)
                .WithTerminationStrategy(_terminationStrategy!)
                .WithPopulationUpdatedHandler(new PopulationUpdateHandler())
                .WithStrategy(_strategy)
                .WithMutationForce(_mutationForce!.Value)
                .WithCrossoverProbability(_crossoverProbability!.Value)
                .WithProcessorsCount(_processorsCount!.Value)
                .WithSeed(_seed)
                .WithNelderMeadRefinement(_nelderMead);

            if (_maxEvaluationNumber.HasValue)
                baseSettingsBuilder = baseSettingsBuilder.WithMaxEvaluationNumber(_maxEvaluationNumber.Value);

            if (_pBestRate.HasValue && _archiveSizeRate.HasValue && _memorySize.HasValue)
                baseSettingsBuilder = baseSettingsBuilder.WithShadeParameters(
                    _pBestRate.Value, _archiveSizeRate.Value, _memorySize.Value);

            var baseSettings = baseSettingsBuilder.Build();

            return new DifferentialEvolutionScenarioSettings(
                baseSettings,
                _meter!,
                _propellants!,
                new ReadOnlyCollection<IPenaltyEvaluator>(_penaltyEvaluators),
                _modelConstants);
        }

        private class PopulationUpdateHandler : IPopulationUpdatedHandler
        {
            private DateTime _lastUpdate = DateTime.Now;
            private readonly TimeSpan _updateInterval = TimeSpan.FromSeconds(3);
            
            public void Handle(Population population)
            {
                if (DateTime.Now - _lastUpdate < _updateInterval)
                    return;

                population.MoveCursorToBestIndividual();

                _lastUpdate = DateTime.Now;

                EventBus<InfoLogEvent>.Publish(
                    new InfoLogEvent(
                        $"Generation: {population.GenerationNumber}, Best individual: {population.IndividualCursor.FitnessFunctionValue}",
                        nameof(GroupDifferentialEvolutionOptimizer)
                    )
                );
            }
        }

        private void ValidateRequiredFields()
        {
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

            if (_propellants == null)
                throw new InvalidOperationException("Propellants must be loaded from file.");

            if (_lowerBound == null)
                throw new InvalidOperationException("Lower bound must be set.");

            if (_upperBound == null)
                throw new InvalidOperationException("Upper bound must be set.");

            // PenaltyEvaluators is optional and can be empty
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
