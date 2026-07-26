using System.Text.Json;
using ParametricCombustionModel.Computation.Builders;
using ParametricCombustionModel.Computation.Models.KnownParams;
using ParametricCombustionModel.Computation.Models.ProblemContexts;
using ParametricCombustionModel.Computation.Solvers;
using ParametricCombustionModel.Core.Models;

namespace ParametricCombustionModel.Computation.Tests;

/// <summary>
/// Verifies that the <c>ByDoubles</c> and <c>ByUnits</c> variants of the solver <c>Visit</c> methods agree on the
/// <c>BurnRateIsFound</c> flag.
/// <para>
/// Every <c>Visit</c> variant re-checks the flag after computing the burn rate, so a bisection that converges to a
/// surface temperature but yields a non-positive burn rate must be reported as "not found" by both variants alike.
/// </para>
/// </summary>
public class BurnRateIsFoundParityTests
{
    private const string PropellantJson = @"propellants.json";

    /// <summary>
    /// A parameter vector that lies strictly inside the optimiser bounds declared in the console host and drives every
    /// region to a genuine solution (surface temperatures near 750-770 K, burn rates near 3 mm/s).
    /// </summary>
    private static readonly double[] ConvergingVector =
    [
        1e6,   // [0]  ADecompose                       kg/(m²·s)
        8e4,   // [1]  EDecompose                       J/mol
        1e7,   // [2]  AKineticFlameInterPocket         1/s
        1e5,   // [3]  EKineticFlameInterPocket         J/mol
        1e7,   // [4]  AKineticFlamePocketOutSkeleton   1/s
        1e5,   // [5]  EKineticFlamePocketOutSkeleton   J/mol
        1e7,   // [6]  AKineticFlamePocketSkeleton      1/s
        1e5,   // [7]  EKineticFlamePocketSkeleton      J/mol
        1.0,   // [8]  NuInterPocket
        1.0,   // [9]  NuPocketOutSkeleton
        1.0,   // [10] NuPocketSkeleton
        1e-6,  // [11] AMetalBurningConstant            m²/s
        1e-8,  // [12] BMetalBurningConstant            m³/s²
        6e6,   // [13] DeltaH                           J/kg
        1.0,   // [14] KDiffusionHeight
        1.0,   // [15] APowOrder
        2.0,   // [16] BPowOrder
        0.5    // [17] KCoefficientRadiationTemperature
    ];

    /// <summary>
    /// The degenerate case that the parity fix exists for. <c>ADecompose = 0</c> zeroes the Arrhenius decomposition
    /// rate at every temperature, so the surface heat-flux error vanishes identically across the whole bracket. The
    /// bisection therefore still returns a positive surface temperature, but the resulting burn rate is exactly zero.
    /// <para>
    /// This point lies outside the optimiser's current lower bound of 1.0 for <c>ADecompose</c>, but well inside the
    /// solver's own domain, which is what makes it a usable probe of the flag contract.
    /// </para>
    /// </summary>
    private static readonly double[] ZeroBurnRateVector = WithADecompose(0.0);

    /// <summary>
    /// A second non-positive burn-rate probe. With a negative <c>ADecompose</c> the inter-pocket flame height stays
    /// finite, so unlike <see cref="ZeroBurnRateVector"/> the bisection actually converges (near 770 K) and the guarded
    /// statement is reached with a strictly negative burn rate. This is what exercises the inter-pocket variants.
    /// <para>
    /// Like the zero case this sits outside the optimiser bounds and is used purely to pin the flag contract:
    /// <c>BurnRateIsFound</c> must never be <c>true</c> for a non-positive burn rate.
    /// </para>
    /// </summary>
    private static readonly double[] NegativeBurnRateVector = WithADecompose(-1e6);

    private static double[] WithADecompose(double aDecompose)
    {
        var vector = (double[])ConvergingVector.Clone();
        vector[0] = aDecompose;
        return vector;
    }

#region Inter-Pocket Solver

    [Fact]
    public void InterPocketSolver_ConvergingCase_FlagsAgreeAndBurnRateIsPositive()
    {
        var (units, doubles) = BuildMatrices();
        var solver = new InterPocketPropellantSolver();

        for (var j = 0; j < units.GetLength(1); j++)
        {
            var unitsParams = SolveInterPocketByUnits(units[0, j], ConvergingVector, solver);
            var doublesParams = SolveInterPocketByDoubles(doubles[0, j], ConvergingVector, solver);

            Assert.True(unitsParams.BurnRateIsFound == doublesParams.BurnRateIsFound,
                        $"BurnRateIsFound mismatch at pressure index {j}: "
                        + $"units={unitsParams.BurnRateIsFound}, doubles={doublesParams.BurnRateIsFound}.");
            Assert.True(unitsParams.BurnRateIsFound,
                        $"Expected the converging vector to yield a solution at pressure index {j}.");
            Assert.True(doublesParams.BurnRate > 0.0,
                        $"Expected a strictly positive burn rate at pressure index {j}.");
            Assert.Equal(doublesParams.BurnRate, unitsParams.BurnRate.MetersPerSecond, 12);
        }
    }

    [Fact]
    public void InterPocketSolver_ZeroBurnRateCase_FlagsAgreeAndReportNotFound()
    {
        var (units, doubles) = BuildMatrices();
        var solver = new InterPocketPropellantSolver();

        for (var j = 0; j < units.GetLength(1); j++)
        {
            var unitsParams = SolveInterPocketByUnits(units[0, j], ZeroBurnRateVector, solver);
            var doublesParams = SolveInterPocketByDoubles(doubles[0, j], ZeroBurnRateVector, solver);

            Assert.Equal(0.0, doublesParams.BurnRate);
            Assert.Equal(0.0, unitsParams.BurnRate.MetersPerSecond);
            Assert.True(unitsParams.BurnRateIsFound == doublesParams.BurnRateIsFound,
                        $"BurnRateIsFound mismatch at pressure index {j}: "
                        + $"units={unitsParams.BurnRateIsFound}, doubles={doublesParams.BurnRateIsFound}.");
            Assert.False(unitsParams.BurnRateIsFound,
                         $"A zero burn rate must not be reported as found at pressure index {j}.");
        }
    }

    [Fact]
    public void InterPocketSolver_NegativeBurnRateCase_FlagsAgreeAndReportNotFound()
    {
        var (units, doubles) = BuildMatrices();
        var solver = new InterPocketPropellantSolver();

        for (var j = 0; j < units.GetLength(1); j++)
        {
            var unitsParams = SolveInterPocketByUnits(units[0, j], NegativeBurnRateVector, solver);
            var doublesParams = SolveInterPocketByDoubles(doubles[0, j], NegativeBurnRateVector, solver);

            Assert.True(doublesParams.BurnRate <= 0.0,
                        $"Expected a non-positive burn rate at pressure index {j}.");
            Assert.Equal(doublesParams.BurnRate, unitsParams.BurnRate.MetersPerSecond, 12);
            Assert.True(unitsParams.BurnRateIsFound == doublesParams.BurnRateIsFound,
                        $"BurnRateIsFound mismatch at pressure index {j}: "
                        + $"units={unitsParams.BurnRateIsFound}, doubles={doublesParams.BurnRateIsFound}.");
            Assert.False(unitsParams.BurnRateIsFound,
                         $"A non-positive burn rate must not be reported as found at pressure index {j}.");
        }
    }

#endregion

#region Pocket Solver

    [Fact]
    public void PocketSolver_ConvergingCase_FlagsAgreeAndBurnRateIsPositive()
    {
        var (units, doubles) = BuildMatrices();
        var solver = new PocketPropellantSolver();

        for (var j = 0; j < units.GetLength(1); j++)
        {
            var unitsParams = SolvePocketByUnits(units[0, j], ConvergingVector, solver);
            var doublesParams = SolvePocketByDoubles(doubles[0, j], ConvergingVector, solver);

            Assert.True(unitsParams.BurnRateIsFound == doublesParams.BurnRateIsFound,
                        $"BurnRateIsFound mismatch at pressure index {j}: "
                        + $"units={unitsParams.BurnRateIsFound}, doubles={doublesParams.BurnRateIsFound}.");
            Assert.True(unitsParams.BurnRateIsFound,
                        $"Expected the converging vector to yield a solution at pressure index {j}.");
            Assert.True(doublesParams.BurnRate > 0.0,
                        $"Expected a strictly positive burn rate at pressure index {j}.");
        }
    }

    [Fact]
    public void PocketSolver_ZeroBurnRateCase_FlagsAgreeAndReportNotFound()
    {
        var (units, doubles) = BuildMatrices();
        var solver = new PocketPropellantSolver();

        for (var j = 0; j < units.GetLength(1); j++)
        {
            var unitsParams = SolvePocketByUnits(units[0, j], ZeroBurnRateVector, solver);
            var doublesParams = SolvePocketByDoubles(doubles[0, j], ZeroBurnRateVector, solver);

            Assert.Equal(0.0, doublesParams.BurnRate);
            Assert.Equal(0.0, unitsParams.BurnRate.MetersPerSecond);
            Assert.True(unitsParams.BurnRateIsFound == doublesParams.BurnRateIsFound,
                        $"BurnRateIsFound mismatch at pressure index {j}: "
                        + $"units={unitsParams.BurnRateIsFound}, doubles={doublesParams.BurnRateIsFound}.");
            Assert.False(unitsParams.BurnRateIsFound,
                         $"A zero burn rate must not be reported as found at pressure index {j}.");
        }
    }

    [Fact]
    public void PocketSolver_NegativeBurnRateCase_FlagsAgreeAndReportNotFound()
    {
        var (units, doubles) = BuildMatrices();
        var solver = new PocketPropellantSolver();

        for (var j = 0; j < units.GetLength(1); j++)
        {
            var unitsParams = SolvePocketByUnits(units[0, j], NegativeBurnRateVector, solver);
            var doublesParams = SolvePocketByDoubles(doubles[0, j], NegativeBurnRateVector, solver);

            Assert.True(doublesParams.BurnRate <= 0.0,
                        $"Expected a non-positive burn rate at pressure index {j}.");
            Assert.True(unitsParams.BurnRateIsFound == doublesParams.BurnRateIsFound,
                        $"BurnRateIsFound mismatch at pressure index {j}: "
                        + $"units={unitsParams.BurnRateIsFound}, doubles={doublesParams.BurnRateIsFound}.");
            Assert.False(unitsParams.BurnRateIsFound,
                         $"A non-positive burn rate must not be reported as found at pressure index {j}.");
        }
    }

#endregion

#region Mixed Solver

    [Fact]
    public void MixedSolver_ConvergingCase_FlagsAgreeAndBurnRateIsPositive()
    {
        var (units, doubles) = BuildMatrices();
        var solver = new MixedPropellantSolver();

        for (var j = 0; j < units.GetLength(1); j++)
        {
            units[0, j].Accept(CombustionSolverParamsByUnits.FromVector(ConvergingVector), solver);
            doubles[0, j].Accept(CombustionSolverParamsByDoubles.FromVector(ConvergingVector), solver);

            var unitsParams = units[0, j].MixedCombustionParams;
            var doublesParams = doubles[0, j].MixedCombustionParams;

            Assert.True(unitsParams.BurnRateIsFound == doublesParams.BurnRateIsFound,
                        $"BurnRateIsFound mismatch at pressure index {j}: "
                        + $"units={unitsParams.BurnRateIsFound}, doubles={doublesParams.BurnRateIsFound}.");
            Assert.True(unitsParams.BurnRateIsFound,
                        $"Expected the converging vector to yield a solution at pressure index {j}.");
            Assert.True(doublesParams.BurnRate > 0.0,
                        $"Expected a strictly positive burn rate at pressure index {j}.");
        }
    }

    [Fact]
    public void MixedSolver_ZeroBurnRateCase_FlagsAgreeAndReportNotFound()
    {
        var (units, doubles) = BuildMatrices();
        var solver = new MixedPropellantSolver();

        for (var j = 0; j < units.GetLength(1); j++)
        {
            units[0, j].Accept(CombustionSolverParamsByUnits.FromVector(ZeroBurnRateVector), solver);
            doubles[0, j].Accept(CombustionSolverParamsByDoubles.FromVector(ZeroBurnRateVector), solver);

            var unitsParams = units[0, j].MixedCombustionParams;
            var doublesParams = doubles[0, j].MixedCombustionParams;

            Assert.True(unitsParams.BurnRateIsFound == doublesParams.BurnRateIsFound,
                        $"BurnRateIsFound mismatch at pressure index {j}: "
                        + $"units={unitsParams.BurnRateIsFound}, doubles={doublesParams.BurnRateIsFound}.");
            Assert.False(unitsParams.BurnRateIsFound,
                         $"A zero burn rate must not be reported as found at pressure index {j}.");
        }
    }

    [Fact]
    public void MixedSolver_NegativeBurnRateCase_FlagsAgreeAndReportNotFound()
    {
        var (units, doubles) = BuildMatrices();
        var solver = new MixedPropellantSolver();

        for (var j = 0; j < units.GetLength(1); j++)
        {
            units[0, j].Accept(CombustionSolverParamsByUnits.FromVector(NegativeBurnRateVector), solver);
            doubles[0, j].Accept(CombustionSolverParamsByDoubles.FromVector(NegativeBurnRateVector), solver);

            var unitsParams = units[0, j].MixedCombustionParams;
            var doublesParams = doubles[0, j].MixedCombustionParams;

            Assert.True(unitsParams.BurnRateIsFound == doublesParams.BurnRateIsFound,
                        $"BurnRateIsFound mismatch at pressure index {j}: "
                        + $"units={unitsParams.BurnRateIsFound}, doubles={doublesParams.BurnRateIsFound}.");
            Assert.False(unitsParams.BurnRateIsFound,
                         $"A non-positive burn rate must not be reported as found at pressure index {j}.");
        }
    }

#endregion

#region Field-by-field parity

    /// <summary>
    /// Compares every computed field of the units-based and doubles-based paths, not just the
    /// <c>BurnRateIsFound</c> flag.
    /// <para>
    /// Ported from <c>MixedPropellantSolverTester.TestProblemContextComparison</c>, which lived in the legacy
    /// root <c>tests/</c> tree. That tree was never a member of the solution and had not compiled since a
    /// repository restructure, so this assertion had not run in a long time. The <c>ByDoubles</c>/<c>ByUnits</c>
    /// pair is a first-class convention in this codebase, and the flag-only check is too narrow to protect it:
    /// the two paths can agree on "a solution was found" while disagreeing on the numbers.
    /// </para>
    /// <para>
    /// The original tolerance of 1e-6 is kept. It is absolute, so it is loose for heat fluxes (order 1e6-1e9
    /// W/m²) and tight for burn rates (order 1e-3 m/s) — worth tightening to a relative comparison if this ever
    /// needs to catch small proportional drift.
    /// </para>
    /// </summary>
    [Fact]
    public void MixedSolver_ConvergingCase_EveryComputedFieldAgrees()
    {
        var (units, doubles) = BuildMatrices();
        var solver = new MixedPropellantSolver();

        for (var j = 0; j < units.GetLength(1); j++)
        {
            units[0, j].Accept(CombustionSolverParamsByUnits.FromVector(ConvergingVector), solver);
            doubles[0, j].Accept(CombustionSolverParamsByDoubles.FromVector(ConvergingVector), solver);

            CompareMixed(units[0, j].MixedCombustionParams, doubles[0, j].MixedCombustionParams, j);
            CompareInterPocket(units[0, j].InterPocketCombustionParams,
                               doubles[0, j].InterPocketCombustionParams, j);

            // The original harness defined this comparison but never called it, so the pocket side has never
            // had parity coverage. It is wired in here.
            ComparePocket(units[0, j].PocketCombustionParams, doubles[0, j].PocketCombustionParams, j);
        }
    }

    private const double Tolerance = 1e-6;

    private static void Close(double units, double doubles, string field, int pressureIndex) =>
        Assert.True(Math.Abs(units - doubles) < Tolerance,
                    $"{field} mismatch at pressure index {pressureIndex}: units={units}, doubles={doubles}.");

    private static void CompareMixed(
        Models.ComputedParams.MixedCombustionParams units,
        Models.ComputedParams.MixedCombustionParamsByDoubles doubles,
        int j)
    {
        Assert.True(units.BurnRateIsFound == doubles.BurnRateIsFound, $"BurnRateIsFound mismatch at index {j}.");
        Close(units.BurnRate.MetersPerSecond, doubles.BurnRate, "Mixed.BurnRate", j);
    }

    private static void CompareInterPocket(
        Models.ComputedParams.InterPocketCombustionParams units,
        Models.ComputedParams.InterPocketCombustionParamsByDoubles doubles,
        int j)
    {
        Assert.True(units.BurnRateIsFound == doubles.BurnRateIsFound,
                    $"InterPocket.BurnRateIsFound mismatch at index {j}.");
        Close(units.SurfaceTemperature.Kelvins, doubles.SurfaceTemperature, "InterPocket.SurfaceTemperature", j);
        Close(units.BurnRate.MetersPerSecond, doubles.BurnRate, "InterPocket.BurnRate", j);
        Close(units.DecomposeRate.KilogramsPerSecondPerSquareMeter, doubles.DecomposeRate,
              "InterPocket.DecomposeRate", j);
        Close(units.SublimationHeatFlux.WattsPerSquareMeter, doubles.SublimationHeatFlux,
              "InterPocket.SublimationHeatFlux", j);
        Close(units.SurfaceHeatFluxesError.WattsPerSquareMeter, doubles.SurfaceHeatFluxesError,
              "InterPocket.SurfaceHeatFluxesError", j);

        CompareKineticFlame(units.KineticFlameCombustionParams, doubles.KineticFlameCombustionParams,
                            "InterPocket", j);
    }

    private static void ComparePocket(
        Models.ComputedParams.PocketCombustionParams units,
        Models.ComputedParams.PocketCombustionParamsByDoubles doubles,
        int j)
    {
        Assert.True(units.BurnRateIsFound == doubles.BurnRateIsFound,
                    $"Pocket.BurnRateIsFound mismatch at index {j}.");
        Close(units.SurfaceTemperature.Kelvins, doubles.SurfaceTemperature, "Pocket.SurfaceTemperature", j);
        Close(units.BurnRate.MetersPerSecond, doubles.BurnRate, "Pocket.BurnRate", j);
        Close(units.DecomposeRate.KilogramsPerSecondPerSquareMeter, doubles.DecomposeRate,
              "Pocket.DecomposeRate", j);

        CompareKineticFlame(units.OutSkeletonKineticFlameCombustionParams,
                            doubles.OutSkeletonKineticFlameCombustionParams, "Pocket.OutSkeleton", j);
        CompareKineticFlame(units.SkeletonKineticFlameCombustionParams,
                            doubles.SkeletonKineticFlameCombustionParams, "Pocket.Skeleton", j);

        Close(units.MetalBurningHeatFlux.WattsPerSquareMeter, doubles.MetalBurningHeatFlux,
              "Pocket.MetalBurningHeatFlux", j);

        Close(units.DiffusionFlameHeight.Meters, doubles.DiffusionFlameHeight, "Pocket.DiffusionFlameHeight", j);
        Close(units.DiffusionFlameHeatFlux.WattsPerSquareMeter, doubles.DiffusionFlameHeatFlux,
              "Pocket.DiffusionFlameHeatFlux", j);

        Close(units.OutSkeletonHeatFlux.WattsPerSquareMeter, doubles.OutSkeletonHeatFlux,
              "Pocket.OutSkeletonHeatFlux", j);
        Close(units.SkeletonHeatFlux.WattsPerSquareMeter, doubles.SkeletonHeatFlux, "Pocket.SkeletonHeatFlux", j);
        Close(units.ToSurfaceTotalHeatFlux.WattsPerSquareMeter, doubles.ToSurfaceTotalHeatFlux,
              "Pocket.ToSurfaceTotalHeatFlux", j);
        Close(units.SublimationHeatFlux.WattsPerSquareMeter, doubles.SublimationHeatFlux,
              "Pocket.SublimationHeatFlux", j);
        Close(units.SurfaceHeatFluxesError.WattsPerSquareMeter, doubles.SurfaceHeatFluxesError,
              "Pocket.SurfaceHeatFluxesError", j);
    }

    private static void CompareKineticFlame(
        Models.ComputedParams.KineticFlameCombustionParams units,
        Models.ComputedParams.KineticFlameCombustionParamsByDoubles doubles,
        string region,
        int j)
    {
        Close(units.KineticFlameHeight.Meters, doubles.KineticFlameHeight, $"{region}.KineticFlameHeight", j);
        Close(units.AverageKineticFlameDensity.KilogramsPerCubicMeter, doubles.AverageKineticFlameDensity,
              $"{region}.AverageKineticFlameDensity", j);
        Close(units.AverageKineticFlameTemperature.Kelvins, doubles.AverageKineticFlameTemperature,
              $"{region}.AverageKineticFlameTemperature", j);
        Close(units.KineticFlameHeatFlux.WattsPerSquareMeter, doubles.KineticFlameHeatFlux,
              $"{region}.KineticFlameHeatFlux", j);
    }

#endregion

#region Fixture

    /// <summary>
    /// Builds the units-based and doubles-based problem-context matrices from the same propellant definition, so that
    /// both solver paths see identical inputs.
    /// </summary>
    private static (ProblemContextByUnits[,] Units, ProblemContextByDoubles[,] Doubles) BuildMatrices()
    {
        var propellant = JsonSerializer.Deserialize<Propellant>(File.ReadAllText(PropellantJson))
                         ?? throw new InvalidOperationException($"Failed to deserialize '{PropellantJson}'.");
        var propellants = new[] { propellant };

        return (ProblemContextByUnitsMatrixBuilder.FromPropellants(propellants).BuildMatrix(),
                ProblemContextByDoublesMatrixBuilder.FromPropellants(propellants).BuildMatrix());
    }

    private static Models.ComputedParams.InterPocketCombustionParams SolveInterPocketByUnits(
        ProblemContextByUnits context,
        double[] vector,
        InterPocketPropellantSolver solver)
    {
        context.Accept(CombustionSolverParamsByUnits.FromVector(vector), solver);
        return context.InterPocketCombustionParams;
    }

    private static Models.ComputedParams.InterPocketCombustionParamsByDoubles SolveInterPocketByDoubles(
        ProblemContextByDoubles context,
        double[] vector,
        InterPocketPropellantSolver solver)
    {
        context.Accept(CombustionSolverParamsByDoubles.FromVector(vector), solver);
        return context.InterPocketCombustionParams;
    }

    private static Models.ComputedParams.PocketCombustionParams SolvePocketByUnits(
        ProblemContextByUnits context,
        double[] vector,
        PocketPropellantSolver solver)
    {
        context.Accept(CombustionSolverParamsByUnits.FromVector(vector), solver);
        return context.PocketCombustionParams;
    }

    private static Models.ComputedParams.PocketCombustionParamsByDoubles SolvePocketByDoubles(
        ProblemContextByDoubles context,
        double[] vector,
        PocketPropellantSolver solver)
    {
        context.Accept(CombustionSolverParamsByDoubles.FromVector(vector), solver);
        return context.PocketCombustionParams;
    }

#endregion
}
