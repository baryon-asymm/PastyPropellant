namespace DifferentialEvolution.Optimizer.Models;

public struct Individual
{
    public double FitnessFunctionCost { get; set; }
    public Memory<double> Vector { get; }

    public Individual(double fitnessFunctionCost, Span<double> vector)
    {
        FitnessFunctionCost = fitnessFunctionCost;
        Vector = vector.ToArray().AsMemory();
    }
}
