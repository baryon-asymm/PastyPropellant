using System.Collections.ObjectModel;
using ParametricCombustionModel.Optimization.Models;
using ParametricCombustionModel.ReportMaking.Interfaces;
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
        row.Add("Характеристики");
        for (int i = 0; i < propellantCount; i++)
        {
            row.Add(optimizedContext.ProblemContextMatrix[i, pressurePointIndex].Propellant.Name);
            row.Add(string.Empty);
        }
        rows.Add(row.ToList());

        // Add experimental burning rates
        row = new List<string>();
        row.Add("Экспериментальная скорость горения");
        for (int i = 0; i < propellantCount; i++)
        {
            row.Add(optimizedContext.ExperimentalBurnRates[i, pressurePointIndex].ToUnit(SpeedUnit.MillimeterPerSecond).ToString());
            row.Add(string.Empty);
        }
        rows.Add(row.ToList());

        // Add calculated burning rates
        row = new List<string>();
        row.Add("Расчетная скорость горения");
        for (int i = 0; i < propellantCount; i++)
        {
            row.Add(optimizedContext.ProblemContextMatrix[i, pressurePointIndex].MixedCombustionParams.BurnRate.ToUnit(SpeedUnit.MillimeterPerSecond).ToString());
            row.Add(string.Empty);
        }
        rows.Add(row.ToList());

        // Add difference between experimental and calculated burning rates
        row = new List<string>();
        row.Add("Разность");
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
        row.Add("-");
        for (int i = 0; i < propellantCount; i++)
        {
            row.Add("I");
            row.Add("II");
        }
        rows.Add(row.ToList());

        // Add burning rate for inter-pocket and pocket
        row = new List<string>();
        row.Add("Линейная скорость горения");
        for (int i = 0; i < propellantCount; i++)
        {
            row.Add(optimizedContext.ProblemContextMatrix[i, pressurePointIndex].InterPocketCombustionParams.BurnRate.ToUnit(SpeedUnit.MillimeterPerSecond).ToString());
            row.Add(optimizedContext.ProblemContextMatrix[i, pressurePointIndex].PocketCombustionParams.BurnRate.ToUnit(SpeedUnit.MillimeterPerSecond).ToString());
        }
        rows.Add(row.ToList());

        // Add surface temperature for inter-pocket and pocket
        row = new List<string>();
        row.Add("Температура поверхности");
        for (int i = 0; i < propellantCount; i++)
        {
            row.Add(optimizedContext.ProblemContextMatrix[i, pressurePointIndex].InterPocketCombustionParams.SurfaceTemperature.ToUnit(TemperatureUnit.Kelvin).ToString());
            row.Add(optimizedContext.ProblemContextMatrix[i, pressurePointIndex].PocketCombustionParams.SurfaceTemperature.ToUnit(TemperatureUnit.Kelvin).ToString());
        }
        rows.Add(row.ToList());

        // Add inter-pocket kinetic flame heat flux
        row = new List<string>();
        row.Add("Плотность теплового потока кинетического пламени (МКМ)");
        for (int i = 0; i < propellantCount; i++)
        {
            row.Add(optimizedContext.ProblemContextMatrix[i, pressurePointIndex].InterPocketCombustionParams.KineticFlameCombustionParams.KineticFlameHeatFlux.ToUnit(HeatFluxUnit.WattPerSquareMeter).ToString());
            row.Add(string.Empty);
        }
        rows.Add(row.ToList());

        // Add inter-pocket kinetic flame height
        row = new List<string>();
        row.Add("Высота кинетического пламени (МКМ)");
        for (int i = 0; i < propellantCount; i++)
        {
            row.Add(optimizedContext.ProblemContextMatrix[i, pressurePointIndex].InterPocketCombustionParams.KineticFlameCombustionParams.KineticFlameHeight.ToUnit(LengthUnit.Micrometer).ToString());
            row.Add(string.Empty);
        }

        // Add pocket kinetic flame heat flux
        row = new List<string>();
        row.Add("Плотность теплового потока кинетического пламени (КС)");
        for (int i = 0; i < propellantCount; i++)
        {
            row.Add(optimizedContext.ProblemContextMatrix[i, pressurePointIndex].PocketCombustionParams.SkeletonKineticFlameCombustionParams.KineticFlameHeatFlux.ToUnit(HeatFluxUnit.WattPerSquareMeter).ToString());
            row.Add(string.Empty);
        }
        rows.Add(row.ToList());

        // Add pocket kinetic flame height
        row = new List<string>();
        row.Add("Высота кинетического пламени (КС)");
        for (int i = 0; i < propellantCount; i++)
        {
            row.Add(optimizedContext.ProblemContextMatrix[i, pressurePointIndex].PocketCombustionParams.SkeletonKineticFlameCombustionParams.KineticFlameHeight.ToUnit(LengthUnit.Micrometer).ToString());
            row.Add(string.Empty);
        }
        rows.Add(row.ToList());

        // Add out-pocket kinetic flame heat flux
        row = new List<string>();
        row.Add("Плотность теплового потока кинетического пламени (вне КС)");
        for (int i = 0; i < propellantCount; i++)
        {
            row.Add(optimizedContext.ProblemContextMatrix[i, pressurePointIndex].PocketCombustionParams.OutSkeletonKineticFlameCombustionParams.KineticFlameHeatFlux.ToUnit(HeatFluxUnit.WattPerSquareMeter).ToString());
            row.Add(string.Empty);
        }
        rows.Add(row.ToList());

        // Add out-pocket kinetic flame height
        row = new List<string>();
        row.Add("Высота кинетического пламени (вне КС)");
        for (int i = 0; i < propellantCount; i++)
        {
            row.Add(optimizedContext.ProblemContextMatrix[i, pressurePointIndex].PocketCombustionParams.OutSkeletonKineticFlameCombustionParams.KineticFlameHeight.ToUnit(LengthUnit.Micrometer).ToString());
            row.Add(string.Empty);
        }
        rows.Add(row.ToList());

        // Add diffusion flame heat flux
        row = new List<string>();
        row.Add("Плотность теплового потока диффузионного пламени");
        for (int i = 0; i < propellantCount; i++)
        {
            row.Add(optimizedContext.ProblemContextMatrix[i, pressurePointIndex].PocketCombustionParams.DiffusionFlameHeatFlux.ToUnit(HeatFluxUnit.WattPerSquareMeter).ToString());
            row.Add(string.Empty);
        }
        rows.Add(row.ToList());

        // Add diffusion flame height
        row = new List<string>();
        row.Add("Высота диффузионного пламени");
        for (int i = 0; i < propellantCount; i++)
        {
            row.Add(optimizedContext.ProblemContextMatrix[i, pressurePointIndex].PocketCombustionParams.DiffusionFlameHeight.ToUnit(LengthUnit.Micrometer).ToString());
            row.Add(string.Empty);
        }
        rows.Add(row.ToList());

        // Add metal heat flux
        row = new List<string>();
        row.Add("Плотность теплового потока металла");
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