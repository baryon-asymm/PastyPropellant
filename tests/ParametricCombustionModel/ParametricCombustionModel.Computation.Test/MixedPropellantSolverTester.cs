using ParametricCombustionModel.Computation.Builders;
using ParametricCombustionModel.Computation.Models.ComputedParams;
using ParametricCombustionModel.Computation.Solvers;
using ParametricCombustionModel.Test.Share.Helpers;

namespace ParametricCombustionModel.Computation.Test;

/// <summary>
/// Provides test cases for the <see cref="MixedPropellantSolver"/> class.
/// </summary>
public class MixedPropellantSolverTester
{
    /// <summary>
    /// Tests the <see cref="ProblemContextByUnits"/> case to ensure that the burn rate is found for each combination of propellant and pressure.
    /// </summary>
    [Fact]
    public void TestProblemContextByUnitsCase()
    {
        var propellants = PropellantHelper.GetPropellants();
        var pressures = PressureHelper.GetPressures();
        var solver = new MixedPropellantSolver();
        var solverParams = CombustionSolverParamsHelper.GetDefaultCombustionSolverParams();
        var matrixOfProblemContext = ProblemContextByUnitsMatrixBuilder.FromPropellants(propellants)
                                                                       .ForPressures(pressures)
                                                                       .BuildMatrix();

        for (int i = 0; i < propellants.Count(); i++)
        {
            for (int j = 0; j < pressures.Count(); j++)
            {
                var problemContext = matrixOfProblemContext[i, j];
                problemContext.Accept(solverParams, solver);
                Assert.True(problemContext.MixedCombustionParams.BurnRateIsFound);
            }
        }
    }

    /// <summary>
    /// Compares the results of problem contexts built using units and doubles to ensure consistency.
    /// </summary>
    [Fact]
    public void TestProblemContextComparison()
    {
        var propellants = PropellantHelper.GetPropellants();
        var pressures = PressureHelper.GetPressures();
        var solver = new MixedPropellantSolver();
        var solverParams = CombustionSolverParamsHelper.GetDefaultCombustionSolverParams();
        var solverParamsByDoubles = CombustionSolverParamsHelper.GetDefaultCombustionSolverParamsByDoubles();
        var matrixOfProblemContextUnits =
            ProblemContextByUnitsMatrixBuilder.FromPropellants(propellants)
                                              .ForPressures(pressures)
                                              .BuildMatrix();
        var matrixOfProblemContextDoubles =
            ProblemContextByDoublesMatrixBuilder.FromPropellants(propellants)
                                                .ForPressures(pressures)
                                                .BuildMatrix();

        for (int i = 0; i < propellants.Count(); i++)
        {
            for (int j = 0; j < pressures.Count(); j++)
            {
                var problemContextUnits = matrixOfProblemContextUnits[i, j];
                var problemContextDoubles = matrixOfProblemContextDoubles[i, j];

                // Process the problem contexts with the solver
                problemContextUnits.Accept(solverParams, solver);
                problemContextDoubles.Accept(solverParamsByDoubles, solver);

                // Compare all parameters
                CompareMixedCombustionParams(problemContextUnits.MixedCombustionParams,
                                             problemContextDoubles.MixedCombustionParams);
                CompareInterPocketCombustionParams(problemContextUnits.InterPocketCombustionParams,
                                                   problemContextDoubles.InterPocketCombustionParams);
            }
        }
    }

    /// <summary>
    /// Compares <see cref="MixedCombustionParams"/> from units-based and double-based problem contexts.
    /// </summary>
    /// <param name="unitsParams">
    /// The <see cref="MixedCombustionParams"/> from the units-based problem context.
    /// </param>
    /// <param name="doublesParams">
    /// The <see cref="MixedCombustionParamsByDoubles"/> from the double-based problem context.
    /// </param>
    private void CompareMixedCombustionParams(
        MixedCombustionParams unitsParams,
        MixedCombustionParamsByDoubles doublesParams)
    {
        Assert.True(unitsParams.BurnRateIsFound == doublesParams.BurnRateIsFound, "BurnRateIsFound mismatch.");

        Assert.True(Math.Abs(unitsParams.BurnRate.MetersPerSecond - doublesParams.BurnRate) < 1e-6,
                    "BurnRate mismatch.");
    }

    /// <summary>
    /// Compares <see cref="InterPocketCombustionParams"/> from units-based and double-based problem contexts.
    /// </summary>
    /// <param name="unitsParams">
    /// The <see cref="InterPocketCombustionParams"/> from the units-based problem context.
    /// </param>
    /// <param name="doublesParams">
    /// The <see cref="InterPocketCombustionParamsByDoubles"/> from the double-based problem context.
    /// </param>
    private void CompareInterPocketCombustionParams(
        InterPocketCombustionParams unitsParams,
        InterPocketCombustionParamsByDoubles doublesParams)
    {
        // Compare boolean flag
        Assert.True(unitsParams.BurnRateIsFound == doublesParams.BurnRateIsFound, "BurnRateIsFound mismatch.");

        // Compare Temperature values
        Assert.True(Math.Abs(unitsParams.SurfaceTemperature.Kelvins - doublesParams.SurfaceTemperature) < 1e-6,
                    "SurfaceTemperature mismatch.");

        // Compare Speed values
        Assert.True(Math.Abs(unitsParams.BurnRate.MetersPerSecond - doublesParams.BurnRate) < 1e-6,
                    "BurnRate mismatch.");

        // Compare MassFlux values
        Assert.True(
            Math.Abs(unitsParams.DecomposeRate.KilogramsPerSecondPerSquareMeter - doublesParams.DecomposeRate) < 1e-6,
            "DecomposeRate mismatch.");

        // Compare KineticFlameCombustionParams
        CompareKineticFlameCombustionParams(unitsParams.KineticFlameCombustionParams,
                                            doublesParams.KineticFlameCombustionParams);

        // Compare HeatFlux values
        Assert.True(
            Math.Abs(unitsParams.SublimationHeatFlux.WattsPerSquareMeter - doublesParams.SublimationHeatFlux) < 1e-6,
            "SublimationHeatFlux mismatch.");
        Assert.True(
            Math.Abs(unitsParams.SurfaceHeatFluxesError.WattsPerSquareMeter - doublesParams.SurfaceHeatFluxesError)
            < 1e-6, "SurfaceHeatFluxesError mismatch.");
    }

    /// <summary>
    /// Compares <see cref="PocketCombustionParams"/> from units-based and double-based problem contexts.
    /// </summary>
    /// <param name="unitsParams">
    /// The <see cref="PocketCombustionParams"/> from the units-based problem context.
    /// </param>
    /// <param name="doublesParams">
    /// The <see cref="PocketCombustionParamsByDoubles"/> from the double-based problem context.
    /// </param>
    private void ComparePocketCombustionParams(
        PocketCombustionParams unitsParams,
        PocketCombustionParamsByDoubles doublesParams)
    {
        // Compare boolean flag
        Assert.True(unitsParams.BurnRateIsFound == doublesParams.BurnRateIsFound, "BurnRateIsFound mismatch.");

        // Compare Temperature values
        Assert.True(Math.Abs(unitsParams.SurfaceTemperature.Kelvins - doublesParams.SurfaceTemperature) < 1e-6,
                    "SurfaceTemperature mismatch.");

        // Compare Speed values
        Assert.True(Math.Abs(unitsParams.BurnRate.MetersPerSecond - doublesParams.BurnRate) < 1e-6,
                    "BurnRate mismatch.");

        // Compare MassFlux values
        Assert.True(
            Math.Abs(unitsParams.DecomposeRate.KilogramsPerSecondPerSquareMeter - doublesParams.DecomposeRate) < 1e-6,
            "DecomposeRate mismatch.");

        // Compare KineticFlameCombustionParams
        CompareKineticFlameCombustionParams(unitsParams.OutSkeletonKineticFlameCombustionParams,
                                            doublesParams.OutSkeletonKineticFlameCombustionParams);
        CompareKineticFlameCombustionParams(unitsParams.SkeletonKineticFlameCombustionParams,
                                            doublesParams.SkeletonKineticFlameCombustionParams);

        // Compare Inside Skeleton Metal Burning Parameters
        Assert.True(
            Math.Abs(unitsParams.AverageMetalBurningTemperature.Kelvins - doublesParams.AverageMetalBurningTemperature)
            < 1e-6, "AverageMetalBurningTemperature mismatch.");
        Assert.True(
            Math.Abs(unitsParams.MetalBurningHeatFlux.WattsPerSquareMeter - doublesParams.MetalBurningHeatFlux) < 1e-6,
            "MetalBurningHeatFlux mismatch.");

        // Compare Diffusion Flame Parameters
        Assert.True(Math.Abs(unitsParams.DiffusionFlameHeight.Meters - doublesParams.DiffusionFlameHeight) < 1e-6,
                    "DiffusionFlameHeight mismatch.");
        Assert.True(
            Math.Abs(unitsParams.DiffusionFlameHeatFlux.WattsPerSquareMeter - doublesParams.DiffusionFlameHeatFlux)
            < 1e-6, "DiffusionFlameHeatFlux mismatch.");

        // Compare HeatFlux values
        Assert.True(
            Math.Abs(unitsParams.OutSkeletonHeatFlux.WattsPerSquareMeter - doublesParams.OutSkeletonHeatFlux) < 1e-6,
            "OutSkeletonHeatFlux mismatch.");
        Assert.True(Math.Abs(unitsParams.SkeletonHeatFlux.WattsPerSquareMeter - doublesParams.SkeletonHeatFlux) < 1e-6,
                    "SkeletonHeatFlux mismatch.");
        Assert.True(
            Math.Abs(unitsParams.ToSurfaceTotalHeatFlux.WattsPerSquareMeter - doublesParams.ToSurfaceTotalHeatFlux)
            < 1e-6, "ToSurfaceTotalHeatFlux mismatch.");
        Assert.True(
            Math.Abs(unitsParams.SublimationHeatFlux.WattsPerSquareMeter - doublesParams.SublimationHeatFlux) < 1e-6,
            "SublimationHeatFlux mismatch.");
        Assert.True(
            Math.Abs(unitsParams.SurfaceHeatFluxesError.WattsPerSquareMeter - doublesParams.SurfaceHeatFluxesError)
            < 1e-6, "SurfaceHeatFluxesError mismatch.");
    }

    /// <summary>
    /// Compares <see cref="KineticFlameCombustionParams"/> from units-based and double-based problem contexts.
    /// </summary>
    /// <param name="unitsParams">
    /// The <see cref="KineticFlameCombustionParams"/> from the units-based problem context.
    /// </param>
    /// <param name="doublesParams">
    /// The <see cref="KineticFlameCombustionParamsByDoubles"/> from the double-based problem context.
    /// </param>
    private void CompareKineticFlameCombustionParams(
        KineticFlameCombustionParams unitsParams,
        KineticFlameCombustionParamsByDoubles doublesParams)
    {
        // Compare Length values
        Assert.True(Math.Abs(unitsParams.KineticFlameHeight.Meters - doublesParams.KineticFlameHeight) < 1e-6,
                    "KineticFlameHeight mismatch.");

        // Compare Density values
        Assert.True(
            Math.Abs(unitsParams.AverageKineticFlameDensity.KilogramsPerCubicMeter
                     - doublesParams.AverageKineticFlameDensity)
            < 1e-6, "AverageKineticFlameDensity mismatch.");

        // Compare Temperature values
        Assert.True(
            Math.Abs(unitsParams.AverageKineticFlameTemperature.Kelvins - doublesParams.AverageKineticFlameTemperature)
            < 1e-6, "AverageKineticFlameTemperature mismatch.");

        // Compare HeatFlux values
        Assert.True(
            Math.Abs(unitsParams.KineticFlameHeatFlux.WattsPerSquareMeter - doublesParams.KineticFlameHeatFlux) < 1e-6,
            "KineticFlameHeatFlux mismatch.");
    }
}
