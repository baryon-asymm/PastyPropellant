using System.Runtime.CompilerServices;
using ParametricCombustionModel.Computation.Solvers;
using ParametricCombustionModel.Computation.Utils;
using ParametricCombustionModel.Optimization.TargetFunctions.Interfaces;

namespace ParametricCombustionModel.Optimization.TargetFunctions;

public class TargetFunctionNonlconSolver : ITargetFunctionSolver
{
    private readonly double[][] _experimentalBurningRates;
    private readonly MixedPropellantSolver[][] _mixedPropellantSolvers;

    private readonly (double, double) _surfaceTemperatureRange;

    public TargetFunctionNonlconSolver(IEnumerable<IEnumerable<double>> experimentalBurningRates,
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

                if (propellantBurningRate < 0 || surfaceTemperatures.IsAnyoneOutOfRange(_surfaceTemperatureRange))
                {
                    values[0] = double.MaxValue;
                    return;
                }

                var experimentalBurningRate = _experimentalBurningRates[i][j];
                sqrPressureBurningRate +=
                    Math.Pow((propellantBurningRate - experimentalBurningRate) / experimentalBurningRate, 2);
            }

            result += Math.Sqrt(sqrPressureBurningRate / _mixedPropellantSolvers[i].Length);
        }

        values[0] = result / _mixedPropellantSolvers.Length;
    }
}
