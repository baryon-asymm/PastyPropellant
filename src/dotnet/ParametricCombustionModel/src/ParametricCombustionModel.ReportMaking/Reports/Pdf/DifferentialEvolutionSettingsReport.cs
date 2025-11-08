using ParametricCombustionModel.Optimization.Models;
using ParametricCombustionModel.ReportMaking.Enums;
using ParametricCombustionModel.ReportMaking.Interfaces;
using ParametricCombustionModel.ReportMaking.PdfOperations;
using ParametricCombustionModel.ReportMaking.Resources;
using ParametricCombustionModel.ReportMaking.Models;
using ParametricCombustionModel.Optimization.Settings;

namespace ParametricCombustionModel.ReportMaking.Reports.Pdf;

public class DifferentialEvolutionSettingsReport : ITransformable<Queue<IPdfOperation>>
{
    private readonly DifferentialEvolutionSettings? _settings;
    private readonly OptimizationResult _optimizationResult;

    public DifferentialEvolutionSettingsReport(ReportContextDto reportContext)
    {
        _settings = reportContext.DifferentialEvolutionSettings;
        _optimizationResult = reportContext.OptimizationResult ?? throw new ArgumentNullException(nameof(reportContext.OptimizationResult));
    }

    public Queue<IPdfOperation> Transform()
    {
        var operations = new Queue<IPdfOperation>();

        if (_settings == null)
        {
            operations.Enqueue(new PrintTextOperation(
                                   "Настройки дифференциальной эволюции не доступны",
                                   TextStyle.Italic));
            operations.Enqueue(new LineBreakOperation());
            return operations;
        }

        operations.Enqueue(new PrintTextOperation(
                               DifferentialEvolutionSettingsReportResources.Header,
                               TextStyle.Bold));
        operations.Enqueue(new LineBreakOperation());

        operations.Enqueue(new PrintTextOperation(
                               string.Format(DifferentialEvolutionSettingsReportResources.PopulationSize,
                                           _settings.PopulationSize),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());

        operations.Enqueue(new PrintTextOperation(
                               string.Format(DifferentialEvolutionSettingsReportResources.MutationForce,
                                           _settings.MutationForce),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());

        operations.Enqueue(new PrintTextOperation(
                               string.Format(DifferentialEvolutionSettingsReportResources.CrossoverProbability,
                                           _settings.CrossoverProbability),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());

        operations.Enqueue(new PrintTextOperation(
                               string.Format(DifferentialEvolutionSettingsReportResources.ThreadsCount,
                                           _settings.ProcessorsCount),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());

        operations.Enqueue(new PrintTextOperation(
                               string.Format(DifferentialEvolutionSettingsReportResources.TerminationStrategy,
                                           _settings.TerminationStrategy.GetType().Name),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());

        return operations;
    }
}