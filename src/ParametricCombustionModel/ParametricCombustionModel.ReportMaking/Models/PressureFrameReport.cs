using System.Text;
using ParametricCombustionModel.ReportMaking.Interfaces;

namespace ParametricCombustionModel.ReportMaking.Models;

public record PressureFrameReport(
    double Pressure,
    double BurningRate,
    double ExperimentalBurningRate,
    PocketPropellantReport PocketPropellantReport,
    InterPocketPropellantReport InterPocketPropellantReport
) : ITransformable<string>
{
    public string Transform()
    {
        return this.ToString();
    }

    public override string ToString()
    {
        var strBuilder = new StringBuilder();
        return this.MergeTo(strBuilder).ToString();
    }

    public StringBuilder MergeTo(StringBuilder strBuilder)
    {
        foreach (var line in GetLocalLines())
            strBuilder.AppendLine(line);
        return strBuilder;
    }

    protected IEnumerable<string> GetLocalLines()
    {
        string[] lines =
        [
            $"При давлении {Pressure:#,0.0} Па",
            $"Экспериментальная скорость горения \t{ExperimentalBurningRate * 1e3:#,0.000000} мм/с",
            $"Расчетная скорость горения \t\t\t{BurningRate * 1e3:#,0.000000} мм/с",
            $"    Первое условное топливо:",
            $"        Температура поверхности {InterPocketPropellantReport.SurfaceTemperature:#,0.000000} К",
            $"        Линейная скорость горения {InterPocketPropellantReport.BurningRate * 1e3:#,0.000000} мм/с",
            $"        Массовая скорость разложения {InterPocketPropellantReport.DecomposingRate:#,0.000000} кг/(с*м^2)",
            $"        Плотность теплового потока кинетического пламени {InterPocketPropellantReport.KineticFlameHeatFlow:#,0.000000}",
            $"        Высота кинетического пламени {InterPocketPropellantReport.KineticFlameHeight * 1e6:#,0.000000} мкм",
            $"        Вклад в интегральную скорость горения {InterPocketPropellantReport.BurningRateFraction * 1e2:#,0.000000} %",
            $"    Второе условное топливо:",
            $"        Температура поверхности {PocketPropellantReport.SurfaceTemperature:#,0.000000} К",
            $"        Линейная скорость горения {PocketPropellantReport.BurningRate * 1e3:#,0.000000} мм/с",
            $"        Массовая скорость разложения {PocketPropellantReport.DecomposingRate:#,0.000000} кг/(с*м^2)",
            $"        Плотность теплового потока кинетического пламени (КС) {PocketPropellantReport.SkeletonKineticFlameHeatFlow:#,0.000000}",
            $"        Высота кинетического пламени (КС) {PocketPropellantReport.SkeletonKineticFlameHeight * 1e6:#,0.000000} мкм",
            $"        Плотность теплового потока кинетического пламени (без КС) {PocketPropellantReport.OutSkeletonKineticFlameHeatFlow:#,0.000000}",
            $"        Высота кинетического пламени (без КС) {PocketPropellantReport.OutSkeletonKineticFlameHeight * 1e6:#,0.000000} мкм",
            $"        Плотность теплового потока диффузионного пламени {PocketPropellantReport.DiffusionFlameHeatFlow:#,0.000000}",
            $"        Плотность теплового потока от горящего металла {PocketPropellantReport.MetalBurningHeatFlow:#,0.000000}",
            $"        Отношение тепловых потоков (max/min) {PocketPropellantReport.HeatFlowMaxMinDeviation:#,0.000000}",
            $"        Вклад в интегральную скорость горения {PocketPropellantReport.BurningRateFraction * 1e2:#,0.000000} %"
        ];

        foreach (var line in lines)
            yield return line;
    }
}
