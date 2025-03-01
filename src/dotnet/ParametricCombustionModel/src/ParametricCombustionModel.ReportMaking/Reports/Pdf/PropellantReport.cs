using ParametricCombustionModel.Core.Models;
using ParametricCombustionModel.Core.Models.GasPhases;
using ParametricCombustionModel.Optimization.Models;
using ParametricCombustionModel.ReportMaking.Enums;
using ParametricCombustionModel.ReportMaking.Interfaces;
using ParametricCombustionModel.ReportMaking.PdfOperations;
using ParametricCombustionModel.ReportMaking.Resources;
using UnitsNet;

namespace ParametricCombustionModel.ReportMaking.Reports.Pdf;

public class PropellantReport : BaseReport, ITransformable<Queue<IPdfOperation>>
{
    public PropellantReport(
        OptimizationResult optimizationResult) : base(optimizationResult)
    {
    }

    public Queue<IPdfOperation> Transform()
    {
        var propellants = GetPropellants(Result);
        var operations = new Queue<IPdfOperation>();

        operations.Enqueue(new PrintTextOperation(PropellantReportResources.HeaderOfPropellants, TextStyle.Bold));
        operations.Enqueue(new LineBreakOperation());
        foreach (var propellant in propellants)
        {
            operations.Enqueue(new PrintTextOperation(
                                   string.Format(PropellantReportResources.NameOfPropellant,
                                                 propellant.Name),
                                   TextStyle.Italic | TextStyle.Bold));
            operations.Enqueue(new LineBreakOperation());
            operations.Enqueue(new PrintTextOperation(
                                   string.Format(PropellantReportResources.PropellantShortInfoPlaceholder,
                                                 Density.FromKilogramsPerCubicMeter(propellant.Density),
                                                 SpecificEntropy.FromJoulesPerKilogramKelvin(
                                                     propellant.SpecificHeatCapacity),
                                                 Temperature.FromKelvins(propellant.InitialTemperature),
                                                 Ratio.FromDecimalFractions(propellant.PocketMassFraction)),
                                   TextStyle.None));
            operations.Enqueue(new LineBreakOperation());
            operations.Enqueue(new PrintTextOperation(
                                   string.Format(PropellantReportResources.VieillesLaw,
                                                 propellant.A,
                                                 propellant.Nu),
                                   TextStyle.None));
            operations.Enqueue(new LineBreakOperation());

            operations.Enqueue(new PrintTextOperation(PropellantReportResources.HeaderOfInterPocketGasPhase,
                                                      TextStyle.Italic));
            operations.Enqueue(new LineBreakOperation());
            AddHomogeneousGasPhaseInfo(operations, propellant.PressureFrames!.ElementAt(propellant.PressureFrames!.Count()).InterPocketGasPhase, 1);

            operations.Enqueue(new PrintTextOperation(PropellantReportResources.HeaderOfPocketGasPhase,
                                                      TextStyle.Italic));
            operations.Enqueue(new LineBreakOperation());

            AddHeterogeneousGasPhaseInfo(operations, propellant.PressureFrames!.ElementAt(propellant.PressureFrames!.Count()).PocketGasPhase, 1);
        }

        return operations;
    }

    private void AddHeterogeneousGasPhaseInfo(
        Queue<IPdfOperation> operations,
        HeterogeneousGasPhase heterogeneousGasPhase,
        int tabsCount)
    {
        AddTabs(operations, tabsCount);
        operations.Enqueue(new PrintTextOperation(
                               string.Format(PropellantReportResources.LamdaGas,
                                             ThermalConductivity.FromWattsPerMeterKelvin(
                                                 heterogeneousGasPhase.Lambda_Gas)),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());

        AddTabs(operations, tabsCount);
        operations.Enqueue(new PrintTextOperation(
                               string.Format(PropellantReportResources.AverageMolarMass,
                                             MolarMass.FromKilogramsPerMole(heterogeneousGasPhase.AverageMolarMass)),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());

        AddTabs(operations, tabsCount);
        operations.Enqueue(new PrintTextOperation(
                               string.Format(PropellantReportResources.SpecificHeatCapacity_Volume,
                                             SpecificEntropy.FromJoulesPerKilogramKelvin(
                                                 heterogeneousGasPhase.SpecificHeatCapacity_Volume)),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());

        AddTabs(operations, tabsCount);
        operations.Enqueue(new PrintTextOperation(
                               string.Format(PropellantReportResources.DiffusionFlameTemperature,
                                             Temperature.FromKelvins(heterogeneousGasPhase.DiffusionFlameTemperature)),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());

        AddTabs(operations, tabsCount);
        operations.Enqueue(new PrintTextOperation(PropellantReportResources.HeaderOfSkeletonGasPhase,
                                                  TextStyle.Italic));
        operations.Enqueue(new LineBreakOperation());
        AddHomogeneousGasPhaseInfo(operations, heterogeneousGasPhase.SkeletonGasPhase, tabsCount + 1);

        AddTabs(operations, tabsCount);
        operations.Enqueue(new PrintTextOperation(PropellantReportResources.HeaderOfOutSkeletonGasPhase,
                                                  TextStyle.Italic));
        operations.Enqueue(new LineBreakOperation());
        AddHomogeneousGasPhaseInfo(operations, heterogeneousGasPhase.OutSkeletonGasPhase, tabsCount + 1);
    }

    private void AddHomogeneousGasPhaseInfo(
        Queue<IPdfOperation> operations,
        HomogeneousGasPhase homogeneousGasPhase,
        int tabsCount)
    {
        AddTabs(operations, tabsCount);
        operations.Enqueue(new PrintTextOperation(
                               string.Format(PropellantReportResources.LamdaGas,
                                             ThermalConductivity.FromWattsPerMeterKelvin(
                                                 homogeneousGasPhase.Lambda_Gas)),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());

        AddTabs(operations, tabsCount);
        operations.Enqueue(new PrintTextOperation(
                               string.Format(PropellantReportResources.AverageMolarMass,
                                             MolarMass.FromKilogramsPerMole(homogeneousGasPhase.AverageMolarMass)),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());

        AddTabs(operations, tabsCount);
        operations.Enqueue(new PrintTextOperation(
                               string.Format(PropellantReportResources.SpecificHeatCapacity_Volume,
                                             SpecificEntropy.FromJoulesPerKilogramKelvin(
                                                 homogeneousGasPhase.SpecificHeatCapacity_Volume)),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());

        AddTabs(operations, tabsCount);
        operations.Enqueue(new PrintTextOperation(
                               string.Format(PropellantReportResources.KineticFlameTemperature,
                                             Temperature.FromKelvins(homogeneousGasPhase.KineticFlameTemperature)),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
    }

    private void AddTabs(
        Queue<IPdfOperation> operations,
        int count)
    {
        for (var i = 0; i < count; i++)
        {
            operations.Enqueue(new AddTabOperation());
        }
    }

    private static IEnumerable<Propellant> GetPropellants(
        OptimizationResult optimizationResult)
    {
        var optimizedContext = optimizationResult.OptimizedContext;
        return Enumerable.Range(0, optimizedContext.PropellantCount)
                         .Select(i => optimizedContext.ProblemContextMatrix[i, 0].Propellant);
    }
}
