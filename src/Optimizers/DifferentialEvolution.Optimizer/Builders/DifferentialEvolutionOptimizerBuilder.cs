namespace DifferentialEvolution.Optimizer.Builders;

/*public interface ILowerBoundRequired
{
    public IUpperBoundRequired WithLowerBound(IEnumerable<double> lowerBound);
}

public interface IUpperBoundRequired
{
    public IPopulationSizeRequired WithUpperBound(IEnumerable<double> upperBound);
}

public interface IPopulationSizeRequired
{
    public  WithPopulationSize(int populationSize);
}

public interface IMutationStrategyRequired
{
    public IRecombinationStrategyRequired WithMutationStrategy(IMutationStrategy mutationStrategy);
}

public interface IBuilder<out T> where T : class
{
    public T Build();
}

public class DifferentialEvolutionOptimizerBuilder : ILowerBoundRequired
{
    private readonly IFitnessFunctionInvoker _invoker;

    private DifferentialEvolutionOptimizerBuilder(IFitnessFunctionInvoker invoker)
    {
        _invoker = invoker;
    }

    public static ILowerBoundRequired ForFitnessFunctionInvoker(IFitnessFunctionInvoker invoker)
    {
        return new DifferentialEvolutionOptimizerBuilder(invoker);
    }

    public IUpperBoundRequired WithLowerBound(IEnumerable<double> lowerBound)
    {
        return this;
    }
}*/
