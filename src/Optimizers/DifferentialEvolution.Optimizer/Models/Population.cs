namespace DifferentialEvolution.Optimizer.Models;

public readonly struct Population
{
    public Memory<Individual> Individuals { get; }

    public Population(Span<Individual> individuals)
    {
        Individuals = individuals.ToArray().AsMemory();
    }
}
