using ParametricCombustionModel.ReportMaking.Reports.Pdf;

namespace ParametricCombustionModel.ReportMaking.Tests;

/// <summary>
/// Boundary coverage for the rail flag in <see cref="GroupCombustionSolverParamsReport"/>.
///
/// The flag tells the reader whether the optimiser pushed a gene onto the edge of its box, which is the cue
/// to widen the bound and re-run. It is a comparison against a relative tolerance of 1e-3 scaled by
/// max(|bound|, 1), so the interesting cases are all at the boundary itself: a value exactly on a bound, and
/// a value exactly one tolerance away from it. Both comparisons are non-strict, so both of those count as
/// railed.
///
/// Every parameter is driven to the same value against the same bounds, so all 32 emitted bounds lines carry
/// the same flag and the assertion does not depend on the vector's slot-to-parameter mapping. The bounds line
/// is identified by the flag being its last token; the report's own explanatory preamble also names the flags
/// but does not end with one.
/// </summary>
public class RailFlagTests
{
    private const int ParameterCount = 32;

    private static IReadOnlyList<string> Render(double value, double lower, double upper)
    {
        var groupResult = ReportFixture.MakeGroupResult(
            ReportFixture.MakeProblem([], [1e6]),
            ReportFixture.MakeProblem([], [1e6]),
            ReportFixture.MakeProblem([], [1e6]),
            bestParams: Enumerable.Repeat(value, ParameterCount).ToArray(),
            lowerBound: Enumerable.Repeat(lower, ParameterCount).ToArray(),
            upperBound: Enumerable.Repeat(upper, ParameterCount).ToArray());

        return ReportFixture.TextLines(new GroupCombustionSolverParamsReport(groupResult).Transform());
    }

    private static void AssertEveryParameterFlagged(
        double value,
        double lower,
        double upper,
        string expectedFlag)
    {
        string[] allFlags = ["[RAILED@lo]", "[RAILED@hi]", "[interior]"];
        var lines = Render(value, lower, upper);

        foreach (var flag in allFlags)
        {
            var expectedCount = flag == expectedFlag ? ParameterCount : 0;
            var actualCount = lines.Count(line => line.EndsWith(flag, StringComparison.Ordinal));

            Assert.True(expectedCount == actualCount,
                        $"Expected {expectedCount} bounds lines ending with '{flag}' for "
                        + $"value={value}, bounds=[{lower}; {upper}], but found {actualCount}.");
        }
    }

    /// <summary>A gene sitting exactly on its lower bound is the canonical railed case.</summary>
    [Fact]
    public void ValueExactlyOnTheLowerBound_IsRailedLow() =>
        AssertEveryParameterFlagged(value: 0.0, lower: 0.0, upper: 1.0, "[RAILED@lo]");

    /// <summary>And exactly on its upper bound.</summary>
    [Fact]
    public void ValueExactlyOnTheUpperBound_IsRailedHigh() =>
        AssertEveryParameterFlagged(value: 1.0, lower: 0.0, upper: 1.0, "[RAILED@hi]");

    /// <summary>Mid-box is the only genuinely interior case.</summary>
    [Fact]
    public void ValueInTheMiddleOfTheBox_IsInterior() =>
        AssertEveryParameterFlagged(value: 0.5, lower: 0.0, upper: 1.0, "[interior]");

    /// <summary>
    /// Exactly one tolerance above the lower bound. With bounds [0, 1] the tolerance is
    /// 1e-3 · max(|0|, 1) = 1e-3, and the comparison is "value - lower &lt;= tolerance", so this is the last
    /// value that still counts as railed.
    /// </summary>
    [Fact]
    public void ValueExactlyOneToleranceAboveTheLowerBound_IsStillRailedLow() =>
        AssertEveryParameterFlagged(value: 1e-3, lower: 0.0, upper: 1.0, "[RAILED@lo]");

    /// <summary>One ulp-ish past that tolerance the gene is interior; this is the other side of the same edge.</summary>
    [Fact]
    public void ValueJustBeyondTheLowerTolerance_IsInterior() =>
        AssertEveryParameterFlagged(value: 1.1e-3, lower: 0.0, upper: 1.0, "[interior]");

    /// <summary>
    /// The symmetric edge at the top of the box, approached from inside the tolerance.
    ///
    /// Unlike the lower edge this is not asserted exactly on the tolerance. The lower comparison is
    /// <c>value - lower</c> with lower = 0, which is exact in binary; the upper comparison is
    /// <c>upper - value</c>, and 1.0 - (1.0 - 1e-3) evaluates to 1.0000000000000009e-3 rather than 1e-3, so a
    /// value placed "exactly" one tolerance below the bound lands just outside it. That is floating-point
    /// representation, not a defect in the flag — the tolerance is a review heuristic and one ulp either way
    /// carries no meaning — so the assertion is placed where the arithmetic is unambiguous.
    /// </summary>
    [Fact]
    public void ValueWithinOneToleranceOfTheUpperBound_IsRailedHigh() =>
        AssertEveryParameterFlagged(value: 1.0 - 0.9e-3, lower: 0.0, upper: 1.0, "[RAILED@hi]");

    /// <summary>And just inside it.</summary>
    [Fact]
    public void ValueJustInsideTheUpperTolerance_IsInterior() =>
        AssertEveryParameterFlagged(value: 1.0 - 1.1e-3, lower: 0.0, upper: 1.0, "[interior]");

    /// <summary>
    /// The tolerance is relative, scaled by max(|bound|, 1), so that O(1) exponents and O(1e13)
    /// pre-exponentials are judged on the same footing. Against a lower bound of 1e13 the tolerance is 1e10,
    /// so a value 1e10 above the bound is railed even though the absolute gap is enormous.
    /// </summary>
    [Fact]
    public void ToleranceScalesWithTheMagnitudeOfTheBound() =>
        AssertEveryParameterFlagged(value: 1e13 + 1e10, lower: 1e13, upper: 1e14, "[RAILED@lo]");

    /// <summary>
    /// Twice the tolerance above the same large bound is interior — without the relative scaling this and the
    /// case above would both read as interior, and the flag would never fire on the pre-exponentials it
    /// matters most for.
    /// </summary>
    [Fact]
    public void ValueBeyondTheScaledToleranceOfALargeBound_IsInterior() =>
        AssertEveryParameterFlagged(value: 1e13 + 2e10, lower: 1e13, upper: 1e14, "[interior]");

    /// <summary>
    /// For a bound smaller than 1 the tolerance floors at 1e-3 · 1 rather than shrinking with the bound, so a
    /// tiny box is reported as railed at both edges. The lower check runs first and wins.
    /// </summary>
    [Fact]
    public void DegenerateBoxNarrowerThanTheTolerance_IsRailedLowBecauseTheLowerCheckRunsFirst() =>
        AssertEveryParameterFlagged(value: 5e-5, lower: 0.0, upper: 1e-4, "[RAILED@lo]");

    /// <summary>
    /// Every one of the 32 parameters must carry a flag. A parameter emitted without one is a silently
    /// unreviewable gene — the same defect class as a dropped table row.
    /// </summary>
    [Fact]
    public void EveryParameterEmitsExactlyOneBoundsLineWithAFlag()
    {
        var lines = Render(value: 0.5, lower: 0.0, upper: 1.0);

        var flagged = lines.Count(line => line.EndsWith("[RAILED@lo]", StringComparison.Ordinal)
                                          || line.EndsWith("[RAILED@hi]", StringComparison.Ordinal)
                                          || line.EndsWith("[interior]", StringComparison.Ordinal));

        Assert.Equal(ParameterCount, flagged);
    }
}
