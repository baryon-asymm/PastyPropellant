# API.md — Instruments

Пространство имён `ParametricCombustionModel.Telemetry.Instruments`.

## Instrument description ✅

```csharp
public abstract class Instrument
{
    public Instrument(string name, string description, string unit, string path);
    public string Name { get; init; }
    public string Description { get; init; }
    public string Unit { get; init; }
    public string Path { get; init; }
}
```

## Execution frame measurers ✅

```csharp
public abstract class ExecutionFrameMeasurer : Instrument, IDisposable
{
    public ExecutionFrameMeasurer(string name, string description, string unit, string path);
    public abstract ExecutionFrameMeasurer StartFrame();
    public abstract void EndFrame();
    public abstract void Dispose();
}

public class SingleExecutionFrameMeasurer : ExecutionFrameMeasurer
{
    public SingleExecutionFrameMeasurer(string name, string description, string unit, string path);
    public DateTime StartTime { get; }
    public DateTime EndTime { get; }
    public double ExecutionTime { get; }
}

public class EnhancedExecutionFrameMeasurer : ExecutionFrameMeasurer
{
    public EnhancedExecutionFrameMeasurer(string name, string description, string unit, string path);
    public uint CallsCount { get; }
    public double MinExecutionTime { get; }
    public double MaxExecutionTime { get; }
    public double MeanExecutionTime { get; }
    public double StdDevExecutionTime { get; }
}
```

Кадр открывается через `using (measurer.StartFrame())`; закрытие делает
`Dispose()`. Все времена — миллисекунды. Накапливающий измеритель
предназначен одному потоку.

## Garbage collector readings ✅

```csharp
public static class GCMeasurer
{
    public static long TotalMemory { get; }
    public static double PauseTimePercentage { get; }
    public static int GetCollectionCount(int generation);
}
```
