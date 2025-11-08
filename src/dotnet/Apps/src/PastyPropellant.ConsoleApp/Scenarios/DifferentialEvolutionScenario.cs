using ParametricCombustionModel.Computation.Builders;
using ParametricCombustionModel.Computation.Models.ProblemContexts;
using ParametricCombustionModel.Computation.Solvers;
using ParametricCombustionModel.Core.Models;
using ParametricCombustionModel.Optimization.ConstraintPenaltyEvaluators.Interfaces;
using ParametricCombustionModel.Optimization.FitnessFunctionEvaluators;
using ParametricCombustionModel.Optimization.Models;
using ParametricCombustionModel.Optimization.Optimizers;
using ParametricCombustionModel.Optimization.Settings;
using PastyPropellant.ConsoleApp.Scenarios.Settings;
using PastyPropellant.Core.Utils;

namespace PastyPropellant.ConsoleApp.Scenarios;

/// <summary>
/// Represents a differential evolution optimization scenario for parametric combustion model optimization.
/// This class encapsulates the configuration and execution of a differential evolution algorithm
/// to optimize propellant combustion parameters while applying various constraint penalties.
/// </summary>
/// <remarks>
/// The differential evolution scenario is designed to optimize mixed propellant combustion parameters
/// by evolving a population of candidate solutions over multiple generations. The optimization process
/// considers multiple constraint penalty evaluators to ensure the solutions meet physical and
/// engineering constraints such as heat flux limits, pore diameter thresholds, and kinetic flame
/// heat flux boundaries.
/// 
/// The scenario supports parallel processing by creating multiple optimization problem contexts
/// based on the number of available processor cores, enabling efficient utilization of system resources.
/// </remarks>
public class DifferentialEvolutionScenario
{
    /// <summary>
    /// The differential evolution optimization settings containing algorithm parameters,
    /// constraint thresholds, and optimization bounds.
    /// </summary>
    private readonly DifferentialEvolutionScenarioSettings _settings;

    /// <summary>
    /// The differential evolution optimizer instance configured with problem contexts,
    /// fitness function evaluators, and algorithm parameters.
    /// </summary>
    private readonly DifferentialEvolutionOptimizer _optimizer;

    /// <summary>
    /// Initializes a new instance of the <see cref="DifferentialEvolutionScenario"/> class.
    /// </summary>
    /// <param name="settings">The differential evolution optimization settings containing 
    /// algorithm parameters, constraint thresholds, and optimization bounds.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="settings"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when propellants cannot be deserialized from the specified file.</exception>
    /// <remarks>
    /// This constructor performs the following initialization steps:
    /// <list type="number">
    /// <item>Creates penalty evaluators based on the provided constraint thresholds</item>
    /// <item>Builds optimization problem contexts for both double and unit-based computations</item>
    /// <item>Configures the differential evolution optimizer with the specified parameters</item>
    /// </list>
    /// 
    /// The penalty evaluators include:
    /// <list type="bullet">
    /// <item>Pocket heat flux ratio competition penalty</item>
    /// <item>Inter-pocket faster burn penalty</item>
    /// <item>Kinetic flame heat flux penalties for different regions</item>
    /// <item>Pore diameter constraint penalty</item>
    /// </list>
    /// </remarks>
    public DifferentialEvolutionScenario(
        DifferentialEvolutionScenarioSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));

        var propellants = _settings.Propellants;
        var penaltyEvaluators = _settings.PenaltyEvaluators;

        var optimizationProblemContext = GetOptimizationProblemContextByDoubles(
            propellants,
            penaltyEvaluators);
        var contextMatrixByUnits = GetProblemContextMatrixByUnits(propellants);
        var optimizationProblemContextByUnits = GetOptimizationProblemContextByUnits(
            contextMatrixByUnits,
            penaltyEvaluators);
        var fitnessFunctionEvaluator = new PenaltyFitnessFunctionEvaluator();

        _optimizer = new DifferentialEvolutionOptimizer(
            settings: _settings.DifferentialEvolutionSettings,
            optimizationContexts: optimizationProblemContext,
            optimizationContextByUnits: optimizationProblemContextByUnits,
            fitnessFunctionSolver: fitnessFunctionEvaluator);
    }

    /// <summary>
    /// Creates a matrix of problem contexts using double-precision values for the given propellants.
    /// </summary>
    /// <param name="propellants">The collection of propellants to build contexts for.</param>
    /// <returns>A two-dimensional array of <see cref="ProblemContextByDoubles"/> representing 
    /// the problem context matrix for optimization calculations.</returns>
    /// <remarks>
    /// This method creates a matrix where each element represents a specific propellant configuration
    /// context using double-precision floating-point values for computational efficiency.
    /// </remarks>
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
    /// <remarks>
    /// This method creates a matrix where each element represents a specific propellant configuration
    /// context using the UnitsNet library for proper dimensional analysis and unit conversions.
    /// </remarks>
    private static ProblemContextByUnits[,] GetProblemContextMatrixByUnits(
        IEnumerable<Propellant> propellants)
    {
        var contextMatrix = ProblemContextByUnitsMatrixBuilder.FromPropellants(propellants)
                                                              .BuildMatrix();

        return contextMatrix;
    }

    /// <summary>
    /// Creates optimization problem contexts using double-precision values for parallel processing.
    /// </summary>
    /// <param name="propellants">The collection of propellants to create contexts for.</param>
    /// <param name="penaltyEvaluators">The penalty evaluators to apply during optimization.</param>
    /// <returns>An array of <see cref="OptimizationProblemByDoubles"/> instances, one for each processor core.</returns>
    /// <remarks>
    /// This method creates multiple optimization problem contexts to enable parallel processing
    /// during the differential evolution algorithm. The number of contexts created equals the
    /// number of available processor cores (<see cref="Environment.ProcessorCount"/>).
    /// Each context contains its own solver and problem context matrix for thread-safe operation.
    /// </remarks>
    private OptimizationProblemByDoubles[] GetOptimizationProblemContextByDoubles(
        IEnumerable<Propellant> propellants,
        IEnumerable<IPenaltyEvaluator> penaltyEvaluators)
    {
        var solver = new MixedPropellantSolver();
        var contexts = new List<OptimizationProblemByDoubles>();
        for (var i = 0; i < _settings.DifferentialEvolutionSettings.ProcessorsCount; i++)
        {
            var contextMatrix = GetProblemContextMatrixByDoubles(propellants);
            contexts.Add(new MeasureOptimizationProblemByDoubles(_settings.Meter, i, contextMatrix, solver, penaltyEvaluators));
        }

        return contexts.ToArray();
    }

    /// <summary>
    /// Creates an optimization problem context using unit-aware values.
    /// </summary>
    /// <param name="contextMatrix">The problem context matrix with proper unit handling.</param>
    /// <param name="penaltyEvaluators">The penalty evaluators to apply during optimization.</param>
    /// <returns>An <see cref="OptimizationProblemByUnits"/> instance for unit-aware computations.</returns>
    /// <remarks>
    /// This method creates a single optimization problem context that uses the UnitsNet library
    /// for proper dimensional analysis and unit conversions during optimization calculations.
    /// This context is typically used for validation and result interpretation.
    /// </remarks>
    private static OptimizationProblemByUnits GetOptimizationProblemContextByUnits(
        ProblemContextByUnits[,] contextMatrix,
        IEnumerable<IPenaltyEvaluator> penaltyEvaluators)
    {
        var solver = new MixedPropellantSolver();
        var context = new OptimizationProblemByUnits(contextMatrix, solver, penaltyEvaluators);
        return context;
    }

    /// <summary>
    /// Executes the differential evolution optimization algorithm asynchronously.
    /// </summary>
    /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous optimization operation.
    /// The task result contains an <see cref="OperationResult{T}"/> with the <see cref="OptimizationResult"/>
    /// indicating the success or failure of the optimization process and the best solution found.</returns>
    /// <remarks>
    /// This method starts the differential evolution optimization process, which will:
    /// <list type="number">
    /// <item>Initialize a population of candidate solutions within the specified bounds</item>
    /// <item>Evolve the population over multiple generations using differential evolution operators</item>
    /// <item>Apply constraint penalties to guide the search toward feasible solutions</item>
    /// <item>Monitor for convergence or maximum stagnation streak</item>
    /// <item>Return the best solution found along with optimization statistics</item>
    /// </list>
    /// 
    /// The optimization process runs asynchronously and can be cancelled or monitored for progress.
    /// The algorithm will continue until convergence criteria are met or the maximum stagnation
    /// streak is reached, as specified in the <see cref="DifferentialEvolutionSettings"/>.
    /// </remarks>
    public Task<OperationResult<OptimizationResult>> RunAsync()
    {
        return _optimizer.RunAsync();
    }
}
