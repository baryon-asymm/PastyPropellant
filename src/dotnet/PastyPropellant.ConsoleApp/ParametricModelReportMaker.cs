namespace PastyPropellant.ConsoleApp;

public class ParametricModelReportMaker
{
    public string MakeReport(IEnumerable<double> point, Report report)
    {
        return $"{GetPointBody(point.ToArray())}\n" +
               $"\n" +
               $"{GetTargetFunctionValueBody(report)}\n" +
               $"\n" +
               $"{GetCombinedPropellantReports(report.PropellantReports)}";
    }

    private string GetPointBody(Span<double> point)
    {
        return $"Предэкспонента разложения связующего {point[0]}\n" +
               $"Энергия активации разложения связующего {point[1]}\n" +
               $"Предэкспонента кинетического пламени {point[2]}\n" +
               $"Энергия активации кинетического пламени {point[3]}\n" +
               $"Порядок химических реакций в кинетическом пламени (не варьируется) {point[4]}\n" +
               $"Множитель в плотности теплового потока от горящего металла {point[5]}\n" +
               $"Энергия активации в горящем металле {point[6]}\n" +
               $"Разница в теплоте газификации и теплоте разложения {point[7]}";
    }

    private string GetTargetFunctionValueBody(Report report)
    {
        return $"Значение целевой функции {0.0:#,0.000000}";
    }

    private string GetCombinedPropellantReports(IEnumerable<PropellantReport> reports)
    {
        var result = string.Empty;
        foreach (var report in reports)
        {
            if (result != string.Empty)
                result += "\n\n";
            result += GetPropellantBody(report);
        }

        return result;
    }

    private string GetPropellantBody(PropellantReport report)
    {
        return $"Топливо {report.Propellant.Name}:\n" +
               $"Объемная доля первого условного топлива {report.InterPocketPropellantVolumeFraction:#,0.000000}\n" +
               $"Объемная доля второго условного топлива {report.PocketPropellantVolumeFraction:#,0.000000}\n" +
               $"\n" +
               $"{GetCombinedPressureFrames(report.PressureFrameReports)}";
    }

    private string GetCombinedPressureFrames(IEnumerable<PressureFrameReport> reports)
    {
        var result = string.Empty;
        foreach (var report in reports)
        {
            if (result != string.Empty)
                result += "\n\n";
            result += GetPressureFrameBody(report);
        }

        return result;
    }

    private string GetPressureFrameBody(PressureFrameReport report)
    {
        return $"При давлении {report.Pressure:#,0.000000} Па\n" +
               $"Экспериментальная скорость горения {report.ExperimentalBurningRate * 1e3:#,0.000000} мм/с\n" +
               $"Расчетная скорость горения {report.BurningRate * 1e3:#,0.000000} мм/с\n" +
               $"    Первое условное топливо:\n" +
               $"        Температура поверхности {report.InterPocketPropellantReport.SurfaceTemperature:#,0.000000} К\n" +
               $"        Линейная скорость горения {report.InterPocketPropellantReport.BurningRate * 1e3:#,0.000000} мм/с\n" +
               $"        Массовая скорость разложения {report.InterPocketPropellantReport.DecomposingRate:#,0.000000} кг/(с*м^2)\n" +
               $"        Плотность теплового потока кинетического пламени {report.InterPocketPropellantReport.KineticFlameHeatFlow:#,0.000000}\n" +
               $"        Вклад в интегральную скорость горения {report.InterPocketPropellantReport.BurningRateFraction * 1e2:#,0.000000} %\n" +
               $"    Второе условное топливо:\n" +
               $"        Температура поверхности {report.PocketPropellantReport.SurfaceTemperature:#,0.000000} К\n" +
               $"        Линейная скорость горения {report.PocketPropellantReport.BurningRate * 1e3:#,0.000000} мм/с\n" +
               $"        Массовая скорость разложения {report.PocketPropellantReport.DecomposingRate:#,0.000000} кг/(с*м^2)\n" +
               $"        Плотность теплового потока кинетического пламени (КС) {report.PocketPropellantReport.SkeletonKineticFlameHeatFlow:#,0.000000}\n" +
               $"        Плотность теплового потока кинетического пламени (без КС) {report.PocketPropellantReport.OutSkeletonKineticFlameHeatFlow:#,0.000000}\n" +
               $"        Плотность теплового потока диффузионного пламени {report.PocketPropellantReport.DiffusionFlameHeatFlow:#,0.000000}\n" +
               $"        Плотность теплового потока от горящего металла {report.PocketPropellantReport.MetalBurningHeatFlow:#,0.000000}\n" +
               $"        Вклад в интегральную скорость горения {report.PocketPropellantReport.BurningRateFraction * 1e2:#,0.000000} %";
    }
}
