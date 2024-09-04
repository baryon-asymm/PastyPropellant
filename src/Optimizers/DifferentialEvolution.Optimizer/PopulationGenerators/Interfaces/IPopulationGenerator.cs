using DifferentialEvolution.Optimizer.Interfaces;
using DifferentialEvolution.Optimizer.Models;

namespace DifferentialEvolution.Optimizer.PopulationGenerators.Interfaces;

public interface IPopulationGenerator
{
    public Population Generate(IFitnessFunctionInvoker fitnessFunctionInvoker);
}
