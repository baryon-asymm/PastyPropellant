using System.Collections.ObjectModel;
using System.Text;
using ParametricCombustionModel.ReportMaking.Interfaces;

namespace ParametricCombustionModel.ReportMaking.Models;

public record PointReport(
    ReadOnlyCollection<double> Vector
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
            $"Предэкспонента разложения связующего {Vector[0]}",
            $"Энергия активации разложения связующего {Vector[1]}",
            $"Предэкспонента кинетического пламени (МКМ) {Vector[2]}",
            $"Энергия активации кинетического пламени (МКМ) {Vector[3]}",
            $"Предэкспонента кинетического пламени (карман, вне КС) {Vector[4]}",
            $"Энергия активации кинетического пламени (карман, вне КС) {Vector[5]}",
            $"Предэкспонента кинетического пламени (карман, КС) {Vector[6]}",
            $"Энергия активации кинетического пламени (карман, КС) {Vector[7]}",
            $"Порядок химических реакций в кинетическом пламени (не варьируется) {Vector[8]}",
            $"Множитель в плотности теплового потока от горящего металла {Vector[9]}",
            $"Энергия активации в горящем металле {Vector[10]}",
            $"Разница в теплоте газификации и теплоте разложения {Vector[11]}",
            $"Коэффициент высоты диффузионного пламени {Vector[12]}"
        ];

        foreach (var line in lines)
            yield return line;
    }
}
