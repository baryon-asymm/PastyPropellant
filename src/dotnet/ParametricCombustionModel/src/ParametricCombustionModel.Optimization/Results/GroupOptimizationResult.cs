using ParametricCombustionModel.Optimization.Models;

namespace ParametricCombustionModel.Optimization.Results;

/// <summary>
/// Contains the results of group optimization for three propellant compositions.
/// Each composition group has its own OptimizationProblemByUnits context.
/// </summary>
public class GroupOptimizationResult
{
    /// <summary>
    /// Optimization problems for each composition group with UnitsNet types.
    /// Index follows the raw 32-vector layout: 0=Bas_2+Bas_3+Bas_4 (combined), 1=Bas_1, 2=Bas_0
    /// </summary>
    public OptimizationProblemByUnits[] CompositionContexts { get; init; }

    /// <summary>
    /// Lower bounds for all 32 parameters (11 shared + 7*3 specific)
    /// </summary>
    public double[] LowerBound { get; init; }

    /// <summary>
    /// Upper bounds for all 32 parameters (11 shared + 7*3 specific)
    /// </summary>
    public double[] UpperBound { get; init; }

    /// <summary>
    /// Best 32-parameter vector found by optimization
    /// [0-10]: shared parameters
    /// [11-17]: bas_2 specific
    /// [18-24]: bas_1 specific
    /// [25-31]: bas_0 specific
    /// </summary>
    public double[] BestParams { get; init; }

    /// <summary>
    /// Aggregated fitness value (average of three composition groups)
    /// </summary>
    public double AggregatedFitness { get; set; }

    /// <summary>
    /// Total aggregated penalty from all constraints across all three groups
    /// </summary>
    public double TotalAggregatedPenalty { get; set; }

    /// <summary>
    /// Individual fitness values for each composition group
    /// </summary>
    public double[] IndividualFitnesses { get; set; } = new double[3];

    /// <summary>
    /// Individual penalty values for each composition group
    /// </summary>
    public double[] IndividualPenalties { get; set; } = new double[3];

    public GroupOptimizationResult(
        OptimizationProblemByUnits[] compositionContexts,
        double[] lowerBound,
        double[] upperBound,
        double[] bestParams)
    {
        if (compositionContexts == null || compositionContexts.Length != 3)
            throw new ArgumentException("Must have exactly 3 composition contexts", nameof(compositionContexts));
        
        if (bestParams == null || bestParams.Length != 32)
            throw new ArgumentException("Best parameters must contain exactly 32 elements", nameof(bestParams));

        CompositionContexts = compositionContexts;
        LowerBound = lowerBound ?? throw new ArgumentNullException(nameof(lowerBound));
        UpperBound = upperBound ?? throw new ArgumentNullException(nameof(upperBound));
        BestParams = bestParams;

        // Initialize individual values from contexts
        for (var i = 0; i < 3; i++)
        {
            IndividualFitnesses[i] = compositionContexts[i].FitnessFunctionValue;
            IndividualPenalties[i] = compositionContexts[i].TotalEvaluatedPenalty;
        }

        AggregatedFitness = (IndividualFitnesses[0] + IndividualFitnesses[1] + IndividualFitnesses[2]) / 3.0;
        TotalAggregatedPenalty = IndividualPenalties[0] + IndividualPenalties[1] + IndividualPenalties[2];
    }
}
