using PastyPropellant.Core.Models.Thermodynamic;
using PastyPropellant.Core.Utils;

namespace PastyPropellant.ConsoleApp.Interfaces;

public interface IPossibleCombustionProductsFinder
{
    public OperationResult<IPossibleCombustionProductsHandler> FindPossibleCombustionSubstances(
        ReadOnlyMemory<ThermodynamicSubstance> substances,
        string propellantFormula
    );
}
