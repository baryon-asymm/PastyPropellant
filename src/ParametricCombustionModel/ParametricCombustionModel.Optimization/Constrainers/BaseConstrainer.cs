using System.Runtime.CompilerServices;
using ParametricCombustionModel.Optimization.Constrainers.Interfaces;
using ParametricCombustionModel.Optimization.TargetFunctions.Interfaces;
using ParametricCombustionModel.ParamsRecording.Models;
using ParametricCombustionModel.ParamsRecording.ParamsRecorders;

namespace ParametricCombustionModel.Optimization.Constrainers;

public abstract class BaseConstrainer : IConstrainer
{
    public const double ZeroPenaltyValue = 0;

    public double PenaltyRate { get; init; }

    private readonly MixedSolverParamsRecorder[][] _mixedSolverMatrix;

    private readonly BaseConstrainer? _nextConstrainer;
    private readonly ITargetFunctionSolver _targetFunctionSolver;

    protected BaseConstrainer(double penaltyRate,
                              IEnumerable<IEnumerable<MixedSolverParamsRecorder>> mixedSolverMatrix,
                              ITargetFunctionSolver targetFunctionSolver,
                              BaseConstrainer? nextConstrainer = default)
    {
        PenaltyRate = penaltyRate;
        _mixedSolverMatrix = mixedSolverMatrix.Select(x => x.ToArray()).ToArray();
        _targetFunctionSolver = targetFunctionSolver;
        _nextConstrainer = nextConstrainer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double GetPenaltyValue(double[] x)
    {
        return GetPenaltyValue(x.AsSpan());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double GetPenaltyValue(Span<double> x)
    {
        var burningParams = BurningParams.FromVector(x);
        return GetPenaltyValue(ref burningParams);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double GetPenaltyValue(ref BurningParams burningParams)
    {
        Span<double> values = stackalloc double[3];
        _targetFunctionSolver.RunTargetFunction(ref burningParams, values);

        if (values[0] == double.MaxValue)
            return ZeroPenaltyValue;

        double totalPenaltyValue = 0;

        foreach (var solversByPressures in _mixedSolverMatrix)
        foreach (var solver in solversByPressures)
        {
            const int surfaceTemperatureOffset = 1;
            var burningRate = solver.GetBurningRate(ref burningParams, values[surfaceTemperatureOffset..]);
            var mixedComputationParams = solver.GetRecord(values[surfaceTemperatureOffset..], ref burningParams);
            totalPenaltyValue += GetPenaltyValue(ref mixedComputationParams);
        }

        return totalPenaltyValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double GetPenaltyValue(ref MixedComputationParams mixedComputationParams)
    {
        var penaltyValueFromNext = _nextConstrainer?.GetPenaltyValue(ref mixedComputationParams) ?? 0;
        return GetCurrentPenaltyValue(ref mixedComputationParams) + penaltyValueFromNext;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected abstract double GetCurrentPenaltyValue(ref MixedComputationParams mixedComputationParams);
}
