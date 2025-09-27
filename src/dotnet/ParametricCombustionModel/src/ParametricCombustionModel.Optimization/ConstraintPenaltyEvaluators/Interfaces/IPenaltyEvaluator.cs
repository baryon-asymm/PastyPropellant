using ParametricCombustionModel.Computation.Models.ProblemContexts;

namespace ParametricCombustionModel.Optimization.ConstraintPenaltyEvaluators.Interfaces
{
    /// <summary>
    /// Defines a contract for classes that calculate penalty values based on problem contexts.
    /// </summary>
    public interface IPenaltyEvaluator
    {
        /// <summary>
        /// Calculates the penalty value based on the given <see cref="ProblemContextByUnits"/>.
        /// </summary>
        /// <param name="updatedProblemContext">
        /// The updated problem context that has been calculated and contains the most recent computed parameters. 
        /// This context reflects the latest values of parameters relevant for penalty calculation.
        /// </param>
        /// <returns>
        /// The calculated penalty value.
        /// </returns>
        double GetPenaltyValue(
            ProblemContextByUnits updatedProblemContext);

        /// <summary>
        /// Calculates the penalty value based on the given <see cref="ProblemContextByDoubles"/>.
        /// </summary>
        /// <param name="updatedProblemContext">
        /// The updated problem context that has been calculated and contains the most recent computed parameters. 
        /// This context reflects the latest values of parameters relevant for penalty calculation.
        /// </param>
        /// <returns>
        /// The calculated penalty value.
        /// </returns>
        double GetPenaltyValue(
            ProblemContextByDoubles updatedProblemContext);
    }
}
