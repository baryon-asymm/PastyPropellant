using System.Globalization;
using ParametricCombustionModel.ReportMaking.Reports.Pdf;
using PastyPropellant.Core.Models;

namespace ParametricCombustionModel.ReportMaking.Tests;

/// <summary>
/// Pins which slots of the 32-vector each composition heading in
/// <see cref="GroupCombustionSolverParamsReport"/> prints under.
///
/// <para>This is the one property <see cref="RailFlagTests"/> deliberately cannot see: it drives every gene
/// to the same value precisely so its assertion "does not depend on the vector's slot-to-parameter mapping".
/// That blind spot let the report read the Bas_0 properties under the Bas_2 heading and vice versa — the
/// numbers were all real and all in range, just filed under the wrong fuel, which no test and no eyeball on
/// the PDF would catch. Here every slot carries a distinct value, so a swapped block fails loudly.</para>
///
/// <para>The authority for the mapping is <c>GroupCombustionSolverParams.ToCompositionVector</c>:
/// compositionIndex 0 = the Bas_2 group = slots [11..17], 1 = Bas_1 = [18..24], 2 = Bas_0 = [25..31].</para>
/// </summary>
public class CompositionBlockAttributionTests
{
    private const int ParameterCount = 32;

    /// <summary>Slot i carries the value i + 0.5 — distinct, and never a prefix of another slot's value.</summary>
    private static double SlotValue(int slot) => slot + 0.5;

    private static IReadOnlyList<string> Render()
    {
        var groupResult = ReportFixture.MakeGroupResult(
            ReportFixture.MakeProblem([], [1e6]),
            ReportFixture.MakeProblem([], [1e6]),
            ReportFixture.MakeProblem([], [1e6]),
            bestParams: Enumerable.Range(0, ParameterCount).Select(SlotValue).ToArray(),
            lowerBound: new double[ParameterCount],
            upperBound: Enumerable.Repeat(100.0, ParameterCount).ToArray());

        return ReportFixture.TextLines(new GroupCombustionSolverParamsReport(groupResult).Transform());
    }

    /// <summary>
    /// The seven value lines under one composition heading, in emission order. Each parameter emits a value
    /// line followed by an italic bounds line ending in a rail flag; the bounds lines are dropped here.
    /// </summary>
    private static IReadOnlyList<string> ValueLinesUnder(IReadOnlyList<string> lines, string heading)
    {
        var start = lines.ToList().FindIndex(line => line.StartsWith(heading, StringComparison.Ordinal));
        Assert.True(start >= 0, $"No heading starting with '{heading}' was emitted.");

        return lines.Skip(start + 1)
            .TakeWhile(line => !line.Contains("Condensed Phase Parameters", StringComparison.Ordinal))
            .Where(line => !line.EndsWith("[RAILED@lo]", StringComparison.Ordinal)
                           && !line.EndsWith("[RAILED@hi]", StringComparison.Ordinal)
                           && !line.EndsWith("[interior]", StringComparison.Ordinal))
            .ToArray();
    }

    [Theory]
    [InlineData(0, 11)]
    [InlineData(1, 18)]
    [InlineData(2, 25)]
    public void EachCompositionHeadingPrintsItsOwnBlockOfTheVector(int compositionIndex, int blockStart)
    {
        var lines = Render();
        var heading = CompositionGroups.ReportNames[compositionIndex];
        var valueLines = ValueLinesUnder(lines, heading);

        Assert.Equal(CompositionParameterCount, valueLines.Count);

        for (var offset = 0; offset < CompositionParameterCount; offset++)
        {
            var expected = SlotValue(blockStart + offset);

            Assert.True(TrailingNumber(valueLines[offset]) == expected,
                        $"Under heading '{heading}', parameter {offset} should carry slot "
                        + $"{blockStart + offset} (= {expected}) but the line was: {valueLines[offset]}");
        }
    }

    /// <summary>
    /// The shared block must stay on slots [0..10] — the same swap class, one heading up.
    /// </summary>
    [Fact]
    public void TheSharedBlockPrintsSlotsZeroToTen()
    {
        var lines = Render();
        var start = lines.ToList().FindIndex(line =>
            line.StartsWith("Shared Parameters", StringComparison.Ordinal));
        Assert.True(start >= 0, "No shared-parameters heading was emitted.");

        var valueLines = lines.Skip(start + 1)
            .TakeWhile(line => !line.Contains("Condensed Phase Parameters", StringComparison.Ordinal))
            .Where(line => !line.EndsWith("[RAILED@lo]", StringComparison.Ordinal)
                           && !line.EndsWith("[RAILED@hi]", StringComparison.Ordinal)
                           && !line.EndsWith("[interior]", StringComparison.Ordinal))
            .ToArray();

        Assert.Equal(SharedParameterCount, valueLines.Length);

        for (var slot = 0; slot < SharedParameterCount; slot++)
        {
            var expected = SlotValue(slot);
            Assert.True(TrailingNumber(valueLines[slot]) == expected,
                        $"Shared parameter {slot} should carry slot {slot} (= {expected}) "
                        + $"but the line was: {valueLines[slot]}");
        }
    }

    /// <summary>
    /// The value a parameter line ends with. Every format string in the resources puts the value last, but
    /// not all of them place it raw — several apply a numeric specifier (e.g. F4), so the assertion compares
    /// the parsed number rather than the rendered text and stays indifferent to formatting and to which
    /// language's resources the test host resolves.
    /// </summary>
    private static double TrailingNumber(string line)
    {
        var token = line.Split(' ', StringSplitOptions.RemoveEmptyEntries).Last();

        Assert.True(double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                    || double.TryParse(token, NumberStyles.Float, CultureInfo.CurrentCulture, out value),
                    $"Expected a numeric value at the end of the line, got '{token}' in: {line}");

        return value;
    }

    // Mirrors BoundsProvider, which lives in the ConsoleApp and is not referenced from this test project.
    private const int SharedParameterCount = 11;
    private const int CompositionParameterCount = 7;
}
