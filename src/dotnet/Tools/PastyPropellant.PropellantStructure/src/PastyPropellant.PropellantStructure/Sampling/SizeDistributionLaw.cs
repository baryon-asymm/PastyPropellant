namespace PastyPropellant.PropellantStructure.Sampling;

/// <summary>
/// JZ of the original: the law particle sizes follow <em>inside</em> one oxidiser
/// fraction. It fixes both halves of the inversion at once — the formula
/// <c>SIZE</c> uses to turn a variate into a diameter, and the weight
/// <c>PARAM</c> uses to turn a mass fraction into a number fraction — because the
/// second is the first one's third moment.
/// </summary>
/// <remarks>
/// Public although the rest of <c>Sampling/</c> is internal: the law is a choice
/// the caller of the whole library makes, and <c>Configuration/</c> only carries
/// it. Defining it here keeps the dependency arrow pointing from configuration
/// into sampling, so <c>Sampling/</c> still knows nobody.
/// </remarks>
public enum SizeDistributionLaw
{
    /// <summary>
    /// JZ = 1. <c>1/D²</c> is uniform across the fraction, so the number density
    /// goes as <c>D⁻³</c>.
    /// </summary>
    Surface = 1,

    /// <summary>
    /// JZ = 2. <c>D</c> itself is uniform across the fraction.
    /// </summary>
    Volume = 2,
}
