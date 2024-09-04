using System.Runtime.CompilerServices;
using ParametricCombustionModel.Computation.Solvers;
using ParametricCombustionModel.Optimization.TargetFunctions.Interfaces;

namespace ParametricCombustionModel.Optimization.TargetFunctions;

public class PenaltyTargetFunctionNonlconSolver : ITargetFunctionSolver
{
    private readonly double[][] _experimentalBurningRates;
    private readonly MixedPropellantSolver[][] _mixedPropellantSolvers;

    private readonly (double, double) _surfaceTemperatureRange;

    public PenaltyTargetFunctionNonlconSolver(IEnumerable<IEnumerable<double>> experimentalBurningRates,
                                              IEnumerable<IEnumerable<MixedPropellantSolver>> mixedPropellantSolvers,
                                              (double, double) surfaceTemperatureRange)
    {
        _experimentalBurningRates = experimentalBurningRates.Select(x => x.ToArray()).ToArray();
        _mixedPropellantSolvers = mixedPropellantSolvers.Select(x => x.ToArray()).ToArray();
        _surfaceTemperatureRange = surfaceTemperatureRange;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void RunTargetFunction(Span<double> x, Span<double> values)
    {
        var burningParams = BurningParams.FromVector(x);
        RunTargetFunction(ref burningParams, values);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RunTargetFunction(ref BurningParams burningParams, Span<double> values)
    {
        double result = 0;
        const int surfaceTemperaturesOffset = 1;
        var surfaceTemperatures = values[surfaceTemperaturesOffset..];

        for (var i = 0; i < _mixedPropellantSolvers.Length; i++)
        {
            double sqrPressureBurningRate = 0;
            for (var j = 0; j < _mixedPropellantSolvers[i].Length; j++)
            {
                var propellantBurningRate =
                    _mixedPropellantSolvers[i][j].GetBurningRate(ref burningParams, surfaceTemperatures);

                if (propellantBurningRate < 0)
                {
                    values[0] = double.MaxValue;
                    return;
                }

                const double penalty = 100;
                foreach (var surfaceTemperature in surfaceTemperatures)
                    if (surfaceTemperature < _surfaceTemperatureRange.Item1)
                        result += penalty * _surfaceTemperatureRange.Item1 / surfaceTemperature;
                    else if (surfaceTemperature > _surfaceTemperatureRange.Item2)
                        result += penalty * surfaceTemperature / _surfaceTemperatureRange.Item2;

                var experimentalBurningRate = _experimentalBurningRates[i][j];
                sqrPressureBurningRate +=
                    Math.Pow((propellantBurningRate - experimentalBurningRate) / experimentalBurningRate, 2);
            }

            result += Math.Sqrt(sqrPressureBurningRate);
        }

        values[0] = result;
    }
}
