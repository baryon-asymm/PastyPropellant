using System.Collections.ObjectModel;
using ParametricCombustionModel.Optimization.Models;
using ParametricCombustionModel.ReportMaking.Interfaces;
using ParametricCombustionModel.ReportMaking.Resources;
using PDFsharp.Api.Models;
using UnitsNet.Units;

namespace ParametricCombustionModel.ReportMaking.Reports.Pdf;

public class PressureTablesReport : BaseReport, ITransformable<ReadOnlyCollection<PressureTable>>
{
    private readonly Memory<int> _pressurePointIndexes;

    public PressureTablesReport(
        Span<int> pressurePointIndexes,
        OptimizationResult optimizationResult) : base(optimizationResult)
    {
        _pressurePointIndexes = new Memory<int>(pressurePointIndexes.ToArray());
    }

    public ReadOnlyCollection<PressureTable> Transform()
    {
        var pressureTables = new List<PressureTable>();
        var optimizedContext = Result.OptimizedContext;

        for (int i = 0; i < _pressurePointIndexes.Length; i++)
        {
            pressureTables.Add(CreatePressureTable(_pressurePointIndexes.Span[i]));
        }

        return pressureTables.AsReadOnly();
    }

    private PressureTable CreatePressureTable(int pressurePointIndex)
    {
        var propellantCount = Result.OptimizedContext.PropellantCount;
        var columnCount = 2 * propellantCount + 1;
        var optimizedContext = Result.OptimizedContext;
        var problemContext = optimizedContext.ProblemContextMatrix[0, pressurePointIndex];
        double[] columnProportions = new double[columnCount];
        columnProportions[0] = 2.0;
        for (int i = 1; i < columnCount; i++)
            columnProportions[i] = 1.0;
        var rows = new List<List<string>>();

        var row = new List<string>();

        // Add header
        row.Add(problemContext.Pressure.ToUnit(PressureUnit.Megapascal).ToString());
        for (int i = 0; i < columnCount - 1; i++)
            row.Add(string.Empty);
        rows.Add(row.ToList());

        // Add propellant names
        row = new List<string>();
        row.Add(PressureTablesReportResources.Characteristics);
        for (int i = 0; i < propellantCount; i++)
        {
            row.Add(optimizedContext.ProblemContextMatrix[i, pressurePointIndex].Propellant.Name);
            row.Add(string.Empty);
        }
        rows.Add(row.ToList());

        // Add experimental burning rates
        row = new List<string>();
        row.Add(PressureTablesReportResources.ExperimentalBurningRate);
        for (int i = 0; i < propellantCount; i++)
        {
            row.Add(optimizedContext.ExperimentalBurnRates[i, pressurePointIndex].ToUnit(SpeedUnit.MillimeterPerSecond).ToString());
            row.Add(string.Empty);
        }
        rows.Add(row.ToList());

        // Add calculated burning rates
        row = new List<string>();
        row.Add(PressureTablesReportResources.CalculatedBurningRate);
        for (int i = 0; i < propellantCount; i++)
        {
            row.Add(optimizedContext.ProblemContextMatrix[i, pressurePointIndex].MixedCombustionParams.BurnRate.ToUnit(SpeedUnit.MillimeterPerSecond).ToString());
            row.Add(string.Empty);
        }
        rows.Add(row.ToList());

        // Add difference between experimental and calculated burning rates
        row = new List<string>();
        row.Add(PressureTablesReportResources.Difference);
        for (int i = 0; i < propellantCount; i++)
        {
            row.Add((
                optimizedContext.ExperimentalBurnRates[i, pressurePointIndex]
                - optimizedContext.ProblemContextMatrix[i, pressurePointIndex].MixedCombustionParams.BurnRate
                ).ToUnit(SpeedUnit.MillimeterPerSecond).ToString());
            row.Add(string.Empty);
        }
        rows.Add(row.ToList());

        // Add another header
        row = new List<string>();
        row.Add(PressureTablesReportResources.Separator);
        for (int i = 0; i < propellantCount; i++)
        {
            row.Add(PressureTablesReportResources.InterPocketColumn);
            row.Add(PressureTablesReportResources.PocketColumn);
        }
        rows.Add(row.ToList());

        // Add burning rate for inter-pocket and pocket
        row = new List<string>();
        row.Add(PressureTablesReportResources.LinearBurningRate);
        for (int i = 0; i < propellantCount; i++)
        {
            row.Add(optimizedContext.ProblemContextMatrix[i, pressurePointIndex].InterPocketCombustionParams.BurnRate.ToUnit(SpeedUnit.MillimeterPerSecond).ToString());
            row.Add(optimizedContext.ProblemContextMatrix[i, pressurePointIndex].PocketCombustionParams.BurnRate.ToUnit(SpeedUnit.MillimeterPerSecond).ToString());
        }
        rows.Add(row.ToList());

        // Add surface temperature for inter-pocket and pocket
        row = new List<string>();
        row.Add(PressureTablesReportResources.SurfaceTemperature);
        for (int i = 0; i < propellantCount; i++)
        {
            row.Add(optimizedContext.ProblemContextMatrix[i, pressurePointIndex].InterPocketCombustionParams.SurfaceTemperature.ToUnit(TemperatureUnit.Kelvin).ToString());
            row.Add(optimizedContext.ProblemContextMatrix[i, pressurePointIndex].PocketCombustionParams.SurfaceTemperature.ToUnit(TemperatureUnit.Kelvin).ToString());
        }
        rows.Add(row.ToList());

        // Add inter-pocket kinetic flame heat flux
        row = new List<string>();
        row.Add(PressureTablesReportResources.InterPocketKineticFlameHeatFlux);
        for (int i = 0; i < propellantCount; i++)
        {
            row.Add(optimizedContext.ProblemContextMatrix[i, pressurePointIndex].InterPocketCombustionParams.KineticFlameCombustionParams.KineticFlameHeatFlux.ToUnit(HeatFluxUnit.WattPerSquareMeter).ToString());
            row.Add(string.Empty);
        }
        rows.Add(row.ToList());

        // Add inter-pocket kinetic flame height
        row = new List<string>();
        row.Add(PressureTablesReportResources.InterPocketKineticFlameHeight);
        for (int i = 0; i < propellantCount; i++)
        {
            row.Add(optimizedContext.ProblemContextMatrix[i, pressurePointIndex].InterPocketCombustionParams.KineticFlameCombustionParams.KineticFlameHeight.ToUnit(LengthUnit.Micrometer).ToString());
            row.Add(string.Empty);
        }

        // Add pocket kinetic flame heat flux
        row = new List<string>();
        row.Add(PressureTablesReportResources.SkeletonKineticFlameHeatFlux);
        for (int i = 0; i < propellantCount; i++)
        {
            row.Add(optimizedContext.ProblemContextMatrix[i, pressurePointIndex].PocketCombustionParams.SkeletonKineticFlameCombustionParams.KineticFlameHeatFlux.ToUnit(HeatFluxUnit.WattPerSquareMeter).ToString());
            row.Add(string.Empty);
        }
        rows.Add(row.ToList());

        // Add pocket kinetic flame height
        row = new List<string>();
        row.Add(PressureTablesReportResources.SkeletonKineticFlameHeight);
        for (int i = 0; i < propellantCount; i++)
        {
            row.Add(optimizedContext.ProblemContextMatrix[i, pressurePointIndex].PocketCombustionParams.SkeletonKineticFlameCombustionParams.KineticFlameHeight.ToUnit(LengthUnit.Micrometer).ToString());
            row.Add(string.Empty);
        }
        rows.Add(row.ToList());

        // Add out-pocket kinetic flame heat flux
        row = new List<string>();
        row.Add(PressureTablesReportResources.OutSkeletonKineticFlameHeatFlux);
        for (int i = 0; i < propellantCount; i++)
        {
            row.Add(optimizedContext.ProblemContextMatrix[i, pressurePointIndex].PocketCombustionParams.OutSkeletonKineticFlameCombustionParams.KineticFlameHeatFlux.ToUnit(HeatFluxUnit.WattPerSquareMeter).ToString());
            row.Add(string.Empty);
        }
        rows.Add(row.ToList());

        // Add out-pocket kinetic flame height
        row = new List<string>();
        row.Add(PressureTablesReportResources.OutSkeletonKineticFlameHeight);
        for (int i = 0; i < propellantCount; i++)
        {
            row.Add(optimizedContext.ProblemContextMatrix[i, pressurePointIndex].PocketCombustionParams.OutSkeletonKineticFlameCombustionParams.KineticFlameHeight.ToUnit(LengthUnit.Micrometer).ToString());
            row.Add(string.Empty);
        }
        rows.Add(row.ToList());

        // Add diffusion flame heat flux
        row = new List<string>();
        row.Add(PressureTablesReportResources.DiffusionFlameHeatFlux);
        for (int i = 0; i < propellantCount; i++)
        {
            row.Add(optimizedContext.ProblemContextMatrix[i, pressurePointIndex].PocketCombustionParams.DiffusionFlameHeatFlux.ToUnit(HeatFluxUnit.WattPerSquareMeter).ToString());
            row.Add(string.Empty);
        }
        rows.Add(row.ToList());

        // Add diffusion flame height
        row = new List<string>();
        row.Add(PressureTablesReportResources.DiffusionFlameHeight);
        for (int i = 0; i < propellantCount; i++)
        {
            row.Add(optimizedContext.ProblemContextMatrix[i, pressurePointIndex].PocketCombustionParams.DiffusionFlameHeight.ToUnit(LengthUnit.Micrometer).ToString());
            row.Add(string.Empty);
        }
        rows.Add(row.ToList());

        // Add metal heat flux
        row = new List<string>();
        row.Add(PressureTablesReportResources.MetalHeatFlux);
        for (int i = 0; i < propellantCount; i++)
        {
            row.Add(optimizedContext.ProblemContextMatrix[i, pressurePointIndex].PocketCombustionParams.MetalBurningHeatFlux.ToUnit(HeatFluxUnit.WattPerSquareMeter).ToString());
            row.Add(string.Empty);
        }
        rows.Add(row.ToList());

        var pressureTable = new PressureTable
        (
            columnProportions: columnProportions,
            rows: rows
        );

        return pressureTable;
    }
}
