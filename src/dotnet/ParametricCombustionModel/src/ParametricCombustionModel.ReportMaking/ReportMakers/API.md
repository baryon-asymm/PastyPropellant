# API.md — ReportMakers

Пространство имён `ParametricCombustionModel.ReportMaking.ReportMakers`.

## Group PDF report maker ✅

```csharp
public class GroupPdfReportMaker : IReportMaker, IPdfOperationVisitor
{
    public GroupPdfReportMaker(GroupReportContextDto reportContext, IPdfGeneratorAdapter pdfGeneratorAdapter);
    public string MakeReport();
    public void Visit(AddTabOperation operation);
    public void Visit(LineBreakOperation operation);
    public void Visit(PrintTextOperation operation);
}
```

`MakeReport()` собирает и генерирует отчёт. Возвращаемое значение сейчас —
пустая строка, полагаться на него нельзя; путь к файлу задаётся адаптером
снаружи.
