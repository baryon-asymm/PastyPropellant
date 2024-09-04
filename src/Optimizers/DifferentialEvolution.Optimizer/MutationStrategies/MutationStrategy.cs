using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using DifferentialEvolution.Optimizer.Models;
using DifferentialEvolution.Optimizer.MutationStrategies.Interfaces;

namespace DifferentialEvolution.Optimizer.MutationStrategies;

public class MutationStrategy : IMutationStrategy
{
    private const int NumberOfIndividualsToChoose = 3;
    private readonly double _crossoverFactor;

    private readonly ReadOnlyCollection<double> _lowerBound;

    private readonly double _mutationForce;

    private readonly Random _random = Random.Shared;
    private readonly ReadOnlyCollection<double> _upperBound;

    public MutationStrategy(IEnumerable<double> lowerBound,
                            IEnumerable<double> upperBound,
                            double mutationForce = 0.3,
                            double crossoverFactor = 0.8)
    {
        _lowerBound = lowerBound.ToList().AsReadOnly();
        _upperBound = upperBound.ToList().AsReadOnly();

        _mutationForce = mutationForce;
        _crossoverFactor = crossoverFactor;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Mutate(ref Population population, int indexOfCurrentIndividual, ref Individual trialIndividual)
    {
        Span<int> indexes = stackalloc int[NumberOfIndividualsToChoose];

        for (var i = 0; i < NumberOfIndividualsToChoose; i++)
        {
            indexes[i] = _random.Next(population.Individuals.Length - 1);
            if (indexes[i] >= indexOfCurrentIndividual) indexes[i]++;

            for (var j = 0; j < i; j++)
                if (indexes[i] == indexes[j])
                {
                    i--;
                    break;
                }
        }

        for (var i = 0; i < trialIndividual.Vector.Length; i++)
            if (_random.NextDouble() <= _crossoverFactor)
            {
                var trialValue = population.Individuals.Span[indexes[0]].Vector.Span[i]
                                 + _mutationForce * (population.Individuals.Span[indexes[1]].Vector.Span[i]
                                                     - population.Individuals.Span[indexes[2]].Vector.Span[i]);
                if (trialValue >= _lowerBound[i] && trialValue <= _upperBound[i])
                    trialIndividual.Vector.Span[i] = trialValue;
                else
                    trialIndividual.Vector.Span[i] =
                        _random.NextDouble() * (_upperBound[i] - _lowerBound[i]) + _lowerBound[i];
            }
            else
            {
                trialIndividual.Vector.Span[i] = population.Individuals.Span[indexOfCurrentIndividual].Vector.Span[i];
            }
    }
}
