using PastyPropellant.Core.Utils;
using PastyPropellant.PorosityCalculation.Models;

namespace PastyPropellant.PorosityCalculation.Interfaces;

public interface IPorosityCalculator
{
    public Task<OperationResult<PorosityPropellant>> CalculatePorosityAsync(
        string propellantsFilePath,
        string propellantName,
        string regionFilePath);
}
