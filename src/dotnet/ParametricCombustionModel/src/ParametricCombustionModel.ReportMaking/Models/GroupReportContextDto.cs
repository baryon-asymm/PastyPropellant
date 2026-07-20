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

    /// <summary>
    /// Pre-formatted summary of the run configuration this result was produced under, or null for a
    /// caller that has none. Printed by <c>RunConfigurationReport</c> as the report's provenance block.
    /// </summary>
    public IReadOnlyList<RunConfigurationSection>? RunConfiguration { get; init; }

    public GroupReportContextDto(
        GroupOptimizationResult groupOptimizationResult,
        string propellantsFilePath,
        DifferentialEvolutionSettings? differentialEvolutionSettings = null,
        PerformanceMeter? meter = null,
        IReadOnlyList<RunConfigurationSection>? runConfiguration = null)
    {
        GroupOptimizationResult = groupOptimizationResult ?? throw new ArgumentNullException(nameof(groupOptimizationResult));
        PropellantsFilePath = propellantsFilePath ?? throw new ArgumentNullException(nameof(propellantsFilePath));

        if (string.IsNullOrWhiteSpace(propellantsFilePath))
            throw new ArgumentException("Propellants file path cannot be null or whitespace.", nameof(propellantsFilePath));

        DifferentialEvolutionSettings = differentialEvolutionSettings;
        Meter = meter;
        RunConfiguration = runConfiguration;
    }
}
