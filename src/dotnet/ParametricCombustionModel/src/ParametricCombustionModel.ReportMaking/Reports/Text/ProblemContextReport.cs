using System.Text;
using ParametricCombustionModel.Optimization.Models;
using ParametricCombustionModel.ReportMaking.Interfaces;
using ParametricCombustionModel.ReportMaking.Resources;
using UnitsNet.Units;

namespace ParametricCombustionModel.ReportMaking.Reports.Text;

public class ProblemContextReport : BaseReport, ITransformable<string>
{
    private readonly int _pressurePointReportCount;

    public ProblemContextReport(
        int pressurePointReportCount,
        OptimizationResult optimizationResult) : base(optimizationResult)
    {
        if (pressurePointReportCount <= 0)
            throw new ArgumentException(ProblemContextReportResources.PressurePointReportCountMustBeGreaterThanZero);

        if (optimizationResult.OptimizedContext.PressureCount < pressurePointReportCount)
            throw new ArgumentException(ProblemContextReportResources.PressurePointReportCountMustBeLessOrEqual);

        _pressurePointReportCount = pressurePointReportCount;
    }

    public string Transform()
    {
        var stringBuilder = new StringBuilder();
        var optimizedContext = Result.OptimizedContext;

        for (var i = 0; i < optimizedContext.PropellantCount; i++)
        {
            AppendProblemContextHeader(stringBuilder, i);

            var pressurePointStep = (int)Math.Ceiling(
                (double)optimizedContext.PressureCount / _pressurePointReportCount);
            for (var j = 0; j < optimizedContext.PressureCount; j += pressurePointStep)
            {
                AppendPressurePoint(stringBuilder, i, j);
            }
        }

        return stringBuilder.ToString();
    }

    private void AppendProblemContextHeader(
        StringBuilder stringBuilder,
        int propellantIndex)
    {
        var optimizedContext = Result.OptimizedContext;
        var propellant = optimizedContext.ProblemContextMatrix[propellantIndex, 0].Propellant;

        stringBuilder.AppendFormat(ProblemContextReportResources.HeaderOfProblemContext, propellant.Name);
        stringBuilder.AppendLine();
    }

    private void AppendPressurePoint(
        StringBuilder stringBuilder,
        int propellantIndex,
        int pressurePointIndex)
    {
        var optimizedContext = Result.OptimizedContext;
        var problemContext = optimizedContext.ProblemContextMatrix[propellantIndex, pressurePointIndex];

        stringBuilder.AppendFormat(ProblemContextReportResources.HeaderOfPressurePoint,
                                   problemContext.Pressure.ToUnit(PressureUnit.Megapascal));
        stringBuilder.AppendLine();
        stringBuilder.AppendFormat(ProblemContextReportResources.ComputedBurnRate,
                                   problemContext.MixedCombustionParams.BurnRate
                                                 .ToUnit(SpeedUnit.MillimeterPerSecond));
        stringBuilder.AppendLine();

        // Append inter-pocket area parameters
        stringBuilder.Append("\t");
        stringBuilder.AppendLine(ProblemContextReportResources.HeaderOfInterPocketArea);
        stringBuilder.Append("\t\t");
        stringBuilder.AppendFormat(ProblemContextReportResources.SurfaceTemperature,
                                   problemContext.InterPocketCombustionParams.SurfaceTemperature);
        stringBuilder.AppendLine();
        stringBuilder.Append("\t\t");
        stringBuilder.AppendFormat(ProblemContextReportResources.LinearBurnRate,
                                   problemContext.InterPocketCombustionParams.BurnRate
                                                 .ToUnit(SpeedUnit.MillimeterPerSecond));
        stringBuilder.AppendLine();
        stringBuilder.Append("\t\t");
        stringBuilder.AppendFormat(ProblemContextReportResources.DecomposeRate,
                                   problemContext.InterPocketCombustionParams.DecomposeRate);
        stringBuilder.AppendLine();
        stringBuilder.Append("\t\t");
        stringBuilder.AppendFormat(ProblemContextReportResources.KineticFlameHeatFlux,
                                   problemContext.InterPocketCombustionParams.KineticFlameCombustionParams
                                                 .KineticFlameHeatFlux);
        stringBuilder.AppendLine();
        stringBuilder.Append("\t\t ");
        stringBuilder.AppendFormat(ProblemContextReportResources.KineticFlameHeight,
                                   problemContext.InterPocketCombustionParams.KineticFlameCombustionParams
                                                 .KineticFlameHeight
                                                 .ToUnit(LengthUnit.Micrometer));
        stringBuilder.AppendLine();
        stringBuilder.Append("\t\t ");
        stringBuilder.AppendFormat(ProblemContextReportResources.AverageKineticFlameDensity,
                                   problemContext.InterPocketCombustionParams.KineticFlameCombustionParams
                                                 .AverageKineticFlameDensity);
        stringBuilder.AppendLine();
        stringBuilder.Append("\t\t ");
        stringBuilder.AppendFormat(ProblemContextReportResources.AverageKineticFlameTemperature,
                                   problemContext.InterPocketCombustionParams.KineticFlameCombustionParams
                                                 .AverageKineticFlameTemperature);
        stringBuilder.AppendLine();
        stringBuilder.Append("\t\t");
        stringBuilder.AppendFormat(ProblemContextReportResources.SublimationHeatFlux,
                                   problemContext.InterPocketCombustionParams.SublimationHeatFlux);
        stringBuilder.AppendLine();
        stringBuilder.Append("\t\t");
        stringBuilder.AppendFormat(ProblemContextReportResources.SurfaceHeatFluxesError,
                                   problemContext.InterPocketCombustionParams.SurfaceHeatFluxesError);
        stringBuilder.AppendLine();

        // Append pocket area parameters
        stringBuilder.Append("\t");
        stringBuilder.AppendLine(ProblemContextReportResources.HeaderOfPocketArea);
        stringBuilder.Append("\t\t");
        stringBuilder.AppendFormat(ProblemContextReportResources.SurfaceTemperature,
                                   problemContext.PocketCombustionParams.SurfaceTemperature);
        stringBuilder.AppendLine();
        stringBuilder.Append("\t\t");
        stringBuilder.AppendFormat(ProblemContextReportResources.LinearBurnRate,
                                   problemContext.PocketCombustionParams.BurnRate
                                                 .ToUnit(SpeedUnit.MillimeterPerSecond));
        stringBuilder.AppendLine();
        stringBuilder.Append("\t\t");
        stringBuilder.AppendFormat(ProblemContextReportResources.DecomposeRate,
                                   problemContext.PocketCombustionParams.DecomposeRate);
        stringBuilder.AppendLine();
        // Append outer skeleton area parameters
        stringBuilder.Append("\t\t->");
        stringBuilder.AppendLine(ProblemContextReportResources.HeaderOfOutSkeletonArea);
        stringBuilder.Append("\t\t\t");
        stringBuilder.AppendFormat(ProblemContextReportResources.KineticFlameHeatFlux,
                                   problemContext.PocketCombustionParams.OutSkeletonKineticFlameCombustionParams
                                                 .KineticFlameHeatFlux);
        stringBuilder.AppendLine();
        stringBuilder.Append("\t\t\t ");
        stringBuilder.AppendFormat(ProblemContextReportResources.KineticFlameHeight,
                                   problemContext.PocketCombustionParams.OutSkeletonKineticFlameCombustionParams
                                                 .KineticFlameHeight
                                                 .ToUnit(LengthUnit.Micrometer));
        stringBuilder.AppendLine();
        stringBuilder.Append("\t\t\t ");
        stringBuilder.AppendFormat(ProblemContextReportResources.AverageKineticFlameDensity,
                                   problemContext.PocketCombustionParams.OutSkeletonKineticFlameCombustionParams
                                                 .AverageKineticFlameDensity);
        stringBuilder.AppendLine();
        stringBuilder.Append("\t\t\t ");
        stringBuilder.AppendFormat(ProblemContextReportResources.AverageKineticFlameTemperature,
                                   problemContext.PocketCombustionParams.OutSkeletonKineticFlameCombustionParams
                                                 .AverageKineticFlameTemperature);
        stringBuilder.AppendLine();
        // Append inner skeleton area parameters
        stringBuilder.Append("\t\t->");
        stringBuilder.AppendLine(ProblemContextReportResources.HeaderOfSkeletonArea);
        stringBuilder.Append("\t\t\t");
        stringBuilder.AppendFormat(ProblemContextReportResources.KineticFlameHeatFlux,
                                   problemContext.PocketCombustionParams.SkeletonKineticFlameCombustionParams
                                                 .KineticFlameHeatFlux);
        stringBuilder.AppendLine();
        stringBuilder.Append("\t\t\t ");
        stringBuilder.AppendFormat(ProblemContextReportResources.KineticFlameHeight,
                                   problemContext.PocketCombustionParams.SkeletonKineticFlameCombustionParams
                                                 .KineticFlameHeight
                                                 .ToUnit(LengthUnit.Micrometer));
        stringBuilder.AppendLine();
        stringBuilder.Append("\t\t\t ");
        stringBuilder.AppendFormat(ProblemContextReportResources.AverageKineticFlameDensity,
                                   problemContext.PocketCombustionParams.SkeletonKineticFlameCombustionParams
                                                 .AverageKineticFlameDensity);
        stringBuilder.AppendLine();
        stringBuilder.Append("\t\t\t ");
        stringBuilder.AppendFormat(ProblemContextReportResources.AverageKineticFlameTemperature,
                                   problemContext.PocketCombustionParams.SkeletonKineticFlameCombustionParams
                                                 .AverageKineticFlameTemperature);
        stringBuilder.AppendLine();
        // Append metal burning parameters
        stringBuilder.Append("\t\t\t");
        stringBuilder.AppendFormat(ProblemContextReportResources.MetalBurningHeatFlux,
                                   problemContext.PocketCombustionParams.MetalBurningHeatFlux);
        stringBuilder.AppendLine();
        stringBuilder.Append("\t\t\t ");
        stringBuilder.AppendFormat(ProblemContextReportResources.AverageMetalBurningTemperature,
                                   problemContext.PocketCombustionParams.AverageMetalBurningTemperature);
        stringBuilder.AppendLine();
        // Append diffusion flame parameters
        stringBuilder.Append("\t\t");
        stringBuilder.AppendFormat(ProblemContextReportResources.DiffusionFlameHeatFlux,
                                   problemContext.PocketCombustionParams.DiffusionFlameHeatFlux);
        stringBuilder.AppendLine();
        stringBuilder.Append("\t\t ");
        stringBuilder.AppendFormat(ProblemContextReportResources.DiffusionFlameHeight,
                                   problemContext.PocketCombustionParams.DiffusionFlameHeight
                                                 .ToUnit(LengthUnit.Micrometer));
        stringBuilder.AppendLine();

        stringBuilder.Append("\t\t");
        stringBuilder.AppendFormat(ProblemContextReportResources.SkeletonHeatFlux,
                                   problemContext.PocketCombustionParams.SkeletonHeatFlux);
        stringBuilder.AppendLine();
        stringBuilder.Append("\t\t");
        stringBuilder.AppendFormat(ProblemContextReportResources.OutSkeletonHeatFlux,
                                   problemContext.PocketCombustionParams.OutSkeletonHeatFlux);
        stringBuilder.AppendLine();
        stringBuilder.Append("\t\t");
        stringBuilder.AppendFormat(ProblemContextReportResources.ToSurfaceTotalHeatFlux,
                                   problemContext.PocketCombustionParams.ToSurfaceTotalHeatFlux);
        stringBuilder.AppendLine();
        stringBuilder.Append("\t\t");
        stringBuilder.AppendFormat(ProblemContextReportResources.SublimationHeatFlux,
                                   problemContext.PocketCombustionParams.SublimationHeatFlux);
        stringBuilder.AppendLine();
        stringBuilder.Append("\t\t");
        stringBuilder.AppendFormat(ProblemContextReportResources.SurfaceHeatFluxesError,
                                   problemContext.PocketCombustionParams.SurfaceHeatFluxesError);
        stringBuilder.AppendLine();
    }
}
