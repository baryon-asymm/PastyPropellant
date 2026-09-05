# API.md — Settings

Пространство имён `ParametricCombustionModel.Optimization.Settings`.

## Strategy ✅

```csharp
public enum DifferentialEvolutionStrategy { Classic, Jde, Jade, Shade, LShade }
```

## Search settings ✅

```csharp
public record DifferentialEvolutionSettings
{
    public ReadOnlyCollection<double> LowerBound { get; init; }
    public ReadOnlyCollection<double> UpperBound { get; init; }
    public int PopulationSize { get; init; }
    public ITerminationStrategy TerminationStrategy { get; init; }
    public IPopulationUpdatedHandler? PopulationUpdatedHandler { get; init; }
    public DifferentialEvolutionStrategy Strategy { get; init; }
    public double MutationForce { get; init; }
    public double CrossoverProbability { get; init; }
    public double PBestRate { get; init; }
    public double ArchiveSizeRate { get; init; }
    public int MemorySize { get; init; }
    public double JadeAdaptationRate { get; init; }
    public long? MaxEvaluationNumber { get; init; }
    public int ProcessorsCount { get; init; }
    public int? Seed { get; init; }
    public NelderMeadRefinementSettings NelderMead { get; init; }

    public static Builder CreateBuilder();

    public sealed class Builder
    {
        public Builder WithPopulationSize(int populationSize);
        public Builder WithLowerBound(IEnumerable<double> lowerBound);
        public Builder WithUpperBound(IEnumerable<double> upperBound);
        public Builder WithTerminationStrategy(ITerminationStrategy terminationStrategy);
        public Builder WithPopulationUpdatedHandler(IPopulationUpdatedHandler? populationUpdatedHandler);
        public Builder WithStrategy(DifferentialEvolutionStrategy strategy);
        public Builder WithMutationForce(double mutationForce);
        public Builder WithCrossoverProbability(double crossoverProbability);
        public Builder WithShadeParameters(double pBestRate, double archiveSizeRate, int memorySize);
        public Builder WithMaxEvaluationNumber(long maxEvaluationNumber);
        public Builder WithProcessorsCount(int processorsCount);
        public Builder WithSeed(int? seed);
        public Builder WithNelderMeadRefinement(NelderMeadRefinementSettings nelderMead);
        public DifferentialEvolutionSettings Build();
    }
}
```

## Nelder-Mead refinement ✅

```csharp
public sealed record NelderMeadRefinementSettings
{
    public bool Enabled { get; init; }
    public bool MemeticInLoop { get; init; }          // Validate() ОТВЕРГАЕТ: адаптер против ДЭ 4.0.0
    public int EveryNGenerations { get; init; }
    public long MemeticMaxEvaluationsPerCall { get; init; }
    public bool FinalPolish { get; init; }
    public long FinalPolishMaxEvaluations { get; init; }
    public bool AdaptiveCoefficients { get; init; }
    public double DomainTolerance { get; init; }
    public double FunctionTolerance { get; init; }
    public int Restarts { get; init; }

    public static NelderMeadRefinementSettings Disabled { get; }
    public void Validate();
}
```
