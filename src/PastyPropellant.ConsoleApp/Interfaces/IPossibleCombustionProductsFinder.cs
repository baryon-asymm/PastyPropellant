using PastyPropellant.Core.Models.Thermodynamic;
using PastyPropellant.Core.Utils;

namespace PastyPropellant.ConsoleApp.Interfaces;

public interface IChemicalElementsParser
{
    public OperationResult<IEnumerable<ThermodynamicSubstance>> GetPossibleCombustionSubstances(
        IEnumerable<ThermodynamicSubstance> substances,
        string propellantFormula
    );

    public OperationResult<IEnumerable<double>> GetChemicalElementsMolesSubstances(
        IEnumerable<ThermodynamicSubstance> substances,
        string propellantFormula
    );
}
