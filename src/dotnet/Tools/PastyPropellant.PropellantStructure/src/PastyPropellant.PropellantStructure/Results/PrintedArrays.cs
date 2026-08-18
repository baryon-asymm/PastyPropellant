namespace PastyPropellant.PropellantStructure.Results;

/// <summary>
/// Which of the original's fourteen printed arrays is a distribution, and how each
/// one's cells add up.
/// </summary>
/// <remarks>
/// <para>
/// One table, read by both sides. The kernel consults it to build
/// <see cref="Distribution"/> values, and the test that checks each distribution
/// really does sum the way it claims consults the same entry. Held in two places the
/// table would let a mislabelled array pass: the test would check the label the test
/// itself carries, not the one the result was built with.
/// </para>
/// <para>
/// Fourteen arrays, ten of them distributions. The other four are named here as well,
/// because "not a distribution" is a claim about the original that a reader should be
/// able to check rather than infer from an absence.
/// </para>
/// </remarks>
public static class PrintedArrays
{
    /// <summary>The ten distributions, with the normalisation each one carries.</summary>
    public static IReadOnlyDictionary<string, DistributionNormalisation> Distributions { get; } =
        new Dictionary<string, DistributionNormalisation>(StringComparer.Ordinal)
        {
            ["fmdok"] = DistributionNormalisation.DensityPerGridUnit,
            ["fmkarm"] = DistributionNormalisation.DensityPerGridUnit,
            ["fmkarm_cor"] = DistributionNormalisation.DensityPerGridUnit,
            ["fmkarm_cor2"] = DistributionNormalisation.DensityPerGridUnit,
            ["fqkarm"] = DistributionNormalisation.DensityPerGridUnit,
            ["fqkarm_cor"] = DistributionNormalisation.DensityPerGridUnit,
            ["fqmkm1"] = DistributionNormalisation.ProbabilityPerBin,
            ["fqmkm2"] = DistributionNormalisation.ProbabilityPerBin,
            ["coef"] = DistributionNormalisation.ProbabilityPerBin,
            ["pdoksmall"] = DistributionNormalisation.Unnormalised,
        };

    /// <summary>
    /// The four printed arrays that are not distributions, and where each one goes.
    /// </summary>
    /// <remarks>
    /// <c>Dkarmcat</c>, <c>dokkarm43</c> and <c>dokkarm10</c> are lengths against
    /// pocket category and live in <see cref="ConditionalOxidiserByPocketSize"/>;
    /// <c>epsdokfr</c> is a draw-accuracy diagnostic and lives in
    /// <see cref="RunDiagnostics.FractionDrawAccuracy"/>.
    /// </remarks>
    public static IReadOnlySet<string> NotDistributions { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "Dkarmcat", "dokkarm43", "dokkarm10", "epsdokfr",
    };

    /// <summary>
    /// The cell width of each ratio-indexed distribution, taken from the line that
    /// bins into it rather than from the header that prints above it.
    /// </summary>
    /// <remarks>
    /// ⚠ <c>fqmkm1</c> is the reason this table exists: line 629 bins on
    /// <c>int(x*1000)+1</c>, so its cells are 0.001 wide, while the header printed
    /// over it (line 1370) says 0.01 — a factor of ten. Line 1110, which computes
    /// <c>Dqmkm1</c> from the same array, uses 0.001 and settles which is right.
    /// The other two bin on <c>int(x*100)+1</c> (lines 637 and 560).
    /// </remarks>
    public static IReadOnlyDictionary<string, double> RatioGridSteps { get; } =
        new Dictionary<string, double>(StringComparer.Ordinal)
        {
            ["fqmkm1"] = 0.001,
            ["fqmkm2"] = 0.01,
            ["coef"] = 0.01,
        };
}
