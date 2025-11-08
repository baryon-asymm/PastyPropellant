using System.Runtime.CompilerServices;
using ParametricCombustionModel.Computation.Interfaces;
using ParametricCombustionModel.Computation.Models.KnownParams;
using ParametricCombustionModel.Computation.Models.ProblemContexts;
using ParametricCombustionModel.Optimization.ConstraintPenaltyEvaluators.Interfaces;
using ParametricCombustionModel.Optimization.Extensions;
using ParametricCombustionModel.Optimization.Interfaces;

namespace ParametricCombustionModel.Optimization.Models;

public class OptimizationProblemByDoubles : IOptimizationVisitable
{
#region Fields

    public ProblemContextByDoubles[,] ProblemContextMatrix;
    public double[,] ExperimentalBurnRates;

    public double FitnessFunctionValue;
    public double TotalEvaluatedPenalty;

    public Memory<IPenaltyEvaluator> PenaltyEvaluators;
    public Memory<double> EvaluatedPenalties;

    public ISolverVisitor Solver;

#endregion

    public int PropellantCount { get; init; }
    public int PressureCount { get; init; }

    public OptimizationProblemByDoubles(
        ProblemContextByDoubles[,] problemContextMatrix,
        ISolverVisitor solver,
        IEnumerable<IPenaltyEvaluator> penaltyEvaluators)
    {
        ProblemContextMatrix = problemContextMatrix ?? throw new ArgumentNullException(nameof(problemContextMatrix));
        Solver = solver ?? throw new ArgumentNullException(nameof(solver));

        PropellantCount = problemContextMatrix.GetLength(0);
        PressureCount = problemContextMatrix.GetLength(1);

        PenaltyEvaluators = penaltyEvaluators.ToArray();
        EvaluatedPenalties = new double[PenaltyEvaluators.Length];

        ExperimentalBurnRates = new double[PropellantCount, PressureCount];
        for (var i = 0; i < PropellantCount; i++)
        {
            for (var j = 0; j < PressureCount; j++)
            {
                ExperimentalBurnRates[i, j] = problemContextMatrix[i, j].Propellant
                                                                        .GetExperimentalBurnRate(
                                                                            problemContextMatrix[i, j].Pressure);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public virtual void Accept(
        in CombustionSolverParamsByUnits solverParamsByUnits,
        IFitnessFunctionVisitor fitnessFunction) =>
        throw new NotSupportedException("This method is not supported.");

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public virtual void Accept(
        in CombustionSolverParamsByDoubles solverParams,
        IFitnessFunctionVisitor fitnessFunction) =>
        fitnessFunction.Visit(solverParams, this);
}
