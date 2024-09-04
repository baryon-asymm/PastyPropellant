using System.Runtime.CompilerServices;
using ParametricCombustionModel.Computation.Solvers;
using ParametricCombustionModel.Optimization.TargetFunctions.Interfaces;

namespace ParametricCombustionModel.Optimization.TargetFunctions;

public class TargetFunctionSolver : ITargetFunctionSolver
{
    private readonly double[][] _experimentalBurningRates;
    private readonly MixedPropellantSolver[][] _mixedPropellantSolvers;

    public TargetFunctionSolver(IEnumerable<IEnumerable<double>> experimentalBurningRates,
                                IEnumerable<IEnumerable<MixedPropellantSolver>> mixedPropellantSolvers)
    {
        _experimentalBurningRates = experimentalBurningRates.Select(x => x.ToArray()).ToArray();
        _mixedPropellantSolvers = mixedPropellantSolvers.Select(x => x.ToArray()).ToArray();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RunTargetFunction(Span<double> x, Span<double> values)
    {
        var burningParams = BurningParams.FromVector(x);
        RunTargetFunction(ref burningParams, values);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RunTargetFunction(ref BurningParams burningParams, Span<double> values)
    {
        double result = 0;
        const int surfaceTemperatureOffset = 1;
        var surfaceTemperatures = values[surfaceTemperatureOffset..];

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

                var experimentalBurningRate = _experimentalBurningRates[i][j];
                sqrPressureBurningRate +=
                    Math.Pow((propellantBurningRate - experimentalBurningRate) / experimentalBurningRate, 2);
            }

            result += Math.Sqrt(sqrPressureBurningRate / _mixedPropellantSolvers[i].Length);
        }

        values[0] = result / _mixedPropellantSolvers.Length;
    }
}
