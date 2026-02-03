using ParametricCombustionModel.Computation.Models.ProblemContexts;
using ParametricCombustionModel.Optimization.Models;

namespace ParametricCombustionModel.Optimization.Results;

/// <summary>
/// Helper class to convert GroupOptimizationResult to OptimizationResult for compatibility
/// with existing report generation infrastructure.
/// </summary>
public static class GroupOptimizationResultAdapter
{
    /// <summary>
    /// Converts GroupOptimizationResult to OptimizationResult by merging all composition contexts.
    /// </summary>
    public static OptimizationResult ToOptimizationResult(this GroupOptimizationResult groupResult)
    {
        if (groupResult == null)
            throw new ArgumentNullException(nameof(groupResult));

        // Merge all composition contexts into a single matrix
        var mergedContextMatrix = MergeContextMatrices(groupResult.CompositionContexts);

        // Create a combined OptimizationProblemByUnits from merged contexts
        var combinedProblem = new OptimizationProblemByUnits(
            mergedContextMatrix,
            groupResult.CompositionContexts[0].Solver,
            groupResult.CompositionContexts[0].PenaltyEvaluators.ToArray());

        // Copy the aggregated fitness and penalty values
        combinedProblem.FitnessFunctionValue = groupResult.AggregatedFitness;
        combinedProblem.TotalEvaluatedPenalty = groupResult.TotalAggregatedPenalty;

        // Create OptimizationResult with empty bounds (as they're not meaningful for merged context)
        var result = new OptimizationResult(
            new Memory<double>(new double[0]),
            new Memory<double>(new double[0]),
            new Memory<double>(groupResult.BestParams),
            combinedProblem);

        return result;
    }

    /// <summary>
    /// Merges three composition context matrices into a single matrix.
    /// Composition structure:
    /// - Bas_0: 1 fuel
    /// - Bas_1: 1 fuel
    /// - Bas_2 group (includes Bas_3, Bas_4): 3 fuels
    /// Total: 5 fuels × pressureCount
    /// </summary>
    private static ProblemContextByUnits[,] MergeContextMatrices(OptimizationProblemByUnits[] compositionContexts)
    {
        if (compositionContexts == null || compositionContexts.Length != 3)
            throw new ArgumentException("Must have exactly 3 composition contexts", nameof(compositionContexts));

        // All contexts should have the same number of pressure points
        var pressureCount = compositionContexts[0].PressureCount;
        
        // Verify all contexts have the same pressure count
        for (int i = 1; i < 3; i++)
        {
            if (compositionContexts[i].PressureCount != pressureCount)
                throw new InvalidOperationException("All composition contexts must have the same pressure count");
        }

        // Calculate total propellant count
        // Bas_0: 1, Bas_1: 1, Bas_2+3+4: 3
        var totalPropellantCount = compositionContexts[0].PropellantCount + 
                                   compositionContexts[1].PropellantCount + 
                                   compositionContexts[2].PropellantCount;

        // Create merged matrix: totalPropellantCount × pressureCount
        var mergedMatrix = new ProblemContextByUnits[totalPropellantCount, pressureCount];

        int propellantIndex = 0;

        // Copy Bas_0 contexts (1 fuel)
        for (int fuel = 0; fuel < compositionContexts[0].PropellantCount; fuel++)
        {
            for (int pressure = 0; pressure < pressureCount; pressure++)
            {
                mergedMatrix[propellantIndex, pressure] = compositionContexts[0].ProblemContextMatrix[fuel, pressure];
            }
            propellantIndex++;
        }

        // Copy Bas_1 contexts (1 fuel)
        for (int fuel = 0; fuel < compositionContexts[1].PropellantCount; fuel++)
        {
            for (int pressure = 0; pressure < pressureCount; pressure++)
            {
                mergedMatrix[propellantIndex, pressure] = compositionContexts[1].ProblemContextMatrix[fuel, pressure];
            }
            propellantIndex++;
        }

        // Copy Bas_2+Bas_3+Bas_4 contexts (3 fuels)
        for (int fuel = 0; fuel < compositionContexts[2].PropellantCount; fuel++)
        {
            for (int pressure = 0; pressure < pressureCount; pressure++)
            {
                mergedMatrix[propellantIndex, pressure] = compositionContexts[2].ProblemContextMatrix[fuel, pressure];
            }
            propellantIndex++;
        }

        return mergedMatrix;
    }
}
