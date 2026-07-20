using DotNetDifferentialEvolution;
using ParametricCombustionModel.Optimization.Results;
using ParametricCombustionModel.Optimization.Settings;
using ParametricCombustionModel.Telemetry;

namespace ParametricCombustionModel.ReportMaking.Models;

public record GroupReportContextDto
{
    public GroupOptimizationResult GroupOptimizationResult { get; init; }

    public string PropellantsFilePath { get; init; }

    public DifferentialEvolutionSettings? DifferentialEvolutionSettings { get; init; }

    public PerformanceMeter? Meter { get; init; }

    public GroupReportContextDto(
        GroupOptimizationResult groupOptimizationResult,
        string propellantsFilePath,
        DifferentialEvolutionSettings? differentialEvolutionSettings = null,
        PerformanceMeter? meter = null)
    {
        GroupOptimizationResult = groupOptimizationResult ?? throw new ArgumentNullException(nameof(groupOptimizationResult));
        PropellantsFilePath = propellantsFilePath ?? throw new ArgumentNullException(nameof(propellantsFilePath));

        if (string.IsNullOrWhiteSpace(propellantsFilePath))
            throw new ArgumentException("Propellants file path cannot be null or whitespace.", nameof(propellantsFilePath));

        DifferentialEvolutionSettings = differentialEvolutionSettings;
        Meter = meter;
    }
}
