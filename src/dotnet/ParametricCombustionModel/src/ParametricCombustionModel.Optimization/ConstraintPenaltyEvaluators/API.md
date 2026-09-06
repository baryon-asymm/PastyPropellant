# API.md — ConstraintPenaltyEvaluators

Пространство имён
`ParametricCombustionModel.Optimization.ConstraintPenaltyEvaluators`.
Все оценщики наследуют базовый и реализуют оба яруса `GetPenaltyValue`.

## Base ✅

```csharp
public abstract class BaseConstraintPenaltyEvaluator : IPenaltyEvaluator
{
    public const double ZeroPenaltyValue = 0.0;
    public double PenaltyRate { get; init; }

    protected BaseConstraintPenaltyEvaluator(double penaltyRate);   // бросает при penaltyRate <= 0

    public abstract double GetPenaltyValue(ProblemContextByUnits updatedProblemContext);
    public abstract double GetPenaltyValue(ProblemContextByDoubles updatedProblemContext);
}
```

## Evaluators ✅

```csharp
// max/min из четырёх потоков кармана не должно превышать порог (> 1).
// ⚠ Единственный оценщик, возвращающий PenaltyRate * ratio без деления на порог:
// на границе штраф равен PenaltyRate * HeatFluxRatioThreshold, а не PenaltyRate.
// См. инвариант-исключение в BOOT.md.
public sealed class PocketHeatFluxRatioCompetitionPenaltyEvaluator : BaseConstraintPenaltyEvaluator
{
    public double HeatFluxRatioThreshold { get; init; }
    public PocketHeatFluxRatioCompetitionPenaltyEvaluator(double penaltyRate, double heatFluxRatioThreshold);
}

// карман не должен гореть быстрее межкарманного вещества.
public sealed class InterPocketFasterBurnPenaltyEvaluator : BaseConstraintPenaltyEvaluator
{
    public InterPocketFasterBurnPenaltyEvaluator(double penaltyRate);
}

// потолки на три кинетических потока.
public class KineticFlameHeatFluxPenaltyEvaluator : BaseConstraintPenaltyEvaluator
{
    public double MaxInterPocketKineticFlameHeatFlux { get; init; }
    public double MaxSkeletonKineticFlameHeatFlux { get; init; }
    public double MaxOutSkeletonKineticFlameHeatFlux { get; init; }

    public KineticFlameHeatFluxPenaltyEvaluator(
        double penaltyRate,
        HeatFlux maxInterPocketKineticFlameHeatFlux,
        HeatFlux maxSkeletonKineticFlameHeatFlux,
        HeatFlux maxOutSkeletonKineticFlameHeatFlux);

    public KineticFlameHeatFluxPenaltyEvaluator(
        double penaltyRate,
        double maxInterPocketKineticFlameHeatFlux,
        double maxSkeletonKineticFlameHeatFlux,
        double maxOutSkeletonKineticFlameHeatFlux);
}

// толщина каркасного слоя не больше LargeParticleDiameterThreshold * СРЕДНЕГО
// диаметра окислителя (AverageOxidizerDiameter) — не диаметра крупной фракции,
// вопреки имени класса; множитель и есть переход от среднего к крупному.
public sealed class LargeOxidizerParticleSizePenaltyEvaluator : BaseConstraintPenaltyEvaluator
{
    public double LargeParticleDiameterThreshold { get; init; }
    public LargeOxidizerParticleSizePenaltyEvaluator(double penaltyRate, double largeParticleDiameterThreshold);
}

// толщина каркасного слоя не меньше доли диаметра поры.
public sealed class PoreDiameterPenaltyEvaluator : BaseConstraintPenaltyEvaluator
{
    public double PoreDiameterThreshold { get; init; }
    public PoreDiameterPenaltyEvaluator(double penaltyRate, double poreDiameterThreshold);
}

// радиационная теплопроводность не ниже кондуктивной.
public sealed class RadiativeThermalConductivityPenaltyEvaluator : BaseConstraintPenaltyEvaluator
{
    public RadiativeThermalConductivityPenaltyEvaluator(double penaltyRate);
}
```

## Children

- [Interfaces/API.md](./Interfaces/API.md) — контракт `IPenaltyEvaluator`.
