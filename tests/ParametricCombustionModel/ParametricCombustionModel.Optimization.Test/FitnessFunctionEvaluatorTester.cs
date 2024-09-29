using ParametricCombustionModel.Computation.Builders;
using ParametricCombustionModel.Computation.Solvers;
using ParametricCombustionModel.Optimization.ConstraintPenaltyEvaluators;
using ParametricCombustionModel.Optimization.ConstraintPenaltyEvaluators.Interfaces;
using ParametricCombustionModel.Optimization.FitnessFunctionEvaluators;
using ParametricCombustionModel.Optimization.Models;
using ParametricCombustionModel.Test.Share.Helpers;
using UnitsNet;

namespace ParametricCombustionModel.Optimization.Test;

public class FitnessFunctionEvaluatorTester
{
    [Fact]
    public void TestEvaluateByDoubles()
    {
        // Arrange
        var solverParams = CombustionSolverParamsHelper.GetDefaultCombustionSolverParams();
        var context = CreateContextByUnits();
        var fitnessFunctionEvaluator = new PenaltyFitnessFunctionEvaluator();

        // Act
        context.Accept(solverParams, fitnessFunctionEvaluator);

        // Assert
    }

    private OptimizationProblemContextByUnits CreateContextByUnits()
    {
        var propellants = PropellantHelper.GetPropellants();
        var pressures = PressureHelper.GetPressures();
        var problemContextMatrix = ProblemContextByUnitsMatrixBuilder.FromPropellants(propellants)
                                                                     .ForPressures(pressures)
                                                                     .BuildMatrix();
        var propellantSolver = new MixedPropellantSolver();
        var penaltyEvaluators = GetPenaltyEvaluators();

        var context = new OptimizationProblemContextByUnits(problemContextMatrix,
                                                            propellantSolver,
                                                            penaltyEvaluators);

        return context;
    }

    private IEnumerable<IPenaltyEvaluator> GetPenaltyEvaluators()
    {
        var penaltyRate = 1.0;

        var minSurfaceTemperature = Temperature.FromKelvins(600);
        var maxSurfaceTemperature = Temperature.FromKelvins(750);
        var interPocketSurfaceTemperatureRange =
            new SurfaceTemperatureRange(minSurfaceTemperature, maxSurfaceTemperature);
        var pocketSurfaceTemperatureRange =
            new SurfaceTemperatureRange(minSurfaceTemperature, maxSurfaceTemperature);

        var heatFluxRatioThreshold = 100.0;

        var maxInterPocketKineticFlameHeatFlux = HeatFlux.FromWattsPerSquareMeter(1e9);
        var maxSkeletonKineticFlameHeatFlux = HeatFlux.FromWattsPerSquareMeter(1e8);
        var maxOutSkeletonKineticFlameHeatFlux = HeatFlux.FromWattsPerSquareMeter(1e8);

        var penaltyEvaluators = new List<IPenaltyEvaluator>
        {
            // new SurfaceTemperaturePenaltyEvaluator(penaltyRate,
            //                                        interPocketSurfaceTemperatureRange,
            //                                        pocketSurfaceTemperatureRange),
            new PocketHeatFluxRatioCompetitionPenaltyEvaluator(penaltyRate, heatFluxRatioThreshold),
            new InterPocketFasterBurnPenaltyEvaluator(penaltyRate),
            new KineticFlameHeatFluxPenaltyEvaluator(penaltyRate,
                                                     maxInterPocketKineticFlameHeatFlux,
                                                     maxSkeletonKineticFlameHeatFlux,
                                                     maxOutSkeletonKineticFlameHeatFlux)
        };

        return penaltyEvaluators;
    }
}
