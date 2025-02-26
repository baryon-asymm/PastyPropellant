using PastyPropellant.Core.Models.Thermodynamic;

namespace PastyPropellant.ConsoleApp.Interfaces;

public interface IPossibleCombustionProductsHandler
{
    public ReadOnlySpan<string> GetChemicalElements();

    public ReadOnlySpan<double> GetPropellantChemicalElementsMolars();

    public ReadOnlySpan<ThermodynamicSubstance> GetPossibleCombustionSubstances();

    public ReadOnlySpan<double[]> GetCombustionProductsElementsMolars();
}
