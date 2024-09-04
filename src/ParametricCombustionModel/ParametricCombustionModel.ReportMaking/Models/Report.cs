using System.Text;
using ParametricCombustionModel.ReportMaking.Interfaces;

namespace ParametricCombustionModel.ReportMaking.Models;

public record Report(
    PointReport Point,
    double TargetFunctionValue,
    IEnumerable<PropellantReport> PropellantReports
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
        strBuilder.AppendLine(Point.Transform());

        foreach (var line in GetLocalLines())
            strBuilder.AppendLine(line);
        foreach (var propellantReport in PropellantReports)
            strBuilder.AppendLine()
                      .Append(propellantReport);

        return strBuilder;
    }

    public IEnumerable<string> GetLocalLines()
    {
        string[] lines =
        [
            $"Значение целевой функции {TargetFunctionValue:#,0.000000}"
        ];

        foreach (var line in lines)
            yield return line;
    }
}
