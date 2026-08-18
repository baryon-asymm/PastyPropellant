namespace PastyPropellant.PropellantStructure.Results;

/// <summary>
/// How the run went, as opposed to what it computed.
/// </summary>
/// <remarks>
/// Two groups, kept apart on purpose. The first the original printed, and L2 compares
/// it first and <b>exactly</b> — a whole-number counter has no print tolerance to
/// hide behind. The second the original never printed; two of those come out of
/// <c>Geometry/</c>, whose contract explicitly does not guarantee bit-exactness
/// because <c>ACOS</c>/<c>SIN</c>/<c>TAN</c> resolve there to Compaq's single
/// precision intrinsics while the port calls the double-precision <c>Math.*</c>.
/// Lying in one undifferentiated record, they would be compared under one rule.
/// </remarks>
public sealed record RunDiagnostics
{
    /// <summary>Pockets defined over the whole run — the original's <c>Nkarm</c>.</summary>
    public required long PocketsTotal { get; init; }

    /// <summary>Draws from the base-particle stream — <c>NFX</c>.</summary>
    public required long BaseParticleDraws { get; init; }

    /// <summary>Draws from the surrounding-particle stream — <c>NFY</c>.</summary>
    public required long SurroundingParticleDraws { get; init; }

    /// <summary>Draws from the pocket-size stream — <c>NFQ</c>.</summary>
    public required long PocketSizeDraws { get; init; }

    /// <summary>Draws from the bridge stream — <c>NFW</c>.</summary>
    public required long BridgeDraws { get; init; }

    /// <summary>Passes actually made — the original's <c>KPRIS</c>.</summary>
    public required int PassesDone { get; init; }

    /// <summary>
    /// Base particles asked for in a pass — the <c>N</c> of the report's
    /// <c>Nbase = N + FI</c>.
    /// </summary>
    public required long BaseParticlesPerCycle { get; init; }

    /// <summary>
    /// Base particles accepted, accumulated — the <c>FI</c> of the same line, and the
    /// divisor of the second group of <see cref="ConditionCounters"/>.
    /// </summary>
    /// <remarks>
    /// ⚠ Equal to <see cref="BaseParticlesPerCycle"/> in all 43 archived runs, which
    /// is a finding rather than a licence to merge the two fields: the equality holds
    /// precisely because no archived run makes more than one pass.
    /// </remarks>
    public required long BaseParticlesAccepted { get; init; }

    /// <summary>Mean error of each random stream — the report's <c>epsx(1..6)</c>.</summary>
    public required IReadOnlyList<double> RandomStreamStatistics { get; init; }

    /// <summary>
    /// Accuracy of the fraction draw, one entry per oxidiser fraction — the array
    /// <c>epsdokfr</c>.
    /// </summary>
    /// <remarks>
    /// Counted among the original's fourteen arrays, but a diagnostic rather than a
    /// distribution: it is indexed by fraction number and carries no normalisation.
    /// </remarks>
    public required IReadOnlyList<double> FractionDrawAccuracy { get; init; }

    /// <summary>
    /// One row per working pass — the six <c>*_n</c> arrays, printed only when the run
    /// makes more than one pass. Empty otherwise.
    /// </summary>
    /// <remarks>
    /// The original prints these as six parallel arrays; they are one table read by
    /// pass, and holding them apart invites a comparison of pass <i>i</i> of one
    /// against pass <i>j</i> of another. Empty rather than absent because a single-pass
    /// run has nothing to converge: the quantities exist, the history does not.
    /// </remarks>
    public required IReadOnlyList<PassConvergence> Convergence { get; init; }

    /// <summary>How often each rejection condition fired.</summary>
    public required ConditionCounters Conditions { get; init; }

    /// <summary>
    /// Base-particle realisations begun over the whole run, accepted or not — one per
    /// arrival at label 11. Never printed.
    /// </summary>
    /// <remarks>
    /// The useful reading is the ratio to the particles actually accepted,
    /// <c>(1 + Cycles) × BaseParticlesPerCycle</c>: how many realisations the recipe
    /// throws away for each one it keeps, which is the cost of the nine conditions and
    /// varies by an order of magnitude between compositions. The count itself has no
    /// oracle — the original prints nothing like it.
    /// </remarks>
    public required Verified<long> AttemptsTotal { get; init; }

    /// <summary>Pocket sizes clamped to the histogram's last cell. Never printed.</summary>
    public required Verified<long> PocketSizeClampCount { get; init; }

    /// <summary>
    /// Bridge rejections from <c>A &gt;= 2*RK</c> in <c>VM</c>. Never printed.
    /// </summary>
    public required Verified<long> BridgeGapExceedsPocket { get; init; }

    /// <summary>Bridge rejections from <c>BB &lt; 0</c> in <c>VM</c>. Never printed.</summary>
    public required Verified<long> BridgeWidthNegative { get; init; }

    /// <summary>Bridge volumes that came out NaN. Never printed; zero in the reference.</summary>
    public required Verified<long> BridgeVolumeNaNCount { get; init; }

    /// <summary>
    /// Passes as the configuration asked for them, before the kernel normalises a
    /// value of 1 or less to a single pass.
    /// </summary>
    public required int CyclesRequested { get; init; }

    /// <inheritdoc />
    public bool Equals(RunDiagnostics? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && PocketsTotal == other.PocketsTotal
            && BaseParticleDraws == other.BaseParticleDraws
            && SurroundingParticleDraws == other.SurroundingParticleDraws
            && PocketSizeDraws == other.PocketSizeDraws
            && BridgeDraws == other.BridgeDraws
            && PassesDone == other.PassesDone
            && BaseParticlesPerCycle == other.BaseParticlesPerCycle
            && BaseParticlesAccepted == other.BaseParticlesAccepted
            && Structural.ListEquals(RandomStreamStatistics, other.RandomStreamStatistics)
            && Structural.ListEquals(FractionDrawAccuracy, other.FractionDrawAccuracy)
            && Structural.ListEquals(Convergence, other.Convergence)
            && Conditions == other.Conditions
            && AttemptsTotal == other.AttemptsTotal
            && PocketSizeClampCount == other.PocketSizeClampCount
            && BridgeGapExceedsPocket == other.BridgeGapExceedsPocket
            && BridgeWidthNegative == other.BridgeWidthNegative
            && BridgeVolumeNaNCount == other.BridgeVolumeNaNCount
            && CyclesRequested == other.CyclesRequested);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(PocketsTotal);
        hash.Add(BaseParticleDraws);
        hash.Add(SurroundingParticleDraws);
        hash.Add(PocketSizeDraws);
        hash.Add(BridgeDraws);
        hash.Add(PassesDone);
        hash.Add(BaseParticlesPerCycle);
        hash.Add(BaseParticlesAccepted);
        Structural.AddList(ref hash, RandomStreamStatistics);
        Structural.AddList(ref hash, FractionDrawAccuracy);
        Structural.AddList(ref hash, Convergence);
        hash.Add(Conditions);
        hash.Add(AttemptsTotal);
        hash.Add(PocketSizeClampCount);
        hash.Add(BridgeGapExceedsPocket);
        hash.Add(BridgeWidthNegative);
        hash.Add(BridgeVolumeNaNCount);
        hash.Add(CyclesRequested);
        return hash.ToHashCode();
    }
}

/// <summary>
/// What one working pass changed — the row of the six <c>*_n</c> arrays, in the
/// original's <c>par1..par6</c> order.
/// </summary>
/// <param name="PocketDistributionError">par1, <c>epsfkarm_n</c>: <c>EPSY</c></param>
/// <param name="PocketMoment3Error">par2, <c>epsm3karm_n</c>: <c>eps(MD3)</c></param>
/// <param name="PocketMoment4Error">par3, <c>epsm4karm_n</c>: <c>eps(MD4)</c></param>
/// <param name="OxidiserMeanError">par4, <c>epsdok43_n</c>, <b>signed</b></param>
/// <param name="OxidiserVarianceError">par5, <c>epsdoksd_n</c>, <b>signed</b>, over variances</param>
/// <param name="PocketMassFraction">par6, <c>epszkarm_n</c> — not an error at all</param>
/// <remarks>
/// <para>
/// ⚠ Six names ending in <c>eps…_n</c>, and they are three different quantities.
/// Nothing in the report says so, and the mistake a reader makes is to plot them on
/// one axis and read the largest as the worst.
/// </para>
/// <list type="bullet">
/// <item><description>
/// The first three are <b>sampling</b> errors — the original's three-sigma half-width
/// <c>3·√(D/Q)/M</c> over the pockets accumulated so far. They measure how well the
/// run knows its own answer, and they fall with the sample as <c>1/√Q</c> whatever
/// the answer is. This is the series that says whether another pass is worth making.
/// </description></item>
/// <item><description>
/// The next two are errors <b>against the recipe</b>: the sampled oxidiser mean and
/// variance against the <c>DokM</c> and <c>DokSD</c> computed analytically from the
/// fractions. They keep their <b>sign</b>, where the printed scalars
/// (<c>epsx1..6</c>, <c>epsalldok</c>) are absolute, so an overshoot and an
/// undershoot read alike among the scalars and apart here. And
/// <paramref name="OxidiserVarianceError"/> is over <b>variances</b> — <c>DokSD</c>
/// is squared, which is why the report prints <c>Dok43sd</c> as
/// <c>DOKSD**0.5</c> — so it runs at roughly twice the relative error of the spread
/// and is not comparable with <c>dok43_sd</c>'s.
/// </description></item>
/// <item><description>
/// The last is <b>not an error</b>: <c>1 - DolM2</c> is the pocket mass fraction
/// itself, and the one it equals is <c>zkarm_cor1</c>, not the plain <c>zkarm</c>.
/// It does not tend to zero and is not supposed to; it is the answer settling, not
/// an accuracy.
/// </description></item>
/// </list>
/// </remarks>
public readonly record struct PassConvergence(
    double PocketDistributionError,
    double PocketMoment3Error,
    double PocketMoment4Error,
    double OxidiserMeanError,
    double OxidiserVarianceError,
    double PocketMassFraction);

/// <summary>
/// How often each of the nine rejection conditions fired, per base particle.
/// </summary>
/// <param name="DokAboveDmax">condition 1: a drawn particle exceeded the largest allowed size</param>
/// <param name="DokBelowDmin">condition 2: a drawn particle fell below the smallest</param>
/// <param name="BaseBelowHalfDok">condition 3: the base particle was under half its neighbour</param>
/// <param name="BaseAboveTwoDok">condition 4: the base particle was over twice its neighbour</param>
/// <param name="GapAbove4p7Base">condition 5: the gap exceeded 4.7 base diameters</param>
/// <param name="NoPockets">condition 6: the configuration produced no pocket at all</param>
/// <param name="FewerThanTwoBridges">condition 7: fewer than two bridges</param>
/// <param name="PocketBridgeRatioBelowMin">condition 8: pocket-to-bridge ratio under the floor</param>
/// <param name="PocketBridgeRatioAboveMax">condition 9: pocket-to-bridge ratio over the ceiling</param>
/// <remarks>
/// ⚠ The two groups are normalised by <b>different</b> divisors: the first five by
/// <c>FI + N</c>, the last four by <c>FI</c> — and <c>FI = N</c> in all 43 archived
/// runs, so the first five carry a divisor twice too large. That is a defect of the
/// original, not a second meaningful normalisation, which is why the groups are kept
/// in printed order and separated here rather than blended into one list: adding or
/// comparing across the two groups produces a number that means nothing.
/// </remarks>
public readonly record struct ConditionCounters(
    double DokAboveDmax,
    double DokBelowDmin,
    double BaseBelowHalfDok,
    double BaseAboveTwoDok,
    double GapAbove4p7Base,
    double NoPockets,
    double FewerThanTwoBridges,
    double PocketBridgeRatioBelowMin,
    double PocketBridgeRatioAboveMax);
