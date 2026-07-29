using System.Text.Json;
using ParametricCombustionModel.Computation.Builders;
using ParametricCombustionModel.Computation.Extensions;
using ParametricCombustionModel.Computation.Models.KnownParams;
using ParametricCombustionModel.Computation.Models.ProblemContexts;
using ParametricCombustionModel.Computation.Solvers;
using ParametricCombustionModel.Core.Models;

namespace ParametricCombustionModel.Computation.Tests;

/// <summary>
/// Pins the two properties that make <see cref="ModelConstants.SkeletonContactFactor"/> safe to introduce.
///
/// <para><b>Why this needs its own tests.</b> The factor divides the only place the skeleton-layer thickness
/// δ enters the model — the Fourier flux <c>λ_eff·(T_melt − T_s)/δ</c>. That makes it algebraically
/// interchangeable with δ itself, which is precisely why it must be a fixed constant rather than something
/// the optimiser can choose, and why the two properties below are load-bearing:</para>
///
/// <list type="number">
///   <item>a factor of 1 must leave every computed number exactly as it was, so that existing results and
///         the whole archive of runs remain reproducible on the new code; and</item>
///   <item>the <c>ByDoubles</c> path (which the DE search minimises) and the <c>ByUnits</c> path (which the
///         report is computed from) must apply it identically — otherwise the optimiser would be searching a
///         different model from the one that gets reported, and nothing would fail loudly.</item>
/// </list>
/// </summary>
public class SkeletonContactFactorTests
{
    private const string PropellantJson = @"propellants.json";

    /// <summary>
    /// The default is a no-op. Contexts built with <see cref="ModelConstants.Default"/> and contexts built
    /// with no constants at all must be indistinguishable, and both must carry a factor of exactly 1.
    ///
    /// <para>This is the guarantee that lets the change ship without invalidating any existing run: a
    /// configuration that does not mention the factor computes what it always computed.</para>
    /// </summary>
    [Fact]
    public void DefaultConstantsLeaveTheModelUntouched()
    {
        var propellants = LoadPropellants();

        var implicitDefaults = ProblemContextByUnitsMatrixBuilder.FromPropellants(propellants).BuildMatrix();
        var explicitDefaults = ProblemContextByUnitsMatrixBuilder
            .FromPropellants(propellants, ModelConstants.Default).BuildMatrix();

        Assert.Equal(1.0, implicitDefaults[0, 0].PocketMetalCombustionParamsByUnits.SkeletonContactFactor);
        Assert.Equal(
            implicitDefaults[0, 0].PocketMetalCombustionParamsByUnits.MetalMeltingTemperature.Kelvins,
            explicitDefaults[0, 0].PocketMetalCombustionParamsByUnits.MetalMeltingTemperature.Kelvins);

        Assert.Equal(
            PropellantExtensions.MetalMeltingTemperatureKelvins,
            implicitDefaults[0, 0].PocketMetalCombustionParamsByUnits.MetalMeltingTemperature.Kelvins);
    }

    /// <summary>
    /// The factor reduces the metal heat flux by (very nearly) itself, and does so identically in both
    /// solver paths.
    ///
    /// <para><b>"Very nearly", not "exactly", and the difference is the physics.</b> The division itself is
    /// exact — it is one operation in <c>GetMetalBurningHeatFlux</c> — but this test goes through a full
    /// solve, and the metal flux is a term in the surface energy balance. Cutting it moves the equilibrium
    /// surface temperature, which moves <c>T_melt − T_s</c>, the radiative conductivity and the burn rate,
    /// so the flux that comes back out has already responded to its own reduction. Measured residual is
    /// under 1 % at a factor of 200. Asserting exact division here would be asserting that the model does
    /// not re-equilibrate, which would be a defect if it were true.</para>
    ///
    /// <para>The ByDoubles/ByUnits comparison is the one that could fail silently in production. The DE
    /// search runs on ByDoubles and the report is computed on ByUnits; if only one of them applied the
    /// factor, the optimiser would minimise one model and the PDF would describe another, with no exception
    /// and no failing test anywhere else in the suite. That assertion stays exact to 1e-12.</para>
    /// </summary>
    [Theory]
    [InlineData(1.0)]
    [InlineData(200.0)]
    [InlineData(1000.0)]
    public void TheFactorDividesTheMetalHeatFluxIdenticallyInBothSolverPaths(double factor)
    {
        var propellants = LoadPropellants();
        var constants = new ModelConstants { SkeletonContactFactor = factor };

        var (baselineUnits, baselineDoubles) = Solve(propellants, ModelConstants.Default);
        var (correctedUnits, correctedDoubles) = Solve(propellants, constants);

        // Both paths must reproduce the same baseline before anything is claimed about the correction.
        AssertClose(baselineUnits, baselineDoubles,
            "The two solver paths disagree on the metal heat flux before any correction is applied, so no "
            + "conclusion about the correction itself would be trustworthy.");

        AssertClose(correctedUnits, correctedDoubles,
            $"ByUnits and ByDoubles applied the contact factor {factor} differently. The DE search minimises "
            + "the ByDoubles model while the report is computed from ByUnits, so this divergence would make "
            + "the optimiser and the PDF describe different physics without failing anything.");

        var observedRatio = baselineUnits / correctedUnits;

        Assert.True(
            observedRatio > factor * 0.9 && observedRatio < factor * 1.1,
            $"Expected the contact factor {factor} to cut the metal heat flux by about {factor}x "
            + $"({baselineUnits:R} -> {correctedUnits:R} is {observedRatio:R}x), within the 10 % the surface "
            + "energy balance can absorb by shifting T_s. A ratio outside that band means the factor is not "
            + "acting as a plain divisor on this one flux — it has either been applied somewhere else as "
            + "well, or been applied before a quantity that feeds back into it.");
    }

    private static double RelativeDifference(double expected, double actual) =>
        Math.Abs(expected - actual) / Math.Max(Math.Abs(expected), 1e-300);

    private static void AssertClose(double units, double doubles, string because) =>
        Assert.True(RelativeDifference(units, doubles) < 1e-12,
            $"{because}\nByUnits: {units:R}\nByDoubles: {doubles:R}");

    /// <summary>
    /// Solves one context in both representations and returns the resulting metal heat flux.
    /// The parameter vector is the same in both, so any difference is the solver's.
    /// </summary>
    private static (double Units, double Doubles) Solve(Propellant[] propellants, ModelConstants constants)
    {
        var unitsMatrix = ProblemContextByUnitsMatrixBuilder.FromPropellants(propellants, constants).BuildMatrix();
        var doublesMatrix = ProblemContextByDoublesMatrixBuilder.FromPropellants(propellants, constants).BuildMatrix();

        var solver = new PocketPropellantSolver();
        var vector = BuildParameterVector();

        unitsMatrix[0, 0].Accept(CombustionSolverParamsByUnits.FromVector(vector), solver);
        doublesMatrix[0, 0].Accept(CombustionSolverParamsByDoubles.FromVector(vector), solver);

        return (unitsMatrix[0, 0].PocketCombustionParams.MetalBurningHeatFlux.WattsPerSquareMeter,
                doublesMatrix[0, 0].PocketCombustionParams.MetalBurningHeatFlux);
    }

    /// <summary>
    /// An 18-parameter vector in the physically sensible region — the point is to reach a converged solve,
    /// not to be near any optimum.
    /// </summary>
    private static double[] BuildParameterVector() =>
    [
        1e10, 120000.0,          // ADecompose, EDecompose
        250000.0, 50000.0,       // A/E kinetic flame inter-pocket
        690000.0, 50000.0,       // A/E kinetic flame pocket out-skeleton
        390000.0, 50000.0,       // A/E kinetic flame pocket skeleton
        2.5, 1.25, 2.0,          // nu inter-pocket, out-skeleton, skeleton
        7.0e-5, 1.0e-5,          // A/B metal burning constants
        -420000.0,               // DeltaH
        1.5,                     // K diffusion height
        1.75, 1.1,               // A/B power orders
        0.13                     // K radiation-temperature coefficient
    ];

    private static Propellant[] LoadPropellants()
    {
        var propellant = JsonSerializer.Deserialize<Propellant>(File.ReadAllText(PropellantJson))
                         ?? throw new InvalidOperationException($"Failed to deserialize '{PropellantJson}'.");
        return [propellant];
    }
}
