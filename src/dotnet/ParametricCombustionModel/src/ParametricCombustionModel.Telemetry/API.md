# API.md — ParametricCombustionModel.Telemetry

Пространство имён `ParametricCombustionModel.Telemetry`.

## Performance meter ✅

```csharp
public class PerformanceMeter
{
    public SingleExecutionFrameMeasurer? TotalExecutionTimeMeasurer { get; }
    public IReadOnlyList<EnhancedExecutionFrameMeasurer> ExecutionFrames { get; }
    public SingleExecutionFrameMeasurer GetTotalExecutionTimeMeasurer();
    public EnhancedExecutionFrameMeasurer CreateExecutionFrameMeasurer(string name, string description, string unit, string path);
}
```

Реестр приборов. `GetTotalExecutionTimeMeasurer()` создаёт измеритель полного
времени при первом вызове и возвращает тот же при последующих;
`TotalExecutionTimeMeasurer` до первого вызова — `null`.
`CreateExecutionFrameMeasurer` заводит измеритель кадров и помещает его в
`ExecutionFrames`. Оба метода следует звать до старта воркеров.

## How the assembly is used

Хост создаёт один `PerformanceMeter` на прогон, заводит измеритель полного
времени, а при построении матрицы контекстов подставляет
`MeasureOptimizationProblemByDoubles` из узла [Models](./Models/API.md) — по
одному на воркер, с его номером. После прогона отчёт перечисляет
`ExecutionFrames` и читает показания сборщика мусора из узла
[Instruments](./Instruments/API.md).

## Children

- [Instruments/API.md](./Instruments/API.md) — приборы и измерители;
- [Models/API.md](./Models/API.md) — измеряющий контекст задачи.
