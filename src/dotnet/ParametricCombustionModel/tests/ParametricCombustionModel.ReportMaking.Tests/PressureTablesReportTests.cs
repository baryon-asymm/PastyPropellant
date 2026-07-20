using ParametricCombustionModel.Optimization.Models;
using ParametricCombustionModel.ReportMaking.Reports.Pdf;

namespace ParametricCombustionModel.ReportMaking.Tests;

/// <summary>
/// Structural coverage for <see cref="PressureTablesReport"/>.
///
/// This is the defect class BUG-1 belonged to: a fully-populated row was built and then dropped because the
/// block that built it was missing its <c>rows.Add(...)</c>. Every cell that did get emitted was correct, so
/// nothing downstream complained and the row was silently absent from every generated PDF for a long time.
/// Only a count catches that, which is why the row count is asserted as an exact number rather than a
/// lower bound.
///
/// Row labels come from localised resources, so they are deliberately not asserted — the shape of the table
/// is the invariant, not the language it is rendered in.
/// </summary>
public class PressureTablesReportTests
{
    /// <summary>
    /// The table layout: a pressure header, a fuel-name header, experimental / calculated / difference burn
    /// rates, an inter-pocket vs pocket column header, then the eleven per-region characteristic rows.
    /// BUG-1 made this 16.
    /// </summary>
    private const int ExpectedRowCount = 17;

    private static OptimizationResult MakeResult(int fuelCount, int pressureCount)
    {
        var propellants = Enumerable.Range(0, fuelCount)
                                    .Select(i => ReportFixture.MakePropellant($"Bas_{i}"))
                                    .ToArray();
        var pascals = Enumerable.Range(0, pressureCount)
                                .Select(i => 1e6 * (i + 1))
                                .ToArray();

        var problem = ReportFixture.MakeProblem(propellants, pascals);
        for (var fuel = 0; fuel < fuelCount; fuel++)
        for (var pressure = 0; pressure < pressureCount; pressure++)
            ReportFixture.SetConverged(problem, fuel, pressure, 0.003);

        return new OptimizationResult(new double[18], new double[18], new double[18], problem);
    }

    [Fact]
    public void Transform_EmitsOneTablePerRequestedPressurePoint()
    {
        var result = MakeResult(fuelCount: 2, pressureCount: 4);

        var tables = new PressureTablesReport([0, 2, 3], result).Transform();

        Assert.Equal(3, tables.Count);
    }

    /// <summary>
    /// The BUG-1 regression proper. Every table must carry the full row set, whatever the fuel count.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    public void Transform_EveryTableHasTheFullRowSet(int fuelCount)
    {
        var result = MakeResult(fuelCount, pressureCount: 3);

        var tables = new PressureTablesReport([0, 1, 2], result).Transform();

        Assert.All(tables, table => Assert.Equal(ExpectedRowCount, table.Rows.Count));
    }

    /// <summary>
    /// A dropped row would also be caught here if it happened to be the last one, but the real point of this
    /// assertion is the transpose: a row built with the wrong cell count is as invisible on the page as a
    /// missing one.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    public void Transform_EveryRowIsRectangularAtTwoCellsPerFuelPlusLabel(int fuelCount)
    {
        var result = MakeResult(fuelCount, pressureCount: 2);
        var expectedColumnCount = 2 * fuelCount + 1;

        var tables = new PressureTablesReport([0, 1], result).Transform();

        foreach (var table in tables)
        {
            Assert.Equal(expectedColumnCount, table.ColumnProportions.Count);
            Assert.All(table.Rows, row => Assert.Equal(expectedColumnCount, row.Count));
        }
    }

    /// <summary>
    /// No row may be blank across every cell. A row that is emitted but never populated is the mirror image
    /// of BUG-1 and just as invisible in review.
    /// </summary>
    [Fact]
    public void Transform_NoRowIsEntirelyEmpty()
    {
        var result = MakeResult(fuelCount: 2, pressureCount: 2);

        var tables = new PressureTablesReport([0, 1], result).Transform();

        foreach (var table in tables)
            Assert.All(table.Rows, row => Assert.Contains(row, cell => !string.IsNullOrWhiteSpace(cell)));
    }

    /// <summary>
    /// The label column is the only one that is populated for every row, so an empty label is a row that
    /// renders as an unattributed strip of numbers.
    /// </summary>
    [Fact]
    public void Transform_EveryRowBelowTheHeaderIsLabelled()
    {
        var result = MakeResult(fuelCount: 2, pressureCount: 1);

        var table = new PressureTablesReport([0], result).Transform().Single();

        // Row 0 is the pressure header, whose first cell is the pressure itself.
        foreach (var row in table.Rows)
            Assert.False(string.IsNullOrWhiteSpace(row[0]));
    }

    /// <summary>
    /// A non-converged point must render the marker in both the calculated-rate row and the
    /// experiment-minus-calculated row, and must not leak the stale value that is still sitting in the
    /// context. The converged fuel alongside it must be untouched.
    /// </summary>
    [Fact]
    public void Transform_NonConvergedPointRendersTheMarkerInsteadOfTheStaleRate()
    {
        var result = MakeResult(fuelCount: 2, pressureCount: 1);
        const double staleMetersPerSecond = 0.00777;
        ReportFixture.SetNonConvergedWithStaleRate(result.OptimizedContext, fuel: 0, pressure: 0,
                                                   staleMetersPerSecond);
        ReportFixture.SetConverged(result.OptimizedContext, fuel: 1, pressure: 0, 0.004);

        var table = new PressureTablesReport([0], result).Transform().Single();

        var markerCells = table.Rows.SelectMany(row => row)
                               .Count(cell => cell.Contains("NOT CONVERGED", StringComparison.Ordinal));

        // Exactly two: the calculated burn rate and the experiment-minus-calculated difference, for the one
        // non-converged fuel only.
        Assert.Equal(2, markerCells);

        // 0.00777 m/s is 7.77 mm/s — the number a reader would otherwise have taken for a result.
        Assert.DoesNotContain(table.Rows.SelectMany(row => row),
                              cell => cell.Contains("7.77", StringComparison.Ordinal));

        // The row set is unchanged by a failed solve: a shorter table would be a second BUG-1.
        Assert.Equal(ExpectedRowCount, table.Rows.Count);
    }
}
