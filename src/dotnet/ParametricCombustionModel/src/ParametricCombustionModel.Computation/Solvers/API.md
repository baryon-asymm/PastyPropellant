# API.md — Solvers

Пространство имён `ParametricCombustionModel.Computation.Solvers`.
Все решатели реализуют `ISolverVisitor` и пишут результат в переданный
контекст; возвращаемого значения у решения нет.

## Solver hierarchy ✅

```csharp
public abstract class BasePropellantSolver : ISolverVisitor
{
    public abstract void Visit(in CombustionSolverParamsByUnits solverParamsByUnits, ProblemContextByUnits context);
    public abstract void Visit(in CombustionSolverParamsByDoubles solverParams, ProblemContextByDoubles context);

    protected abstract HeatFlux GetSurfaceHeatFluxesError(/* ярус ByUnits */);
    protected abstract double GetSurfaceHeatFluxesError(/* ярус ByDoubles */);
    protected virtual bool TryGetSurfaceTemperature(/* деление отрезка, допуск 1e-8 K */);
    protected virtual Speed GetBurnRate(/* ярус ByUnits */);
    protected virtual MassFlux GetDecomposeRate(/* ярус ByUnits */);
}

public abstract class BaseKineticPropellantSolver : BasePropellantSolver
{
    protected abstract void ExtractKineticBurnParams(/* оба яруса */);
    protected virtual HeatFlux GetKineticFlameHeatFlux(/* ярус ByUnits */);
    protected virtual double GetKineticFlameHeatFlux(/* ярус ByDoubles */);
}
```

## Inter-pocket solver ✅

```csharp
public sealed class InterPocketPropellantSolver : BaseKineticPropellantSolver
{
    public override void Visit(in CombustionSolverParamsByUnits solverParamsByUnits, ProblemContextByUnits context);
    public override void Visit(in CombustionSolverParamsByDoubles solverParams, ProblemContextByDoubles context);
}
```

## Pocket solver ✅

```csharp
public sealed class PocketPropellantSolver : BasePropellantSolver
{
    public PocketPropellantSolver();
    public override void Visit(in CombustionSolverParamsByUnits solverParamsByUnits, ProblemContextByUnits context);
    public override void Visit(in CombustionSolverParamsByDoubles solverParams, ProblemContextByDoubles context);
}

// Безсостоянийные выборщики того, какое из двух пламён кармана считается.
public sealed class KineticSkeletonHelper
{
    public HeatFlux GetKineticFlameHeatFlux(/* ярус ByUnits */);
    public double GetKineticFlameHeatFlux(/* ярус ByDoubles */);
}

public sealed class KineticOutSkeletonHelper
{
    public HeatFlux GetKineticFlameHeatFlux(/* ярус ByUnits */);
    public double GetKineticFlameHeatFlux(/* ярус ByDoubles */);
}
```

## Mixing solver ✅

```csharp
// r = 1 / (phi_ip / r_ip + phi_p / r_p); сходится только если сошлись оба вклада.
public sealed class MixedPropellantSolver : ISolverVisitor
{
    public MixedPropellantSolver();
    public void Visit(in CombustionSolverParamsByUnits solverParamsByUnits, ProblemContextByUnits context);
    public void Visit(in CombustionSolverParamsByDoubles solverParams, ProblemContextByDoubles context);
}
```

## Kinetic flame calculator ✅

```csharp
public static class KineticFlameCalculator
{
    public static HeatFlux GetKineticFlameHeatFlux(/* ярус ByUnits */);
    public static Temperature GetAverageKineticFlameTemperature(/* ярус ByUnits */);
    public static Density GetAverageKineticFlameDensity(/* ярус ByUnits */);
    public static Length GetKineticFlameHeight(/* ярус ByUnits */);

    public static double GetKineticFlameHeatFlux(/* ярус ByDoubles */);
    public static double GetAverageKineticFlameTemperature(/* ярус ByDoubles */);
    public static double GetAverageKineticFlameDensity(/* ярус ByDoubles */);
    public static double GetKineticFlameHeight(/* ярус ByDoubles */);
}
```
