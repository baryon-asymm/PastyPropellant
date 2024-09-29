using System.Collections.ObjectModel;
using DifferentialEvolution.Optimizer.Interfaces;
using DifferentialEvolution.Optimizer.Models;
using DifferentialEvolution.Optimizer.PopulationGenerators.Interfaces;

namespace DifferentialEvolution.Optimizer.PopulationGenerators;

public class PopulationGenerator : IPopulationGenerator
{
    private readonly int _populationSize;
    private readonly ReadOnlyCollection<double> _upperBound;
    private readonly ReadOnlyCollection<double> _lowerBound;

    public PopulationGenerator(int populationSize,
                               ReadOnlyCollection<double> upperBound,
                               ReadOnlyCollection<double> lowerBound)
    {
        _populationSize = populationSize;
        _upperBound = upperBound;
        _lowerBound = lowerBound;
    }

    public Population Generate(IFitnessFunctionInvoker fitnessFunctionInvoker)
    {
        var random = new Random();
        var individuals = new Individual[_populationSize];
        var vectors = new double[_populationSize][];
        for (var i = 0; i < _populationSize; i++)
        {
            vectors[i] = new double[_lowerBound.Count];
            for (var j = 0; j < _lowerBound.Count; j++)
                vectors[i][j] = random.NextDouble() * (_upperBound[j] - _lowerBound[j]) + _lowerBound[j];

            var individual = new Individual(fitnessFunctionInvoker.Invoke(0, vectors[i]), vectors[i]);
            individuals[i] = individual;
        }

        return new Population(individuals.ToArray());
    }
}
