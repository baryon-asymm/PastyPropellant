using System.Runtime.CompilerServices;
using ParametricCombustionModel.Computation.Models.KnownParams;
using ParametricCombustionModel.Optimization.Interfaces;
using ParametricCombustionModel.Optimization.Models;

namespace ParametricCombustionModel.Optimization.FitnessFunctionEvaluators;

public class FitnessFunctionEvaluator : IFitnessFunctionVisitor
{
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public virtual void Visit(
        in CombustionSolverParamsByUnits solverParamsByUnits,
        OptimizationProblemContextByUnits context)
    {
        context.FitnessFunctionValue = EvaluateByUnits(solverParamsByUnits, context);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public virtual void Visit(
        in CombustionSolverParamsByDoubles solverParams,
        OptimizationProblemContextByDoubles context)
    {
        context.FitnessFunctionValue = EvaluateByDoubles(solverParams, context);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static double EvaluateByDoubles(
        in CombustionSolverParamsByDoubles solverParams,
        OptimizationProblemContextByDoubles context)
    {
        var fitnessFunctionValue = 0.0;
        var solver = context.Solver;

        for (var i = 0; i < context.PropellantCount; i++)
        {
            double sqrPressureBurnRate = 0;
            for (var j = 0; j < context.PressureCount; j++)
            {
                context.ProblemContextMatrix[i, j].Accept(solverParams, solver);
                ref var mixedCombustionParams = ref context.ProblemContextMatrix[i, j].MixedCombustionParams;

                if (mixedCombustionParams.BurnRateIsFound == false)
                    return double.MaxValue;

                var propellantBurnRate = mixedCombustionParams.BurnRate;
                var experimentalBurnRate = context.ExperimentalBurnRates[i, j];
                sqrPressureBurnRate +=
                    Math.Pow((propellantBurnRate - experimentalBurnRate) / experimentalBurnRate, 2);
            }

            fitnessFunctionValue += Math.Sqrt(sqrPressureBurnRate / context.PressureCount);
        }

        fitnessFunctionValue /= context.PropellantCount;

        return fitnessFunctionValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static double EvaluateByUnits(
        in CombustionSolverParamsByUnits solverParamsByUnits,
        OptimizationProblemContextByUnits context)
    {
        var fitnessFunctionValue = 0.0;
        var solver = context.Solver;

        for (var i = 0; i < context.PropellantCount; i++)
        {
            double sqrPressureBurnRate = 0;
            for (var j = 0; j < context.PressureCount; j++)
            {
                context.ProblemContextMatrix[i, j].Accept(solverParamsByUnits, solver);
                ref var mixedCombustionParams = ref context.ProblemContextMatrix[i, j].MixedCombustionParams;

                if (mixedCombustionParams.BurnRateIsFound == false)
                    return double.MaxValue;

                var propellantBurnRate = mixedCombustionParams.BurnRate;
                var experimentalBurnRate = context.ExperimentalBurnRates[i, j];
                sqrPressureBurnRate +=
                    Math.Pow((propellantBurnRate - experimentalBurnRate) / experimentalBurnRate, 2);
            }

            fitnessFunctionValue += Math.Sqrt(sqrPressureBurnRate / context.PressureCount);
        }

        fitnessFunctionValue /= context.PropellantCount;

        return fitnessFunctionValue;
    }
}
