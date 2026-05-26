using ParametricCombustionModel.Computation.Builders;
using ParametricCombustionModel.Computation.Models.KnownParams;
using ParametricCombustionModel.Computation.Models.ProblemContexts;
using ParametricCombustionModel.Computation.Solvers;
using ParametricCombustionModel.Core.Models;
using ParametricCombustionModel.Optimization.ConstraintPenaltyEvaluators.Interfaces;
using ParametricCombustionModel.Optimization.FitnessFunctionEvaluators;
using ParametricCombustionModel.Optimization.Models;
using ParametricCombustionModel.Optimization.Optimizers;
using ParametricCombustionModel.Optimization.Results;
using ParametricCombustionModel.Optimization.Settings;
using PastyPropellant.ConsoleApp.Scenarios.Settings;
using PastyPropellant.Core.Utils;

namespace PastyPropellant.ConsoleApp.Scenarios;

/// <summary>
/// Represents a differential evolution optimization scenario for group optimization of three propellant compositions.
/// This class encapsulates the configuration and execution of simultaneous optimization of three fuel groups
/// using a unified 32-parameter vector (11 shared + 7 specific parameters per group).
/// </summary>
/// <remarks>
/// The group differential evolution scenario optimizes three propellant composition groups
/// (indexed in the same order as the raw 32-parameter vector layout):
/// - groupIndex 0: Bas_2+Bas_3+Bas_4 (combined, must be optimized together with shared specific parameters)
/// - groupIndex 1: Bas_1
/// - groupIndex 2: Bas_0
///
/// Each evaluation converts the 32-parameter group vector into three 18-parameter vectors,
/// evaluates each composition independently, and aggregates the fitness results.
/// The optimization process considers multiple constraint penalty evaluators to ensure
/// the solutions meet physical and engineering constraints.
/// </remarks>
public class GroupDifferentialEvolutionScenario
{
    /// <summary>
    /// The differential evolution optimization settings containing algorithm parameters,
    /// constraint thresholds, and optimization bounds for the 32-parameter group vector.
    /// </summary>
    private readonly DifferentialEvolutionScenarioSettings _settings;

    /// <summary>
    /// The group differential evolution optimizer instance configured with problem contexts
    /// for all three composition groups, fitness function evaluators, and algorithm parameters.
    /// </summary>
    private readonly GroupDifferentialEvolutionOptimizer _optimizer;

    /// <summary>
    /// Initializes a new instance of the <see cref="GroupDifferentialEvolutionScenario"/> class.
    /// </summary>
    /// <param name="settings">The differential evolution optimization settings containing 
    /// algorithm parameters, constraint thresholds, and optimization bounds.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="settings"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when propellants cannot be deserialized from the specified file.</exception>
    /// <remarks>
    /// This constructor performs the following initialization steps:
    /// <list type="number">
    /// <item>Groups propellants into three composition groups (Bas_2+Bas_3+Bas_4, Bas_1, Bas_0)</item>
    /// <item>Creates problem context matrices for each group</item>
    /// <item>Creates a matrix of optimization problem contexts for parallel processing</item>
    /// <item>Configures the group differential evolution optimizer with the specified parameters</item>
    /// </list>
    /// </remarks>
    public GroupDifferentialEvolutionScenario(
        DifferentialEvolutionScenarioSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));

        var propellants = _settings.Propellants;
        var penaltyEvaluators = _settings.PenaltyEvaluators;

        // Create problem contexts for each composition group
        var bas0Propellant = propellants.First(p => p.Name == "Bas_0");
        var bas1Propellant = propellants.First(p => p.Name == "Bas_1");
        var bas2Propellants = propellants.Where(p => p.Name == "Bas_2" || p.Name == "Bas_3" || p.Name == "Bas_4").ToList();

        // Build problem context matrices for each group
        var contextMatrixBas0ByDoubles = GetProblemContextMatrixByDoubles(new[] { bas0Propellant });
        var contextMatrixBas1ByDoubles = GetProblemContextMatrixByDoubles(new[] { bas1Propellant });
        var contextMatrixBas2ByDoubles = GetProblemContextMatrixByDoubles(bas2Propellants);

        var contextMatrixBas0ByUnits = GetProblemContextMatrixByUnits(new[] { bas0Propellant });
        var contextMatrixBas1ByUnits = GetProblemContextMatrixByUnits(new[] { bas1Propellant });
        var contextMatrixBas2ByUnits = GetProblemContextMatrixByUnits(bas2Propellants);

        // Create optimization problem matrix [processorCount, 3]
        // Each processor gets its own separate copy of context matrices to avoid concurrency issues
        var processorCount = _settings.DifferentialEvolutionSettings.ProcessorsCount;
        
        var contextMatricesByDoublesList = new List<ProblemContextByDoubles[,]>[processorCount];
        for (int i = 0; i < processorCount; i++)
        {
            // Create fresh matrices for each worker
            var workerBas0Matrix = GetProblemContextMatrixByDoubles(new[] { bas0Propellant });
            var workerBas1Matrix = GetProblemContextMatrixByDoubles(new[] { bas1Propellant });
            var workerBas2Matrix = GetProblemContextMatrixByDoubles(bas2Propellants);
            
            // Order matches the raw 32-vector layout: [11-17]=Bas_2+3+4, [18-24]=Bas_1, [25-31]=Bas_0
            contextMatricesByDoublesList[i] = new List<ProblemContextByDoubles[,]>
            {
                workerBas2Matrix,
                workerBas1Matrix,
                workerBas0Matrix
            };
        }
        
        var optimizationProblemContextMatrix = GetGroupOptimizationProblemContextByDoubles(
            contextMatricesByDoublesList,
            penaltyEvaluators);

        // Create final contexts array for result evaluation [3]
        // Use fresh matrices (same contexts for result evaluation)
        // Order matches the raw 32-vector layout: [11-17]=Bas_2+3+4, [18-24]=Bas_1, [25-31]=Bas_0
        var contextMatricesByUnitsList = new List<ProblemContextByUnits[,]>
        {
            contextMatrixBas2ByUnits,
            contextMatrixBas1ByUnits,
            contextMatrixBas0ByUnits
        };
        
        var finalContextsByUnits = GetGroupOptimizationProblemContextByUnits(
            contextMatricesByUnitsList,
            penaltyEvaluators);

        _optimizer = new GroupDifferentialEvolutionOptimizer(
            settings: _settings.DifferentialEvolutionSettings,
            compositionProblems: optimizationProblemContextMatrix,
            finalContextsByUnits: finalContextsByUnits);
    }

    /// <summary>
    /// Creates a matrix of problem contexts using double-precision values for the given propellants.
    /// </summary>
    /// <param name="propellants">The collection of propellants to build contexts for.</param>
    /// <returns>A two-dimensional array of <see cref="ProblemContextByDoubles"/> representing 
    /// the problem context matrix for optimization calculations.</returns>
    private static ProblemContextByDoubles[,] GetProblemContextMatrixByDoubles(
        IEnumerable<Propellant> propellants)
    {
        var contextMatrix = ProblemContextByDoublesMatrixBuilder.FromPropellants(propellants)
                                                                .BuildMatrix();
        return contextMatrix;
    }

    /// <summary>
    /// Creates a matrix of problem contexts using unit-aware values for the given propellants.
    /// </summary>
    /// <param name="propellants">The collection of propellants to build contexts for.</param>
    /// <returns>A two-dimensional array of <see cref="ProblemContextByUnits"/> representing 
    /// the problem context matrix with proper unit handling.</returns>
    private static ProblemContextByUnits[,] GetProblemContextMatrixByUnits(
        IEnumerable<Propellant> propellants)
    {
        var contextMatrix = ProblemContextByUnitsMatrixBuilder.FromPropellants(propellants)
                                                              .BuildMatrix();
        return contextMatrix;
    }

    /// <summary>
    /// Creates a matrix of optimization problem contexts for group optimization using double-precision values.
    /// Each worker receives a list of 3 context matrices (one per group).
    /// Matrix dimensions: [propellantCount, pressureCount] - full matrices with all pressure points.
    /// </summary>
    /// <param name="contextMatricesByDoublesList">Array where each element is a worker's list of 3 context matrices.</param>
    /// <param name="penaltyEvaluators">The penalty evaluators to apply during optimization.</param>
    /// <returns>A two-dimensional array of <see cref="OptimizationProblemByDoubles"/> instances [workerIndex, groupIndex].</returns>
    private OptimizationProblemByDoubles[,] GetGroupOptimizationProblemContextByDoubles(
        List<ProblemContextByDoubles[,]>[] contextMatricesByDoublesList,
        IEnumerable<IPenaltyEvaluator> penaltyEvaluators)
    {
        var processorCount = contextMatricesByDoublesList.Length;
        var solver = new MixedPropellantSolver();
        var matrix = new OptimizationProblemByDoubles[processorCount, 3];

        for (var workerIdx = 0; workerIdx < processorCount; workerIdx++)
        {
            var workerContextMatrices = contextMatricesByDoublesList[workerIdx];
            
            for (var groupIdx = 0; groupIdx < 3; groupIdx++)
            {
                var contextMatrix = workerContextMatrices[groupIdx];  // Full matrix with all pressure points
                matrix[workerIdx, groupIdx] = new MeasureOptimizationProblemByDoubles(
                    _settings.Meter,
                    workerIdx,
                    contextMatrix,
                    solver,
                    penaltyEvaluators);
            }
        }

        return matrix;
    }

    /// <summary>
    /// Creates an array of optimization problem contexts for final result evaluation using unit-aware values.
    /// Array dimensions: [groupIndex]
    /// groupIndex: 0=Bas_2+Bas_3+Bas_4, 1=Bas_1, 2=Bas_0 (matches the raw vector layout)
    /// </summary>
    /// <param name="contextMatricesByUnitsList">List of 3 context matrices (one per group) with all pressure points.</param>
    /// <param name="penaltyEvaluators">The penalty evaluators to apply during optimization.</param>
    /// <returns>An array of <see cref="OptimizationProblemByUnits"/> instances.</returns>
    private static OptimizationProblemByUnits[] GetGroupOptimizationProblemContextByUnits(
        List<ProblemContextByUnits[,]> contextMatricesByUnitsList,
        IEnumerable<IPenaltyEvaluator> penaltyEvaluators)
    {
        var solver = new MixedPropellantSolver();
        var contexts = new OptimizationProblemByUnits[3];

        for (var groupIdx = 0; groupIdx < 3; groupIdx++)
        {
            var contextMatrix = contextMatricesByUnitsList[groupIdx];  // Full matrix with all pressure points
            contexts[groupIdx] = new OptimizationProblemByUnits(
                contextMatrix,
                solver,
                penaltyEvaluators);
        }

        return contexts;
    }

    /// <summary>
    /// Executes the group differential evolution optimization algorithm asynchronously.
    /// </summary>
    /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous optimization operation.
    /// The task result contains an <see cref="OperationResult{T}"/> with the <see cref="GroupOptimizationResult"/>
    /// indicating the success or failure of the optimization process and the best 32-parameter solution found.</returns>
    /// <remarks>
    /// This method starts the group differential evolution optimization process, which will:
    /// <list type="number">
    /// <item>Initialize a population of 32-parameter candidate solutions within the specified bounds</item>
    /// <item>Evolve the population over multiple generations using differential evolution operators</item>
    /// <item>For each evaluation, convert 32-parameter vector into three 18-parameter vectors</item>
    /// <item>Evaluate each composition group independently and aggregate fitness results</item>
    /// <item>Apply constraint penalties to guide the search toward feasible solutions</item>
    /// <item>Monitor for convergence or maximum stagnation streak</item>
    /// <item>Return the best 32-parameter solution found along with optimization statistics</item>
    /// </list>
    /// 
    /// The optimization process runs asynchronously and can be cancelled or monitored for progress.
    /// </remarks>
    public Task<OperationResult<GroupOptimizationResult>> RunAsync()
    {
        return _optimizer.RunAsync();
    }
}
