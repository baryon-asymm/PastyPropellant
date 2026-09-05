# API.md — Interfaces

Пространство имён `ParametricCombustionModel.ReportMaking.Interfaces`.

## Operation contract ✅

```csharp
public interface IPdfOperation
{
    void Accept(IPdfOperationVisitor visitor);
}

public interface IPdfOperationVisitor
{
    void Visit(AddTabOperation operation);
    void Visit(LineBreakOperation operation);
    void Visit(PrintTextOperation operation);
}
```

## Report contracts ✅

```csharp
// Единственная реализация возвращает пустую строку; документ пишется по пути генератора.
public interface IReportMaker
{
    string MakeReport();
}

public interface ITransformable<out T>
{
    T Transform();
}
```
