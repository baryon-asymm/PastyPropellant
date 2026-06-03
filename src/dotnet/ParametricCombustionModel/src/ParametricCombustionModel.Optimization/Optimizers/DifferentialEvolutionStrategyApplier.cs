using DotNetDifferentialEvolution;
using ParametricCombustionModel.Optimization.Settings;

namespace ParametricCombustionModel.Optimization.Optimizers;

/// <summary>
/// Applies the configured <see cref="DifferentialEvolutionStrategy"/> to the staged
/// DotNetDifferentialEvolution builder, replacing the mutation+selection step. All branches
/// converge on <see cref="ITerminationConditionRequired"/>, so callers continue the fluent
/// chain with <c>WithTerminationCondition(...)</c> uniformly.
/// </summary>
internal static class DifferentialEvolutionStrategyApplier
{
    public static ITerminationConditionRequired ApplyStrategy(
        this IMutationStrategyRequired builder,
        DifferentialEvolutionSettings settings) =>
        settings.Strategy switch
        {
            DifferentialEvolutionStrategy.Classic =>
                builder.WithDefaultMutationStrategy(settings.MutationForce, settings.CrossoverProbability)
                       .WithDefaultSelectionStrategy(),
            DifferentialEvolutionStrategy.Jde =>
                builder.WithJde(settings.MutationForce, settings.CrossoverProbability),
            DifferentialEvolutionStrategy.Jade =>
                builder.WithJade(settings.PBestRate, settings.ArchiveSizeRate, settings.JadeAdaptationRate),
            DifferentialEvolutionStrategy.Shade =>
                builder.WithShade(settings.PBestRate, settings.ArchiveSizeRate, settings.MemorySize),
            // L-SHADE: evaluation budget drives the linear population-size reduction; the JADE/SHADE
            // control parameters (pBestRate/archiveSizeRate/memorySize) are passed explicitly in v4.
            DifferentialEvolutionStrategy.LShade =>
                builder.WithLShade(
                    settings.MaxEvaluationNumber
                        ?? throw new InvalidOperationException(
                            "MaxEvaluationNumber must be set when using the L-SHADE strategy."),
                    settings.PBestRate,
                    settings.ArchiveSizeRate,
                    settings.MemorySize),
            _ => throw new ArgumentOutOfRangeException(
                nameof(settings), settings.Strategy, "Unknown differential evolution strategy.")
        };
}
