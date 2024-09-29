using DifferentialEvolution.Optimizer.Interfaces;
using DifferentialEvolution.Optimizer.Models;
using DifferentialEvolution.Optimizer.MutationStrategies.Interfaces;

namespace DifferentialEvolution.Optimizer;

public class Optimizer
{
    private readonly Action<int, Individual> _callback;
    private readonly IFitnessFunctionInvoker _fitnessFunctionInvoker;
    private readonly int[] _indexesOfBestIndividuals;

    private readonly int _numberOfGenerations;

    private readonly int _threadNumber = Environment.ProcessorCount;
    private int _currentGeneration;

    private Population _currentPopulation;

    private int _indexOfBestIndividual;
    private readonly IMutationStrategy _mutationStrategy;
    private Population _nextPopulation;
    private int _syncFlag;

    public Optimizer(
        IMutationStrategy mutationStrategy,
        IFitnessFunctionInvoker fitnessFunctionInvoker,
        Population firstGenPopulation,
        Population secondGenPopulation,
        int numberOfGenerations,
        Action<int, Individual> callback)
    {
        _mutationStrategy = mutationStrategy;
        _fitnessFunctionInvoker = fitnessFunctionInvoker;

        _currentPopulation = firstGenPopulation;
        _nextPopulation = secondGenPopulation;

        _numberOfGenerations = numberOfGenerations;

        _callback = callback;

        _indexesOfBestIndividuals = new int[_threadNumber];
    }

    public Individual Run()
    {
        for (var i = 1; i < _threadNumber; i++)
        {
            var index = i;
            Task.Factory.StartNew(() => RunWorker(index), TaskCreationOptions.LongRunning);
        }

        RunWorker(0);

        FindBestIndividual();

        return _currentPopulation.Individuals.Span[_indexOfBestIndividual];
    }

    private void FindBestIndividual()
    {
        for (var i = 0; i < _indexesOfBestIndividuals.Length; i++)
        {
            var index = _indexesOfBestIndividuals[i];
            if (_currentPopulation.Individuals.Span[index].FitnessFunctionCost
                < _currentPopulation.Individuals.Span[_indexOfBestIndividual].FitnessFunctionCost)
                _indexOfBestIndividual = index;
        }
    }

    private void RunWorker(
        int workerIndex)
    {
        var localGeneration = 0;
        var tempIndividualVector = new double[_currentPopulation.Individuals.Span[0].Vector.Length];
        var tempIndividual = new Individual(0, tempIndividualVector);
        var bestIndividualIndex = 0;

        var sum = 0;
        var lastBestIndividualIndex = 0;
        while (sum < _numberOfGenerations)
        {
            for (var j = workerIndex; j < _currentPopulation.Individuals.Length; j += _threadNumber)
            {
                _mutationStrategy.Mutate(ref _currentPopulation, j, ref tempIndividual);
                tempIndividual.FitnessFunctionCost =
                    _fitnessFunctionInvoker.Invoke(workerIndex, tempIndividual.Vector.Span);

                if (tempIndividual.FitnessFunctionCost < _currentPopulation.Individuals.Span[j].FitnessFunctionCost)
                {
                    (tempIndividual, _nextPopulation.Individuals.Span[j]) =
                        (_nextPopulation.Individuals.Span[j], tempIndividual);
                }
                else
                {
                    for (var k = 0; k < _nextPopulation.Individuals.Span[j].Vector.Length; k++)
                        _nextPopulation.Individuals.Span[j].Vector.Span[k] =
                            _currentPopulation.Individuals.Span[j].Vector.Span[k];
                    _nextPopulation.Individuals.Span[j].FitnessFunctionCost =
                        _currentPopulation.Individuals.Span[j].FitnessFunctionCost;
                }

                if (_nextPopulation.Individuals.Span[j].FitnessFunctionCost
                    < _nextPopulation.Individuals.Span[bestIndividualIndex].FitnessFunctionCost)
                    bestIndividualIndex = j;
            }

            _indexesOfBestIndividuals[workerIndex] = bestIndividualIndex;

            if (workerIndex > 0)
            {
                Interlocked.Increment(ref _syncFlag);
                while (Interlocked.CompareExchange(ref _currentGeneration, int.MaxValue, int.MaxValue)
                       == localGeneration)
                {
                }

                localGeneration++;
            }
            else
            {
                while (Interlocked.CompareExchange(ref _syncFlag, int.MaxValue, int.MaxValue) < _threadNumber - 1)
                {
                }

                (_currentPopulation, _nextPopulation) = (_nextPopulation, _currentPopulation);

                FindBestIndividual();
                _callback(_currentGeneration, _currentPopulation.Individuals.Span[_indexOfBestIndividual]);

                if (Math.Abs(_currentPopulation.Individuals.Span[lastBestIndividualIndex].FitnessFunctionCost
                             - _currentPopulation.Individuals.Span[_indexOfBestIndividual].FitnessFunctionCost)
                    < 1e-12
                    && _currentPopulation.Individuals.Span[_indexOfBestIndividual].FitnessFunctionCost
                    != double.MaxValue)
                    sum++;
                else
                {
                    lastBestIndividualIndex = _indexOfBestIndividual;
                    sum = 0;
                }

                Interlocked.Add(ref _syncFlag, -(_threadNumber - 1));
                Interlocked.Increment(ref _currentGeneration);
            }
        }
    }
}
