using System.Text.Json.Serialization;

namespace ParametricCombustionModel.Core.Models.PropellantComponents;

public record Aluminum(
    double MassFraction,
    double Density,
    [property: JsonRequired]
    [property: JsonPropertyName("agglomeration_coefficients")]
    IEnumerable<double> AgglomerationCoefficients,

    /// <summary>
    /// Measured agglomerated-metal mass fraction Z_a^m with its error bars — the data the
    /// <see cref="AgglomerationCoefficients"/> polynomial approximates.
    ///
    /// <para>Same record and the same conventions as the burn-rate
    /// <see cref="Propellant.ConfidenceIntervals"/>: <c>x_value</c> is a pressure in
    /// <b>MPa</b> (not Pa, unlike the pressure axis these plots are drawn on), and
    /// <c>size_of_confidence_interval</c> is the FULL height of the bar, so a whisker
    /// reaches <c>y_value ± size/2</c>.</para>
    ///
    /// <para>Optional: a propellant without measurements simply carries no intervals, and
    /// nothing downstream may assume they are present.</para>
    /// </summary>
    [property: JsonPropertyName("agglomeration_confidence_intervals")]
    IEnumerable<ConfidenceInterval>? AgglomerationConfidenceIntervals = null
) : BaseComponent(MassFraction, Density);
