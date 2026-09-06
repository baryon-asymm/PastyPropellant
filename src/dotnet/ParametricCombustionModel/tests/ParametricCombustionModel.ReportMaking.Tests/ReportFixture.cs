using ParametricCombustionModel.Computation.Interfaces;
using ParametricCombustionModel.Computation.Models.ComputedParams;
using ParametricCombustionModel.Computation.Models.KnownParams;
using ParametricCombustionModel.Computation.Models.ProblemContexts;
using ParametricCombustionModel.Core.Models;
using ParametricCombustionModel.Core.Models.PropellantComponents;
using ParametricCombustionModel.Optimization.ConstraintPenaltyEvaluators.Interfaces;
using ParametricCombustionModel.Optimization.Models;
using ParametricCombustionModel.Optimization.Results;
using ParametricCombustionModel.ReportMaking.Interfaces;
using ParametricCombustionModel.ReportMaking.PdfOperations;
using UnitsNet;

namespace ParametricCombustionModel.ReportMaking.Tests;

/// <summary>
/// Builds the smallest problem contexts the PDF reports will accept, with every number under the test's
/// control.
///
/// The contexts are assembled field-by-field rather than through
/// <c>ProblemContextByUnitsMatrixBuilder</c> on a real propellants file on purpose: these tests pin
/// arithmetic (an OLS fit, an RMS, a rail-flag comparison), so the inputs have to be values a reader can
/// verify by hand, not whatever the checked-in data set happens to contain. The physical params that the
/// reports only echo are filled with benign placeholders — nothing under test reads them.
/// </summary>
internal static class ReportFixture
{
    /// <summary>
    /// A solver that must never be called. The reports only read already-computed params off the contexts;
    /// if one of them ever starts driving a solve, that is a behaviour change these tests should surface
    /// rather than silently absorb.
    /// </summary>
    private sealed class UnusedSolver : ISolverVisitor
    {
        public void Visit(in CombustionSolverParamsByUnits solverParamsByUnits, ProblemContextByUnits context) =>
            throw new InvalidOperationException("Reports must not run the solver.");

        public void Visit(in CombustionSolverParamsByDoubles solverParams, ProblemContextByDoubles context) =>
            throw new InvalidOperationException("Reports must not run the solver.");
    }

    /// <summary>
    /// A propellant whose Vieille coefficients are exactly <paramref name="a"/> / <paramref name="nu"/>, so
    /// the experimental burn rate <c>OptimizationProblemByUnits</c> derives is a·p^nu and is predictable.
    /// </summary>
    public static Propellant MakePropellant(string name, double a = 1e-6, double nu = 0.5) =>
        new(
            Name: name,
            A: a,
            Nu: nu,
            Components: Array.Empty<BaseComponent>(),
            Density: 1700.0,
            SpecificHeatCapacity: 1400.0,
            InitialTemperature: 293.0,
            PocketSurfaceFractionCoefficients: new[] { 0.5, 0.0 },
            ConfidenceIntervals: null,
            PocketMassFraction: 0.5,
            PressureFrames: null);

    /// <summary>One context at one pressure. Computed params start zeroed and non-converged.</summary>
    public static ProblemContextByUnits MakeContext(Propellant propellant, double pascals) =>
        new()
        {
            Propellant = propellant,
            Pressure = Pressure.FromPascals(pascals),
            PropellantParamsByUnits = new PropellantParamsByUnits
            {
                AverageOxidizerDiameter = Length.FromMicrometers(100.0),
                Density = Density.FromKilogramsPerCubicMeter(propellant.Density),
                InitialTemperature = Temperature.FromKelvins(propellant.InitialTemperature),
                SkeletonCoverage = SkeletonCoverageCurve.Constant(0.5),
                SpecificHeatCapacity =
                    SpecificEntropy.FromJoulesPerKilogramKelvin(propellant.SpecificHeatCapacity)
            },
            InterPocketKineticFlameParamsByUnits = MakeKineticFlameParams(),
            PocketSkeletonKineticFlameParamsByUnits = MakeKineticFlameParams(),
            PocketOutSkeletonKineticFlameParamsByUnits = MakeKineticFlameParams(),
            PocketDiffusionFlameParamsByUnits = new DiffusionFlameParamsByUnits
            {
                FinalTemperature = Temperature.FromKelvins(3000.0),
                AverageMolarMass = MolarMass.FromKilogramsPerMole(0.025),
                ThermalConductivity = ThermalConductivity.FromWattsPerMeterKelvin(0.2),
                VolumetricSpecificHeatCapacity = SpecificEntropy.FromJoulesPerKilogramKelvin(1500.0)
            },
            PocketMetalCombustionParamsByUnits = new MetalCombustionParamsByUnits
            {
                MetalBoilingTemperature = Temperature.FromKelvins(2740.0),
                MetalMeltingTemperature = Temperature.FromKelvins(933.0)
            },
            SkeletonLayerParamsByUnits = new SkeletonLayerParamsByUnits
            {
                Porosity = Ratio.FromDecimalFractions(0.3),
                CondensedThermalConductivity = ThermalConductivity.FromWattsPerMeterKelvin(150.0)
            },
            InterPocketVolumeFraction = Ratio.FromDecimalFractions(0.5),
            PocketVolumeFraction = Ratio.FromDecimalFractions(0.5),
            MixedCombustionParams = new MixedCombustionParams(),
            InterPocketCombustionParams = new InterPocketCombustionParams(),
            PocketCombustionParams = new PocketCombustionParams()
        };

    private static KineticFlameParamsByUnits MakeKineticFlameParams() =>
        new()
        {
            FinalTemperature = Temperature.FromKelvins(2500.0),
            AverageMolarMass = MolarMass.FromKilogramsPerMole(0.025),
            ThermalConductivity = ThermalConductivity.FromWattsPerMeterKelvin(0.2),
            VolumetricSpecificHeatCapacity = SpecificEntropy.FromJoulesPerKilogramKelvin(1500.0)
        };

    /// <summary>
    /// A [fuel, pressure] problem over the cartesian product of <paramref name="propellants"/> and
    /// <paramref name="pascals"/>. Experimental burn rates are derived by the production constructor.
    /// </summary>
    public static OptimizationProblemByUnits MakeProblem(
        IReadOnlyList<Propellant> propellants,
        IReadOnlyList<double> pascals)
    {
        var matrix = new ProblemContextByUnits[propellants.Count, pascals.Count];
        for (var fuel = 0; fuel < propellants.Count; fuel++)
        for (var pressure = 0; pressure < pascals.Count; pressure++)
            matrix[fuel, pressure] = MakeContext(propellants[fuel], pascals[pressure]);

        return new OptimizationProblemByUnits(matrix, new UnusedSolver(), Array.Empty<IPenaltyEvaluator>());
    }

    /// <summary>Marks one point converged at the given burn rate.</summary>
    public static void SetConverged(
        OptimizationProblemByUnits problem,
        int fuel,
        int pressure,
        double metersPerSecond) =>
        problem.ProblemContextMatrix[fuel, pressure].MixedCombustionParams = new MixedCombustionParams
        {
            BurnRateIsFound = true,
            BurnRate = Speed.FromMetersPerSecond(metersPerSecond)
        };

    /// <summary>
    /// Marks one point non-converged while leaving a plausible positive burn rate in the field — the exact
    /// state the per-worker contexts are left in when a solve fails after a previous one succeeded, and the
    /// state the convergence guard exists to catch.
    /// </summary>
    public static void SetNonConvergedWithStaleRate(
        OptimizationProblemByUnits problem,
        int fuel,
        int pressure,
        double staleMetersPerSecond) =>
        problem.ProblemContextMatrix[fuel, pressure].MixedCombustionParams = new MixedCombustionParams
        {
            BurnRateIsFound = false,
            BurnRate = Speed.FromMetersPerSecond(staleMetersPerSecond)
        };

    /// <summary>
    /// A grouped result over three composition contexts. <paramref name="bestParams"/> defaults to zeros;
    /// bounds default to [0, 1] per slot.
    /// </summary>
    public static GroupOptimizationResult MakeGroupResult(
        OptimizationProblemByUnits composition0,
        OptimizationProblemByUnits composition1,
        OptimizationProblemByUnits composition2,
        double[]? bestParams = null,
        double[]? lowerBound = null,
        double[]? upperBound = null) =>
        new(
            [composition0, composition1, composition2],
            lowerBound ?? new double[32],
            upperBound ?? Enumerable.Repeat(1.0, 32).ToArray(),
            bestParams ?? new double[32]);

    /// <summary>
    /// The text a report emitted, one entry per <see cref="PrintTextOperation"/> in queue order. Tabs and
    /// line breaks carry no text, so they are dropped: these tests assert on content, not on layout.
    /// </summary>
    public static IReadOnlyList<string> TextLines(Queue<IPdfOperation> operations) =>
        operations.OfType<PrintTextOperation>().Select(op => op.Text).ToArray();

    /// <summary>The single emitted line containing <paramref name="needle"/>; fails if not exactly one.</summary>
    public static string SingleLineContaining(IReadOnlyList<string> lines, string needle)
    {
        var matches = lines.Where(line => line.Contains(needle, StringComparison.Ordinal)).ToArray();
        Assert.True(matches.Length == 1,
                    $"Expected exactly one line containing '{needle}', found {matches.Length}:"
                    + Environment.NewLine + string.Join(Environment.NewLine, matches));
        return matches[0];
    }
}
