using ParametricCombustionModel.Optimization.Models;
using ParametricCombustionModel.ReportMaking.Enums;
using ParametricCombustionModel.ReportMaking.Interfaces;
using ParametricCombustionModel.ReportMaking.PdfOperations;
using ParametricCombustionModel.ReportMaking.Resources;
using UnitsNet.Units;

namespace ParametricCombustionModel.ReportMaking.Reports.Pdf;

public class ProblemContextReport : BaseReport, ITransformable<Queue<IPdfOperation>>
{
    private readonly Memory<int> _pressurePointIndexes;

    public ProblemContextReport(
        Span<int> pressurePointIndexes,
        OptimizationResult optimizationResult) : base(optimizationResult)
    {
        _pressurePointIndexes = new Memory<int>(pressurePointIndexes.ToArray());
    }

    public Queue<IPdfOperation> Transform()
    {
        var operations = new Queue<IPdfOperation>();
        var optimizedContext = Result.OptimizedContext;

        for (var i = 0; i < optimizedContext.PropellantCount; i++)
        {
            operations.Enqueue(new LineBreakOperation());
            AppendProblemContextHeader(operations, i);

            foreach (var pressurePointIndex in _pressurePointIndexes.Span)
            {
                operations.Enqueue(new LineBreakOperation());
                AppendPressurePoint(operations, i, pressurePointIndex);
            }
        }

        operations.Dequeue();

        return operations;
    }

    private void AppendProblemContextHeader(
        Queue<IPdfOperation> operations,
        int propellantIndex)
    {
        var optimizedContext = Result.OptimizedContext;
        var propellant = optimizedContext.ProblemContextMatrix[propellantIndex, 0].Propellant;

        operations.Enqueue(new PrintTextOperation(
                               string.Format(ProblemContextReportResources.HeaderOfProblemContext, propellant.Name),
                               TextStyle.Bold));
        operations.Enqueue(new LineBreakOperation());
    }

    private void AppendPressurePoint(
        Queue<IPdfOperation> operations,
        int propellantIndex,
        int pressurePointIndex)
    {
        var optimizedContext = Result.OptimizedContext;
        var problemContext = optimizedContext.ProblemContextMatrix[propellantIndex, pressurePointIndex];
        var experimentalBurnRate = optimizedContext.ExperimentalBurnRates[propellantIndex, pressurePointIndex];

        operations.Enqueue(new PrintTextOperation(
                               string.Format(ProblemContextReportResources.HeaderOfPressurePoint,
                                             problemContext.Pressure.ToUnit(PressureUnit.Megapascal)),
                               TextStyle.Italic | TextStyle.Underline));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new PrintTextOperation(ProblemContextReportResources.ExperimentalBurnRate, TextStyle.None));
        operations.Enqueue(new AddTabOperation());
        operations.Enqueue(new PrintTextOperation(
                               experimentalBurnRate.ToUnit(SpeedUnit.MillimeterPerSecond).ToString(),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new PrintTextOperation(ProblemContextReportResources.ComputedBurnRate, TextStyle.None));
        operations.Enqueue(new AddTabOperation());
        operations.Enqueue(new AddTabOperation());
        operations.Enqueue(new AddTabOperation());
        operations.Enqueue(new PrintTextOperation(
                               problemContext.MixedCombustionParams.BurnRate
                                             .ToUnit(SpeedUnit.MillimeterPerSecond).ToString(),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());

        // Append inter-pocket area parameters
        operations.Enqueue(new PrintTextOperation(
                               ProblemContextReportResources.HeaderOfInterPocketArea,
                               TextStyle.Italic));
        operations.Enqueue(new LineBreakOperation());
        AddTab(operations);
        operations.Enqueue(new PrintTextOperation(
                               string.Format(ProblemContextReportResources.SurfaceTemperature,
                                             problemContext.InterPocketCombustionParams.SurfaceTemperature),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        AddTab(operations);
        operations.Enqueue(new PrintTextOperation(
                               string.Format(ProblemContextReportResources.LinearBurnRate,
                                             problemContext.InterPocketCombustionParams.BurnRate
                                                           .ToUnit(SpeedUnit.MillimeterPerSecond)),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        AddTab(operations);
        operations.Enqueue(new PrintTextOperation(
                               string.Format(ProblemContextReportResources.DecomposeRate,
                                             problemContext.InterPocketCombustionParams.DecomposeRate),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        AddTab(operations);
        operations.Enqueue(new PrintTextOperation(
                               string.Format(ProblemContextReportResources.KineticFlameHeatFlux,
                                             problemContext.InterPocketCombustionParams.KineticFlameCombustionParams
                                                           .KineticFlameHeatFlux),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        AddTab(operations);
        operations.Enqueue(new PrintTextOperation(
                               string.Format(ProblemContextReportResources.KineticFlameHeight,
                                             problemContext.InterPocketCombustionParams.KineticFlameCombustionParams
                                                           .KineticFlameHeight
                                                           .ToUnit(LengthUnit.Micrometer)),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        AddTab(operations);
        operations.Enqueue(new PrintTextOperation(
                               string.Format(ProblemContextReportResources.AverageKineticFlameDensity,
                                             problemContext.InterPocketCombustionParams.KineticFlameCombustionParams
                                                           .AverageKineticFlameDensity),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        AddTab(operations);
        operations.Enqueue(new PrintTextOperation(
                               string.Format(ProblemContextReportResources.AverageKineticFlameTemperature,
                                             problemContext.InterPocketCombustionParams.KineticFlameCombustionParams
                                                           .AverageKineticFlameTemperature),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        AddTab(operations);
        operations.Enqueue(new PrintTextOperation(
                               string.Format(ProblemContextReportResources.SublimationHeatFlux,
                                             problemContext.InterPocketCombustionParams.SublimationHeatFlux),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        AddTab(operations);
        operations.Enqueue(new PrintTextOperation(
                               string.Format(ProblemContextReportResources.SurfaceHeatFluxesError,
                                             problemContext.InterPocketCombustionParams.SurfaceHeatFluxesError),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());

        // Append pocket area parameters
        operations.Enqueue(new PrintTextOperation(
                               ProblemContextReportResources.HeaderOfPocketArea,
                               TextStyle.Italic));
        operations.Enqueue(new LineBreakOperation());
        AddTab(operations);
        operations.Enqueue(new PrintTextOperation(
                               string.Format(ProblemContextReportResources.SurfaceTemperature,
                                             problemContext.PocketCombustionParams.SurfaceTemperature),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        AddTab(operations);
        operations.Enqueue(new PrintTextOperation(
                               string.Format(ProblemContextReportResources.LinearBurnRate,
                                             problemContext.PocketCombustionParams.BurnRate
                                                           .ToUnit(SpeedUnit.MillimeterPerSecond)),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        AddTab(operations);
        operations.Enqueue(new PrintTextOperation(
                               string.Format(ProblemContextReportResources.DecomposeRate,
                                             problemContext.PocketCombustionParams.DecomposeRate),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        // Append outer skeleton area parameters
        AddTab(operations);
        operations.Enqueue(new PrintTextOperation(
                               ProblemContextReportResources.HeaderOfOutSkeletonArea,
                               TextStyle.Italic));
        operations.Enqueue(new LineBreakOperation());
        AddTab(operations, 2);
        operations.Enqueue(new PrintTextOperation(
                               string.Format(ProblemContextReportResources.KineticFlameHeatFlux,
                                             problemContext
                                                 .PocketCombustionParams.OutSkeletonKineticFlameCombustionParams
                                                 .KineticFlameHeatFlux),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        AddTab(operations, 2);
        operations.Enqueue(new PrintTextOperation(
                               string.Format(ProblemContextReportResources.KineticFlameHeight,
                                             problemContext
                                                 .PocketCombustionParams.OutSkeletonKineticFlameCombustionParams
                                                 .KineticFlameHeight
                                                 .ToUnit(LengthUnit.Micrometer)),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        AddTab(operations, 2);
        operations.Enqueue(new PrintTextOperation(
                               string.Format(ProblemContextReportResources.AverageKineticFlameDensity,
                                             problemContext
                                                 .PocketCombustionParams.OutSkeletonKineticFlameCombustionParams
                                                 .AverageKineticFlameDensity),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        AddTab(operations, 2);
        operations.Enqueue(new PrintTextOperation(
                               string.Format(ProblemContextReportResources.AverageKineticFlameTemperature,
                                             problemContext
                                                 .PocketCombustionParams.OutSkeletonKineticFlameCombustionParams
                                                 .AverageKineticFlameTemperature),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        // Append inner skeleton area parameters
        AddTab(operations);
        operations.Enqueue(new PrintTextOperation(
                               ProblemContextReportResources.HeaderOfSkeletonArea,
                               TextStyle.Italic));
        operations.Enqueue(new LineBreakOperation());
        AddTab(operations, 2);
        operations.Enqueue(new PrintTextOperation(
                               string.Format(ProblemContextReportResources.KineticFlameHeatFlux,
                                             problemContext.PocketCombustionParams.SkeletonKineticFlameCombustionParams
                                                           .KineticFlameHeatFlux),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        AddTab(operations, 2);
        operations.Enqueue(new PrintTextOperation(
                               string.Format(ProblemContextReportResources.KineticFlameHeight,
                                             problemContext.PocketCombustionParams.SkeletonKineticFlameCombustionParams
                                                           .KineticFlameHeight
                                                           .ToUnit(LengthUnit.Micrometer)),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        AddTab(operations, 2);
        operations.Enqueue(new PrintTextOperation(
                               string.Format(ProblemContextReportResources.AverageKineticFlameDensity,
                                             problemContext.PocketCombustionParams.SkeletonKineticFlameCombustionParams
                                                           .AverageKineticFlameDensity),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        AddTab(operations, 2);
        operations.Enqueue(new PrintTextOperation(
                               string.Format(ProblemContextReportResources.AverageKineticFlameTemperature,
                                             problemContext.PocketCombustionParams.SkeletonKineticFlameCombustionParams
                                                           .AverageKineticFlameTemperature),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        // Append metal burning parameters
        AddTab(operations, 2);
        operations.Enqueue(new PrintTextOperation(
                               string.Format(ProblemContextReportResources.MetalBurningHeatFlux,
                                             problemContext.PocketCombustionParams.MetalBurningHeatFlux),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        AddTab(operations, 2);
        operations.Enqueue(new PrintTextOperation(
                               string.Format(ProblemContextReportResources.EffectiveThermalConductivity,
                                   problemContext.PocketCombustionParams.EffectiveThermalConductivity),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        AddTab(operations, 2);
        operations.Enqueue(new PrintTextOperation(
                               string.Format(ProblemContextReportResources.ConductiveThermalConductivity,
                                   problemContext.PocketCombustionParams.ConductiveThermalConductivity),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        AddTab(operations, 2);
        operations.Enqueue(new PrintTextOperation(
                               string.Format(ProblemContextReportResources.RadiativeThermalConductivity,
                                   problemContext.PocketCombustionParams.RadiativeThermalConductivity),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        AddTab(operations, 2);
        operations.Enqueue(new PrintTextOperation(
                               string.Format(ProblemContextReportResources.ConductiveThermalConductivityBalanceError,
                                   problemContext.PocketCombustionParams.ConductiveThermalConductivityBalanceError),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        AddTab(operations, 2);
        operations.Enqueue(new PrintTextOperation(
                               string.Format(ProblemContextReportResources.SkeletonLayerThickness,
                                   problemContext.PocketCombustionParams.SkeletonLayerThickness),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        AddTab(operations, 2);
        operations.Enqueue(new PrintTextOperation(
                               string.Format(ProblemContextReportResources.PoreDiameter,
                                   problemContext.PocketCombustionParams.PoreDiameter),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        // Append diffusion flame parameters
        AddTab(operations);
        operations.Enqueue(new PrintTextOperation(
                               string.Format(ProblemContextReportResources.DiffusionFlameHeatFlux,
                                             problemContext.PocketCombustionParams.DiffusionFlameHeatFlux),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        AddTab(operations);
        operations.Enqueue(new PrintTextOperation(
                               string.Format(ProblemContextReportResources.DiffusionFlameHeight,
                                             problemContext.PocketCombustionParams.DiffusionFlameHeight
                                                           .ToUnit(LengthUnit.Micrometer)),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        // Append surface heat fluxes
        AddTab(operations);
        operations.Enqueue(new PrintTextOperation(
                               string.Format(ProblemContextReportResources.SkeletonHeatFlux,
                                             problemContext.PocketCombustionParams.SkeletonHeatFlux),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        AddTab(operations);
        operations.Enqueue(new PrintTextOperation(
                               string.Format(ProblemContextReportResources.OutSkeletonHeatFlux,
                                             problemContext.PocketCombustionParams.OutSkeletonHeatFlux),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        AddTab(operations);
        operations.Enqueue(new PrintTextOperation(
                               string.Format(ProblemContextReportResources.ToSurfaceTotalHeatFlux,
                                             problemContext.PocketCombustionParams.ToSurfaceTotalHeatFlux),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        AddTab(operations);
        operations.Enqueue(new PrintTextOperation(
                               string.Format(ProblemContextReportResources.SublimationHeatFlux,
                                             problemContext.PocketCombustionParams.SublimationHeatFlux),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        AddTab(operations);
        operations.Enqueue(new PrintTextOperation(
                               string.Format(ProblemContextReportResources.SurfaceHeatFluxesError,
                                             problemContext.PocketCombustionParams.SurfaceHeatFluxesError),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
    }

    private static void AddTab(
        Queue<IPdfOperation> operations,
        int times = 1)
    {
        for (var i = 0; i < times; i++)
        {
            operations.Enqueue(new AddTabOperation());
        }
    }
}
