# API.md — Models

Пространство имён `ParametricCombustionModel.ReportMaking.Models`.

## Report input ✅

```csharp
public record GroupReportContextDto
{
    public GroupOptimizationResult GroupOptimizationResult { get; init; }
    public string PropellantsFilePath { get; init; }
    public DifferentialEvolutionSettings? DifferentialEvolutionSettings { get; init; }
    public PerformanceMeter? Meter { get; init; }
    public IReadOnlyList<RunConfigurationSection>? RunConfiguration { get; init; }

    public GroupReportContextDto(
        GroupOptimizationResult groupOptimizationResult,
        string propellantsFilePath,
        DifferentialEvolutionSettings? differentialEvolutionSettings = null,
        PerformanceMeter? meter = null,
        IReadOnlyList<RunConfigurationSection>? runConfiguration = null);
}
```

## Run configuration summary ✅

```csharp
// Готовый текст: хост форматирует, отчёт печатает дословно.
public sealed record RunConfigurationSection(string Title, IReadOnlyList<RunConfigurationEntry> Entries);
public sealed record RunConfigurationEntry(string Label, string Value);
```
