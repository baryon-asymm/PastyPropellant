namespace PastyPropellant.PropellantStructure.Configuration;

/// <summary>
/// The dimensionless coefficients the original sets at lines 66-79 and lets the
/// interactive menu override.
/// </summary>
/// <remarks>
/// Every one of them holds the same value in all 43 archived runs, so the reference
/// pins the value but says nothing about the behaviour on either side of it. A run
/// that moves any of these is recorded as unverified — see
/// <see cref="ResolvedRunRecordSnapshot.UnverifiedSettings"/>.
/// </remarks>
/// <param name="SurroundingVolumeShare">
/// <c>alpha</c>, line 69: the share of a base particle's own volume the neighbour
/// shell is allowed to reach before the loop stops adding neighbours.
/// </param>
/// <param name="PocketInPocket"><c>karmcoef</c>, line 79.</param>
/// <param name="PocketInBridge"><c>mkmcoef</c>, line 78.</param>
/// <param name="Accuracy"><c>eps_dok</c>, line 72.</param>
/// <param name="StatisticalSignificance">
/// <c>alfa</c>, line 73. Zero in every archived run, which is what keeps the
/// truncation branch of <c>PARAM</c> — and the mutation of this very value on line
/// 1750 — out of reach.
/// </param>
/// <param name="HomogenisedOxidiser">
/// <c>gdokns</c>, line 74: oxidiser mass declared fine enough to count as part of
/// the binder rather than as particles.
/// </param>
/// <param name="PocketBridgeRatioMin"><c>nn_min</c>, line 70.</param>
/// <param name="PocketBridgeRatioMax"><c>nn_max</c>, line 71.</param>
public readonly record struct ModelCoefficients(
    double SurroundingVolumeShare,
    double PocketInPocket,
    double PocketInBridge,
    double Accuracy,
    double StatisticalSignificance,
    double HomogenisedOxidiser,
    double PocketBridgeRatioMin,
    double PocketBridgeRatioMax)
{
    /// <summary>
    /// The literals of lines 69-79, which are also the values of all 43 archived
    /// runs.
    /// </summary>
    public static ModelCoefficients Historical { get; } = new(
        SurroundingVolumeShare: 0.25,   // alpha
        PocketInPocket: 8.2,            // karmcoef
        PocketInBridge: 7.73,           // mkmcoef
        Accuracy: 5e-2,                 // eps_dok; the report prints 0.050000001
        StatisticalSignificance: 0.0,   // alfa
        HomogenisedOxidiser: 0.0,       // gdokns
        PocketBridgeRatioMin: 3.0,      // nn_min
        PocketBridgeRatioMax: 1e2);     // nn_max
}
