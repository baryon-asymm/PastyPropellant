using System.Runtime.CompilerServices;
using ParametricCombustionModel.Computation.Interfaces;
using ParametricCombustionModel.Computation.Models.KnownParams;
using ParametricCombustionModel.Computation.Models.ProblemContexts;
using ParametricCombustionModel.Optimization.ConstraintPenaltyEvaluators.Interfaces;
using ParametricCombustionModel.Optimization.Interfaces;
using ParametricCombustionModel.Telemetry;
using ParametricCombustionModel.Telemetry.Instruments;

namespace ParametricCombustionModel.Optimization.Models;

public class MeasureOptimizationProblemByDoubles : OptimizationProblemByDoubles
{
#region Fields

    private readonly EnhancedExecutionFrameMeasurer _optimizationProblemMeasurer;

#endregion

    public MeasureOptimizationProblemByDoubles(
        PerformanceMeter performanceMeter,
        int processorId,
        ProblemContextByDoubles[,] problemContextMatrix,
        ISolverVisitor solver,
        IEnumerable<IPenaltyEvaluator> penaltyEvaluators) : base(problemContextMatrix, solver, penaltyEvaluators)
    {
        if (performanceMeter == null)
            throw new ArgumentNullException(nameof(performanceMeter));
            
        _optimizationProblemMeasurer = performanceMeter.CreateExecutionFrameMeasurer(
            $"OptimizationProblemByDoubles_Processor{processorId}",
            "Measures the execution time of the optimization problem (one iteration).",
            "ms",
            $"Telemetry/Proc/{processorId}/OptimizationProblemExecutionPerformance"
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public override void Accept(
        in CombustionSolverParamsByUnits solverParamsByUnits,
        IFitnessFunctionVisitor fitnessFunction) =>
        throw new NotSupportedException("This method is not supported.");

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public override void Accept(
        in CombustionSolverParamsByDoubles solverParams,
        IFitnessFunctionVisitor fitnessFunction)
    {
        using (_optimizationProblemMeasurer.StartFrame())
        {
            fitnessFunction.Visit(solverParams, this);
        }
    }
}
