# API.md — Reports/Pdf

Пространство имён `ParametricCombustionModel.ReportMaking.Reports.Pdf`.
Все разделы — преобразования, `Transform()` не имеет побочных эффектов.

## Base for per-composition sections ✅

```csharp
public abstract class PerCompositionPerFuelPdfReport : ITransformable<Queue<IPdfOperation>>
{
    protected PerCompositionPerFuelPdfReport(GroupOptimizationResult groupResult);
    protected GroupOptimizationResult GroupResult { get; }
    protected abstract string Title { get; }
    protected abstract string Introduction { get; }
    protected abstract string CompositionSectionSuffix { get; }
    protected static IReadOnlyList<string> CompositionNames { get; }
    public Queue<IPdfOperation> Transform();
}
```

Наследник задаёт три текста и тело по топливу; необязательные хвосты
(по композиции и в конце отчёта) существуют потому, что разбивка ошибки
сводит числа до целевой функции, а остальные разделы останавливаются на
уровне топлива.

## Sections over the grouped result ✅

```csharp
public class BurnRateErrorReport : PerCompositionPerFuelPdfReport
{
    public BurnRateErrorReport(GroupOptimizationResult groupResult);
}

public class BurnRateVieilleFitReport : PerCompositionPerFuelPdfReport
{
    public BurnRateVieilleFitReport(GroupOptimizationResult groupResult);
}

public class FlameStructureReport : PerCompositionPerFuelPdfReport
{
    public FlameStructureReport(GroupOptimizationResult groupResult);
}

public class GroupCombustionSolverParamsReport : ITransformable<Queue<IPdfOperation>>
{
    public GroupCombustionSolverParamsReport(GroupOptimizationResult groupResult);
    public Queue<IPdfOperation> Transform();
}
```

## Sections over the single result ✅

```csharp
public class FitnessFunctionEvaluatorReport : BaseReport, ITransformable<Queue<IPdfOperation>>
{
    public FitnessFunctionEvaluatorReport(OptimizationResult optimizationResult);
    public Queue<IPdfOperation> Transform();
}

public class ConstraintPenaltyEvaluatorReport : BaseReport, ITransformable<Queue<IPdfOperation>>
{
    public ConstraintPenaltyEvaluatorReport(OptimizationResult optimizationResult);
    public Queue<IPdfOperation> Transform();
}

public class ParametricConstraintReport : BaseReport, ITransformable<Queue<IPdfOperation>>
{
    public ParametricConstraintReport(OptimizationResult optimizationResult);
    public Queue<IPdfOperation> Transform();
}

public class PropellantReport : BaseReport, ITransformable<Queue<IPdfOperation>>
{
    public PropellantReport(OptimizationResult optimizationResult);
    public Queue<IPdfOperation> Transform();
}

public class ProblemContextReport : BaseReport, ITransformable<Queue<IPdfOperation>>
{
    public ProblemContextReport(Span<int> pressurePointIndexes, OptimizationResult optimizationResult);
    public Queue<IPdfOperation> Transform();
}

public class PressureTablesReport : BaseReport, ITransformable<ReadOnlyCollection<PressureTable>>
{
    public PressureTablesReport(Span<int> pressurePointIndexes, OptimizationResult optimizationResult);
    public ReadOnlyCollection<PressureTable> Transform();
}
```

`PressureTablesReport` — единственный раздел, чей выход не очередь операций:
он отдаёт таблицы, которые сборщик отчёта печатает своим табличным
оператором.

## Sections over their own input ✅

```csharp
public class ReportHeaderReport : ITransformable<Queue<IPdfOperation>>
{
    public ReportHeaderReport(GroupReportContextDto context);
    public Queue<IPdfOperation> Transform();
}

public class RunConfigurationReport : ITransformable<Queue<IPdfOperation>>
{
    public RunConfigurationReport(IReadOnlyList<RunConfigurationSection> sections);
    public Queue<IPdfOperation> Transform();
}

public class DifferentialEvolutionSettingsReport : ITransformable<Queue<IPdfOperation>>
{
    public DifferentialEvolutionSettingsReport(DifferentialEvolutionSettings? settings);
    public Queue<IPdfOperation> Transform();
}

public class ReproductionVectorReport : ITransformable<Queue<IPdfOperation>>
{
    public ReproductionVectorReport(GroupReportContextDto context);
    public Queue<IPdfOperation> Transform();
}

public class GroupPerformanceMeterReport : ITransformable<Queue<IPdfOperation>>
{
    public GroupPerformanceMeterReport(GroupReportContextDto reportContext);
    public Queue<IPdfOperation> Transform();
}
```

## Not part of the contract

`BurnRateConvergence` — `internal static`. Единственное место, решающее, можно
ли печатать смешанную скорость горения: при `BurnRateIsFound == false`
печатается литерал `NOT CONVERGED`. Разделы обязаны идти через него, но
наружу узла он не виден.
