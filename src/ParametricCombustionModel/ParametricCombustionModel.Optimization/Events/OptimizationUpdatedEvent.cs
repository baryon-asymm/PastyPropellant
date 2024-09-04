namespace ParametricCombustionModel.Optimization.Events;

public enum State : byte
{
    init,
    iter,
    done
}

public readonly struct OptimizationUpdatedEvent
{
    public ReadOnlyMemory<double> Args { get; init; }
    public State State { get; init; }
}
