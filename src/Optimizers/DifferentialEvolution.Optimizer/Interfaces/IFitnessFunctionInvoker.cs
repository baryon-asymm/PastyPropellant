namespace DifferentialEvolution.Optimizer.Interfaces;

public interface IFitnessFunctionInvoker
{
    public double Invoke(
        int threadIndex,
        Span<double> point);
}
