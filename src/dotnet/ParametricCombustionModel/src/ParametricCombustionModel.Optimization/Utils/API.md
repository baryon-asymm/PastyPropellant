# API.md — Utils

Пространство имён `ParametricCombustionModel.Optimization.Utils`.

## Vector text format ✅

```csharp
public static class VectorFormat
{
    public static string FormatGene(double gene);                    // "R", инвариантная культура
    public static IEnumerable<string> FormatLines(double[] genes);   // по гену на строку
    public static string Canonical(double[] genes);                  // склейка через '\n'
    public static string Checksum(double[] genes);                   // SHA-256, нижний регистр
}
```
