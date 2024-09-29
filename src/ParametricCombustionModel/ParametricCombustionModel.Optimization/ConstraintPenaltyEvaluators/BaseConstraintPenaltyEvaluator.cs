using ParametricCombustionModel.Computation.Models.ProblemContexts;
using ParametricCombustionModel.Optimization.ConstraintPenaltyEvaluators.Interfaces;

namespace ParametricCombustionModel.Optimization.ConstraintPenaltyEvaluators;

public abstract class BaseConstraintPenaltyEvaluator : IPenaltyEvaluator
{
    public const double ZeroPenaltyValue = 0.0;

    public double PenaltyRate { get; init; }

    protected BaseConstraintPenaltyEvaluator(
        double penaltyRate)
    {
        if (penaltyRate <= 0)
            throw new ArgumentException("Penalty rate must be greater than 0");

        PenaltyRate = penaltyRate;
    }

    public abstract double GetPenaltyValue(
        ProblemContextByUnits updatedProblemContext);

    public abstract double GetPenaltyValue(
        ProblemContextByDoubles updatedProblemContext);
}
