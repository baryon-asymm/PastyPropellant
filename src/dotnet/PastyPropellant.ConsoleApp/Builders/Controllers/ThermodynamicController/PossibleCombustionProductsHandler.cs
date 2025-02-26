using PastyPropellant.ConsoleApp.Interfaces;
using PastyPropellant.Core.Models.Thermodynamic;

namespace PastyPropellant.ConsoleApp.Builders.Controllers.ThermodynamicController;

public class PossibleCombustionProductsHandler(
    ReadOnlyMemory<string> chemicalElements,
    ReadOnlyMemory<double> propellantChemicalElementsMolars,
    ReadOnlyMemory<ThermodynamicSubstance> possibleCombustionSubstances,
    ReadOnlyMemory<double[]> combustionProductsElementsMolars
) : IPossibleCombustionProductsHandler
{
    public ReadOnlySpan<string> GetChemicalElements()
    {
        return chemicalElements.Span;
    }

    public ReadOnlySpan<double> GetPropellantChemicalElementsMolars()
    {
        return propellantChemicalElementsMolars.Span;
    }

    public ReadOnlySpan<ThermodynamicSubstance> GetPossibleCombustionSubstances()
    {
        return possibleCombustionSubstances.Span;
    }

    public ReadOnlySpan<double[]> GetCombustionProductsElementsMolars()
    {
        return combustionProductsElementsMolars.Span;
    }
}
