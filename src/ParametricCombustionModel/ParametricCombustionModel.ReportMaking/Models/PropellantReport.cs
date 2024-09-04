using System.Text;
using ParametricCombustionModel.Core.Models;
using ParametricCombustionModel.ReportMaking.Interfaces;

namespace ParametricCombustionModel.ReportMaking.Models;

public record PropellantReport(
    Propellant Propellant,
    double PocketPropellantVolumeFraction,
    double InterPocketPropellantVolumeFraction,
    IEnumerable<PressureFrameReport> PressureFrameReports
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
        foreach (var pressureFrameReport in PressureFrameReports)
            strBuilder.AppendLine()
                      .Append(pressureFrameReport);

        return strBuilder;
    }
    
    protected IEnumerable<string> GetLocalLines()
    {
        string[] lines =
        [
            $"Топливо {Propellant.Name}",
            $"Объемная доля первого условного топлива {InterPocketPropellantVolumeFraction:#,0.000000}",
            $"Объемная доля второго условного топлива {PocketPropellantVolumeFraction:#,0.000000}"
        ];

        foreach (var line in lines)
            yield return line;
    }
}
