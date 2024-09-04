namespace DifferentialEvolution.Optimizer.Interfaces;

public interface IFitnessFunctionInvoker
{
    public double Invoke(Span<double> point);
}
