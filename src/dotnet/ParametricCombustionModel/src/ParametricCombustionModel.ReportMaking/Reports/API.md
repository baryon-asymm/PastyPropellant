# API.md — Reports

Пространство имён `ParametricCombustionModel.ReportMaking.Reports`.

## Report base ✅

```csharp
public abstract class BaseReport
{
    protected readonly OptimizationResult Result;
    public BaseReport(OptimizationResult optimizationResult);
}
```

## Children

- [Pdf/API.md](./Pdf/API.md) — все разделы PDF-отчёта.
