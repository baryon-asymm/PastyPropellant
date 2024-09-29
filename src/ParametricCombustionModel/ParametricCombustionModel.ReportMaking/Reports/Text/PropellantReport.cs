using System.Text;
using ParametricCombustionModel.Core.Models;
using ParametricCombustionModel.Optimization.Models;
using ParametricCombustionModel.ReportMaking.Interfaces;
using ParametricCombustionModel.ReportMaking.Resources;
using UnitsNet;

namespace ParametricCombustionModel.ReportMaking.Reports.Text;

public class PropellantReport : BaseReport, ITransformable<string>
{
    public PropellantReport(
        OptimizationResult optimizationResult) : base(optimizationResult)
    {
    }

    public string Transform()
    {
        var propellants = GetPropellants(Result);
        var stringBuilder = new StringBuilder();

        stringBuilder.AppendLine(PropellantReportResources.HeaderOfPropellants);
        foreach (var propellant in propellants)
        {
            stringBuilder.AppendFormat(PropellantReportResources.PropellantShortInfoPlaceholder,
                                       propellant.Name,
                                       Density.FromKilogramsPerCubicMeter(propellant.Density),
                                       SpecificEntropy.FromJoulesPerKilogramKelvin(propellant.SpecificHeatCapacity),
                                       Temperature.FromKelvins(propellant.InitialTemperature),
                                       Ratio.FromDecimalFractions(propellant.PocketMassFraction));
            stringBuilder.AppendLine();
        }

        return stringBuilder.ToString();
    }

    private static IEnumerable<Propellant> GetPropellants(
        OptimizationResult optimizationResult)
    {
        var optimizedContext = optimizationResult.OptimizedContext;
        return Enumerable.Range(0, optimizedContext.PropellantCount)
                         .Select(i => optimizedContext.ProblemContextMatrix[i, 0].Propellant);
    }
}
