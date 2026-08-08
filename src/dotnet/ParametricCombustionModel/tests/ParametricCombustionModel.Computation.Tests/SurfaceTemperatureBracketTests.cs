using System.Text.Json;
using ParametricCombustionModel.Computation.Builders;
using ParametricCombustionModel.Computation.Models.KnownParams;
using ParametricCombustionModel.Computation.Models.ProblemContexts;
using ParametricCombustionModel.Core.Models;

namespace ParametricCombustionModel.Computation.Tests;

/// <summary>
/// Pins the surface-temperature search bracket as a configured value that reaches both solver tiers.
///
/// <para><b>Why this is configuration and why it is tested.</b> The bracket used to be a pair of
/// <c>const</c>s. It is not a handbook constant: the condensed-phase solve reports failure when no root
/// lies inside it, so it acts as a soft constraint, and a composition sitting on a bound is a solution the
/// bracket imposed rather than one the physics chose. Widening or narrowing it can also move the solve
/// onto a different root, which is exactly what makes a run under an unrecorded bracket uninterpretable —
/// the same lesson as the metal melting temperature. So the value must travel from configuration into
/// every context, and the default must stay where it was.</para>
/// </summary>
public class SurfaceTemperatureBracketTests
{
    private const string PropellantJson = @"propellants.01234.json";

    /// <summary>The default is the historical 600..900 K, so an unconfigured run is unchanged.</summary>
    [Fact]
    public void DefaultBracketIsTheHistoricalOne()
    {
        Assert.Equal(SurfaceTemperatureSearchBounds.MinKelvins,
            ModelConstants.Default.MinSurfaceTemperatureKelvins);
        Assert.Equal(SurfaceTemperatureSearchBounds.MaxKelvins,
            ModelConstants.Default.MaxSurfaceTemperatureKelvins);

        var byDoubles = ProblemContextByDoublesMatrixBuilder
            .FromPropellants(LoadPropellants(), ModelConstants.Default).BuildMatrix();

        Assert.Equal(SurfaceTemperatureSearchBounds.MinKelvins, byDoubles[0, 0].MinSurfaceTemperature);
        Assert.Equal(SurfaceTemperatureSearchBounds.MaxKelvins, byDoubles[0, 0].MaxSurfaceTemperature);
    }

    /// <summary>
    /// A narrowed bracket reaches every context of both tiers. Drift here would let the search bisect one
    /// interval while the report was computed on another.
    /// </summary>
    [Fact]
    public void AConfiguredBracketReachesBothTiers()
    {
        var constants = new ModelConstants
        {
            MinSurfaceTemperatureKelvins = 600.0,
            MaxSurfaceTemperatureKelvins = 750.0
        };

        var propellants = LoadPropellants();
        var byDoubles = ProblemContextByDoublesMatrixBuilder
            .FromPropellants(propellants, constants).BuildMatrix();
        var byUnits = ProblemContextByUnitsMatrixBuilder
            .FromPropellants(propellants, constants).BuildMatrix();

        for (var i = 0; i < byDoubles.GetLength(0); i++)
        for (var j = 0; j < byDoubles.GetLength(1); j++)
        {
            Assert.Equal(600.0, byDoubles[i, j].MinSurfaceTemperature);
            Assert.Equal(750.0, byDoubles[i, j].MaxSurfaceTemperature);
            Assert.Equal(600.0, byUnits[i, j].MinSurfaceTemperature.Kelvins);
            Assert.Equal(750.0, byUnits[i, j].MaxSurfaceTemperature.Kelvins);
        }
    }

    /// <summary>An inverted or non-positive bracket is rejected up front, not at the first bisection.</summary>
    [Theory]
    [InlineData(900.0, 600.0)]
    [InlineData(700.0, 700.0)]
    [InlineData(-1.0, 900.0)]
    public void AnUnusableBracketIsRejected(double minimum, double maximum)
    {
        var constants = new ModelConstants
        {
            MinSurfaceTemperatureKelvins = minimum,
            MaxSurfaceTemperatureKelvins = maximum
        };

        Assert.Throws<ArgumentOutOfRangeException>(constants.Validate);
    }

    private static Propellant[] LoadPropellants() =>
        JsonSerializer.Deserialize<List<Propellant>>(
            File.ReadAllText(PropellantJson),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })?.ToArray()
        ?? throw new InvalidOperationException($"{PropellantJson} could not be read.");
}
