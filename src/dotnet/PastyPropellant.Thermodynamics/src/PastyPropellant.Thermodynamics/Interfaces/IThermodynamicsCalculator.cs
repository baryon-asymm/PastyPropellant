using PastyPropellant.Core.Utils;
using PastyPropellant.Thermodynamics.Models;
using UnitsNet;

namespace PastyPropellant.Thermodynamics.Interfaces;

public interface IThermodynamicsCalculator
{
    public Task<OperationResult<PropellantThermodynamics>> CalculateThermodynamicPropertiesAsync(
        string propellantFilePath,
        string combustionProductsFilePath,
        Pressure pressure
    );
}
