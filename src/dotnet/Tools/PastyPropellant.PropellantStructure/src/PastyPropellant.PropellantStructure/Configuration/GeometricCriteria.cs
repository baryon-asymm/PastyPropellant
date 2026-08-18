namespace PastyPropellant.PropellantStructure.Configuration;

/// <summary>
/// <c>AK1..AK4</c> of the original — the four ratios that decide what counts as a
/// pocket at all.
/// </summary>
/// <remarks>
/// These are the constants the archived <c>.m</c> reports never printed, which is
/// why a report is not an input record of its own run. They are not lost, though:
/// line 126 reads them from the <c>.dat</c>, and the <c>.dat</c> files ship next to
/// the reference. So the historical values below come from the primary source, not
/// from a reconstruction.
/// </remarks>
/// <param name="Ak1">
/// Lower end of the size-ratio window a neighbour must fall in to be considered.
/// </param>
/// <param name="Ak2">Upper end of the same window.</param>
/// <param name="Ak3">
/// Threshold separating a pocket from an inter-pocket bridge.
/// </param>
/// <param name="Ak4">
/// Radius, in base-particle diameters, beyond which the whole realisation is
/// discarded.
/// </param>
public readonly record struct GeometricCriteria(double Ak1, double Ak2, double Ak3, double Ak4)
{
    /// <summary>The values every checked <c>.dat</c> carries.</summary>
    public static GeometricCriteria Historical => new(0.5, 2.0, 0.27, 4.7);
}
