# API.md — Units

Пространство имён `ParametricCombustionModel.Computation.Units`.

## Metal burning constant A ✅

```csharp
public enum AMetalBurningConstantUnit { SquareMetersPerSecond }

public readonly struct AMetalBurningConstant
    : IDivisionOperators<AMetalBurningConstant, Speed, Length>
{
    public AMetalBurningConstant(double value, AMetalBurningConstantUnit unit);
    public double SquareMetersPerSecond { get; }
    public static Length operator /(AMetalBurningConstant left, Speed right);
    public static AMetalBurningConstant FromSquareMetersPerSecond(double value);
    public override string ToString();
}
```

## Metal burning constant B ✅

```csharp
public enum BMetalBurningConstantUnit { CubicMetersPerSquareSecond }

public readonly struct BMetalBurningConstant
    : IDivisionOperators<BMetalBurningConstant, Speed, AMetalBurningConstant>
{
    public BMetalBurningConstant(double value, BMetalBurningConstantUnit unit);
    public double CubicMetersPerSquareSecond { get; }
    public static AMetalBurningConstant operator /(BMetalBurningConstant left, Speed right);
    public static BMetalBurningConstant FromCubicMetersPerSquareSecond(double value);
    public override string ToString();
}
```
