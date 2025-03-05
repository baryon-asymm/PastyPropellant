using System.Text.Json;
using ParametricCombustionModel.Core.Models;

namespace ParametricCombustionModel.Core.Tests;

public class PropellantParsingTest
{
    public const string PropellantJson = @"propellants.json";

    public readonly double[] Pressures = [ 4e6, 7e6 ];

    [Fact]
    public void ParsePropellant()
    {
        var propellant = JsonSerializer.Deserialize<Propellant>(
            File.ReadAllText(PropellantJson));

        Assert.NotNull(propellant);
        Assert.Equal(Pressures.Length, propellant.PressureFrames!.Count());
        
        for (int i = 0; i < Pressures.Length; i++)
        {
            Assert.Equal(Pressures[i], propellant.PressureFrames!.ElementAt(i).Pressure);
        }
    }
}
