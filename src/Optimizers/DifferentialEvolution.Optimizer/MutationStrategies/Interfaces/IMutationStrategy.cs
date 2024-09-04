using DifferentialEvolution.Optimizer.Models;

namespace DifferentialEvolution.Optimizer.MutationStrategies.Interfaces;

public interface IMutationStrategy
{
    public void Mutate(ref Population population, int indexOfCurrentIndividual, ref Individual trialIndividual);
}
