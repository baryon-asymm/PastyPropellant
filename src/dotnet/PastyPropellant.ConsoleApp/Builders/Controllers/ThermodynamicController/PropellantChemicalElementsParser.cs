using System.Text.RegularExpressions;
using PastyPropellant.ConsoleApp.Builders.Controllers.ThermodynamicController;
using PastyPropellant.ConsoleApp.Interfaces;
using PastyPropellant.Core.Models.Thermodynamic;
using PastyPropellant.Core.Utils;

namespace PastyPropellant.ConsoleApp.Builders.ThermodynamicController;

public partial class PropellantChemicalElementsParser : IPossibleCombustionProductsFinder
{
    private const string ChemicalElementsPattern = @"([A-Z][a-z]*)(\d*\,?\d*)";
    private readonly List<string> _chemicalElements = new();
    private readonly List<double> _chemicalElementsMolars = new();

    public OperationResult<IPossibleCombustionProductsHandler> FindPossibleCombustionSubstances(
        ReadOnlyMemory<ThermodynamicSubstance> substances,
        string propellantFormula
    )
    {
        try
        {
            propellantFormula = propellantFormula.Replace(".", ",")
                                                 .Replace(" ", "");
            TryParsePropellantChemicalElements(propellantFormula);

            return TryGetPossibleCombustionProductsHandler(substances);
        }
        catch (Exception ex)
        {
            return new OperationResult<IPossibleCombustionProductsHandler>(ex);
        }
    }

    private void TryParsePropellantChemicalElements(string propellantFormula)
    {
        var matches = ChemicalElementsRegex().Matches(propellantFormula);
        foreach (Match match in matches)
        {
            var element = match.Groups[1].Value;
            var molars = match.Groups[2].Value;
            _chemicalElements.Add(element);
            _chemicalElementsMolars.Add(string.IsNullOrEmpty(molars) ? 1 : double.Parse(molars));
        }
    }

    private OperationResult<IPossibleCombustionProductsHandler> TryGetPossibleCombustionProductsHandler(
        ReadOnlyMemory<ThermodynamicSubstance> substances
    )
    {
        var possibleCombustionProducts = new List<ThermodynamicSubstance>();
        var combustionProductsElementsMolars = new List<double[]>();

        foreach (var substance in substances.Span)
        {
            var isPossibleCombustionProduct = true;
            var matches = ChemicalElementsRegex().Matches(substance.Formula);
            var molars = new double[_chemicalElementsMolars.Count];
            for (var i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                var element = match.Groups[1].Value;
                var molarsStr = match.Groups[2].Value;
                int chemicalElementMolarsPosition;
                if ((chemicalElementMolarsPosition = _chemicalElements.IndexOf(element)) == -1)
                {
                    isPossibleCombustionProduct = false;
                    break;
                }

                molars[chemicalElementMolarsPosition] = string.IsNullOrEmpty(molarsStr) ? 1 : double.Parse(molarsStr);
            }

            if (isPossibleCombustionProduct)
            {
                possibleCombustionProducts.Add(substance);
                combustionProductsElementsMolars.Add(molars);
            }
        }

        return new OperationResult<IPossibleCombustionProductsHandler>(
                                                                       new PossibleCombustionProductsHandler(
                                                                                                             _chemicalElements
                                                                                                                 .ToArray(),
                                                                                                             _chemicalElementsMolars
                                                                                                                 .ToArray(),
                                                                                                             possibleCombustionProducts
                                                                                                                 .ToArray(),
                                                                                                             combustionProductsElementsMolars
                                                                                                                 .ToArray()
                                                                                                            )
                                                                      );
    }

    [GeneratedRegex(ChemicalElementsPattern)]
    private static partial Regex ChemicalElementsRegex();
}
