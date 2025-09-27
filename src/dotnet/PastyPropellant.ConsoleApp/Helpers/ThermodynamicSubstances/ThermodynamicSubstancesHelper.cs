using PastyPropellant.ConsoleApp.Interfaces;
using PastyPropellant.Core.Models.Thermodynamic;
using PastyPropellant.Core.Utils;

namespace PastyPropellant.ConsoleApp.Helpers.ThermodynamicSubstances;

public class ThermodynamicSubstancesHelper(
    IModelReader<List<ThermodynamicSubstance>> modelReader
)
{
    public virtual Task<OperationResult<List<ThermodynamicSubstance>>> GetAllAsync()
    {
        return modelReader.ReadAllAsync();
    }
}
