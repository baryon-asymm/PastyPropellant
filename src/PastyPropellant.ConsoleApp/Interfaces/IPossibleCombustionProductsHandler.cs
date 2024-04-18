using PastyPropellant.Core.Models.Thermodynamic;

namespace PastyPropellant.ConsoleApp.Interfaces;

public interface IProductsChemicalElementsParser
{
    public IEnumerable<string> GetChemicalElements();
    
    public IEnumerable<ThermodynamicSubstance> GetPossibleCombustionSubstances();
    
    public IEnumerable<double[]> GetCombustionProductsElementsMolars();
}
