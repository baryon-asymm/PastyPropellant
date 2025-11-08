using System.Collections.ObjectModel;
using DotNetDifferentialEvolution;
using ParametricCombustionModel.Core.Models;
using ParametricCombustionModel.Optimization.Models;
using ParametricCombustionModel.Optimization.Settings;
using ParametricCombustionModel.Telemetry;

namespace ParametricCombustionModel.ReportMaking.Models;

public record ReportContextDto
{
    public OptimizationResult OptimizationResult { get; init; }

    public string PropellantsFilePath { get; init; }

    public ReadOnlyCollection<Propellant> Propellants { get; init; }
    
    public DifferentialEvolutionSettings? DifferentialEvolutionSettings { get; init; }

    public PerformanceMeter? Meter { get; init; }

    public ReportContextDto(
        OptimizationResult optimizationResult,
        string propellantsFilePath,
        ReadOnlyCollection<Propellant> propellants,
        DifferentialEvolutionSettings? differentialEvolutionSettings = null,
        PerformanceMeter? meter = null)
    {
        OptimizationResult = optimizationResult ?? throw new ArgumentNullException(nameof(optimizationResult));
        PropellantsFilePath = propellantsFilePath ?? throw new ArgumentNullException(nameof(propellantsFilePath));
        Propellants = propellants ?? throw new ArgumentNullException(nameof(propellants));
        
        if (string.IsNullOrWhiteSpace(propellantsFilePath))
            throw new ArgumentException("Propellants file path cannot be null or whitespace.", nameof(propellantsFilePath));

        DifferentialEvolutionSettings = differentialEvolutionSettings;
        Meter = meter;
    }
}
