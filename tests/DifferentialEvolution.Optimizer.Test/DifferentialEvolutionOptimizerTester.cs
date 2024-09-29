using DifferentialEvolution.Optimizer.Interfaces;
using DifferentialEvolution.Optimizer.Models;
using DifferentialEvolution.Optimizer.MutationStrategies;

namespace DifferentialEvolution.Optimizer.Test;

public class DifferentialEvolutionOptimizerTester
{
    private static readonly double[] _initialPoint = [-78.0, -78.0];
    private static readonly double[] _lowerBound = [-1000.0, -1000.0];
    private static readonly double[] _upperBound = [1000.0, 1000.0];

    private double RosenbrockTargetFunction(Span<double> point, double a, double b)
    {
        var x = point[0];
        var y = point[1];

        return Math.Pow(a - x, 2) + b * Math.Pow(y - x * x, 2);
    }

    [Fact]
    public void RunRosenbrockFunction()
    {
        var mutationStrategy = new MutationStrategy();
        var fitnessFunctionInvoker = new FintessFunctionInvoker(this);

        var random = new Random();
        var populations = new List<Population>();
        for (var k = 0; k < 2; k++)
        {
            var individuals = new List<Individual>();
            for (var i = 0; i < 1000; i++)
            {
                var vector = new double[2];
                for (var j = 0; j < vector.Length; j++)
                    vector[j] = _lowerBound[j] + (_upperBound[j] - _lowerBound[j]) * random.NextDouble();
                var individual = new Individual(fitnessFunctionInvoker.Invoke(vector), vector);
                individuals.Add(individual);
            }

            populations.Add(new Population(individuals.ToArray()));
        }

        var optimizer =
            new Optimizer(mutationStrategy,
                                               fitnessFunctionInvoker,
                                               populations[0],
                                               populations[1],
                                               1000);
        var bestIndividual = optimizer.Run();

        Assert.Equal(0.0, bestIndividual.FitnessFunctionCost);
        Assert.Equal(1.0, bestIndividual.Vector.Span[0]);
        Assert.Equal(1.0, bestIndividual.Vector.Span[1]);
    }

    public class FintessFunctionInvoker : IFitnessFunctionInvoker
    {
        private readonly DifferentialEvolutionOptimizerTester _tester;

        public FintessFunctionInvoker(DifferentialEvolutionOptimizerTester tester)
        {
            _tester = tester;
        }

        public double Invoke(Span<double> x)
        {
            return _tester.RosenbrockTargetFunction(x, 1.0, 100.0);
        }
    }
}
