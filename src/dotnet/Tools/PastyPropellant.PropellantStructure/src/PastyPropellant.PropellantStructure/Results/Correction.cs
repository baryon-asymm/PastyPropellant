namespace PastyPropellant.PropellantStructure.Results;

/// <summary>
/// Which of the original's parallel correction accumulators a value came from.
/// </summary>
/// <remarks>
/// <para>
/// The variants are <b>independent</b>: three accumulators filled side by side, not
/// three stages of one correction. None is computed from another's result, so there
/// is no "more corrected" variant and none can be chosen on the caller's behalf.
/// </para>
/// <para>
/// Which quantity carries which branches differs, and the dictionaries on
/// <see cref="StructureResult"/> carry exactly the set that quantity has: three for
/// the pocket mass fraction (<c>zkarm</c>, <c>zkarm_cor1</c>, <c>zkarm_cor2</c> =
/// 1 − <c>DolM1/2/3</c>, lines 1137/1146/1151) and for both <c>D43</c> values, two
/// for <c>D10</c> (<c>dkarm10</c>, <c>dkarm10_cor</c>).
/// </para>
/// <para>
/// There is deliberately no second axis for the original's two <i>estimators</i>.
/// <c>Dkarm43(1)</c> is a ratio of moments taken straight off the sample
/// (<c>DP43 = DP41/DP31</c>, line 994); <c>Dkarm43(2)</c> is the same moment off the
/// <c>VKSO</c> histogram. They are two estimators of one quantity — across the 43
/// archived runs they differ in 13 of them by at most 1e-4 relative, against a print
/// resolution of 4.8e-5 — while the correction axis on the same field moves the
/// value by up to 110%. Both of the corrections are built on the histogram, so the
/// dictionaries use the histogram estimator throughout: keying <see cref="None"/> to
/// the sample-moment value would compare an uncorrected number against corrected
/// ones computed a different way, which is the substitution this node exists to
/// prevent. <c>dkarm43_v1</c> stays reachable through
/// <see cref="StructureResult.Printed"/> under its own name.
/// </para>
/// </remarks>
public enum Correction
{
    /// <summary>No correction applied.</summary>
    None = 0,

    /// <summary>The report's "cor. var. #1".</summary>
    Variant1 = 1,

    /// <summary>The report's "cor. var. #2".</summary>
    Variant2 = 2,
}
