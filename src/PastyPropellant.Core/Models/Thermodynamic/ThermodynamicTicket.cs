using System.Text.Json.Serialization;

namespace PastyPropellant.Core.Models.Thermodynamic;

public record ThermodynamicTicket(
    [property: JsonPropertyName("name")]
    [property: JsonRequired]
    string Name,
    [property: JsonPropertyName("formula")]
    [property: JsonRequired]
    string Formula,
    [property: JsonPropertyName("output_file")]
    string OutputFilePath = "thermodynamic_output.json",
    [property: JsonPropertyName("substances_file")]
    string CoefficientsFilePath = "thermodynamic_substances.json",
    [property: JsonPropertyName("global_params_file")]
    string GlobalParamsFilePath = "global_params.json",
    [property: JsonPropertyName("chemical_elements_file")]
    string ChemicalElementsFilePath = "chemical_elements.json"
);
