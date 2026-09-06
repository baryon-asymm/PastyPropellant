using System.Text.Json;
using ParametricCombustionModel.Computation.Builders;
using ParametricCombustionModel.Computation.Extensions;
using ParametricCombustionModel.Computation.Models.KnownParams;
using ParametricCombustionModel.Core.Models;

namespace ParametricCombustionModel.Computation.Tests;

/// <summary>
/// Pins that building a context with no constants at all and building it with
/// <see cref="ModelConstants.Default"/> are the same thing.
///
/// <para>Every run constant is configuration for one reason — traceability — and that only holds if the
/// unconfigured path is not a second, separate set of values. The assertion below is what keeps the two
/// entry points from drifting apart: if the implicit path ever stopped going through
/// <c>ModelConstants.Default</c>, a run without a config file would compute something no report records.</para>
///
/// <para>Migrated from <c>SkeletonContactFactorTests</c> when the skeleton contact factor was removed from
/// the model; the factor is gone, this guarantee is not.</para>
/// </summary>
public class ModelConstantsDefaultsTests
{
    private const string PropellantJson = @"propellants.json";

    /// <summary>
    /// Contexts built with no constants and with the explicit defaults are indistinguishable, and the
    /// metal melting temperature is the historical 1300 K.
    /// </summary>
    [Fact]
    public void ImplicitAndExplicitDefaultsBuildTheSameContext()
    {
        var propellants = LoadPropellants();

        var implicitDefaults = ProblemContextByUnitsMatrixBuilder.FromPropellants(propellants).BuildMatrix();
        var explicitDefaults = ProblemContextByUnitsMatrixBuilder
            .FromPropellants(propellants, ModelConstants.Default).BuildMatrix();

        Assert.Equal(
            implicitDefaults[0, 0].PocketMetalCombustionParamsByUnits.MetalMeltingTemperature.Kelvins,
            explicitDefaults[0, 0].PocketMetalCombustionParamsByUnits.MetalMeltingTemperature.Kelvins);

        Assert.Equal(
            PropellantExtensions.MetalMeltingTemperatureKelvins,
            implicitDefaults[0, 0].PocketMetalCombustionParamsByUnits.MetalMeltingTemperature.Kelvins);
    }

    private static Propellant[] LoadPropellants()
    {
        var propellant = JsonSerializer.Deserialize<Propellant>(File.ReadAllText(PropellantJson))
                         ?? throw new InvalidOperationException($"Failed to deserialize '{PropellantJson}'.");
        return [propellant];
    }
}
