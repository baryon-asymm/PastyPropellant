# API.md — PdfOperations

Пространство имён `ParametricCombustionModel.ReportMaking.PdfOperations`.

## Operations ✅

```csharp
public class PrintTextOperation : IPdfOperation
{
    public TextStyle Style { get; init; }
    public string Text { get; init; }
    public void Accept(IPdfOperationVisitor visitor);
}

public class LineBreakOperation : IPdfOperation
{
    public void Accept(IPdfOperationVisitor visitor);
}

public class AddTabOperation : IPdfOperation
{
    public void Accept(IPdfOperationVisitor visitor);
}
```
