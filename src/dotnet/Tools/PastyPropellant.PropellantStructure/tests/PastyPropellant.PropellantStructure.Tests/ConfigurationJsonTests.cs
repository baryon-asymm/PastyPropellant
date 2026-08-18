using PastyPropellant.PropellantStructure.Configuration;
using UnitsNet;

namespace PastyPropellant.PropellantStructure.Tests;

/// <summary>
/// The JSON contract of the configuration node: what a file may say, what it may
/// not, and what a run leaves behind before it starts.
/// </summary>
public sealed class ConfigurationJsonTests
{
    private const string MinimalJson =
        """
        {
          "oxidiserDensity": 1950,
          "binderMetalDensity": 1800,
          "oxidiserMassFraction": 0.583,
          "metalMassFraction": 0.207,
          "fractions": [
            { "massFraction": 0.18, "minSize": 1e-5, "maxSize": 5e-5 },
            { "massFraction": 0.82, "minSize": 1.13e-4, "maxSize": 1.8e-4 }
          ]
        }
        """;

    [Fact]
    public void AMinimalFileInheritsEveryHistoricalDefault()
    {
        var configuration = StructureRunConfigurationFile.Parse(MinimalJson);

        Assert.Equal(GeometricCriteria.Historical, configuration.Criteria);
        Assert.Equal(ModelCoefficients.Historical, configuration.Coefficients);
        Assert.Equal(100_000, configuration.BaseParticles);
        Assert.Equal(2, configuration.Fractions.Count);
        Assert.Equal(1e-5, configuration.Fractions[0].MinSize.Meters, 15);
    }

    /// <summary>
    /// A misspelt key that silently left its setting at the default is the exact
    /// failure this node exists to prevent, so it is an error rather than a note.
    /// </summary>
    [Fact]
    public void AMemberTheConfigurationDoesNotHaveIsAnError()
    {
        // "basePartikles" is the kind of typo that would otherwise leave
        // baseParticles quietly at its default.
        var json = MinimalJson.TrimEnd().TrimEnd('}') + ", \"basePartikles\": 500 }";

        var exception = Assert.Throws<StructureConfigurationException>(
            () => StructureRunConfigurationFile.Parse(json));

        Assert.Contains("basePartikles", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AMissingRequiredMemberIsAnError()
    {
        var json = MinimalJson.Replace("\"metalMassFraction\": 0.207,", string.Empty, StringComparison.Ordinal);

        Assert.Throws<StructureConfigurationException>(() => StructureRunConfigurationFile.Parse(json));
    }

    [Fact]
    public void MalformedJsonIsAConfigurationError()
    {
        Assert.Throws<StructureConfigurationException>(() => StructureRunConfigurationFile.Parse("{"));
    }

    [Fact]
    public void AMissingFileIsAConfigurationError()
    {
        var path = Path.Combine(Path.GetTempPath(), $"no-such-config-{Guid.NewGuid():N}.json");

        var exception = Assert.Throws<StructureConfigurationException>(() => StructureRunConfigurationFile.Read(path));

        Assert.Contains(path, exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// The trap this test exists for: <c>formsPockets</c> defaults to true, and a
    /// deserialiser that goes through the struct's parameterless constructor would
    /// turn an omitted key into false — a fraction silently taken out of pocket
    /// formation.
    /// </summary>
    [Fact]
    public void AnOmittedFormsPocketsStaysTrue()
    {
        var configuration = StructureRunConfigurationFile.Parse(MinimalJson);

        Assert.All(configuration.Fractions, fraction => Assert.True(fraction.FormsPockets));
    }

    [Fact]
    public void FormsPocketsIsCarriedWhenItIsStated()
    {
        var json = MinimalJson.Replace(
            "{ \"massFraction\": 0.18, \"minSize\": 1e-5, \"maxSize\": 5e-5 }",
            "{ \"massFraction\": 0.18, \"minSize\": 1e-5, \"maxSize\": 5e-5, \"formsPockets\": false }",
            StringComparison.Ordinal);

        var configuration = StructureRunConfigurationFile.Parse(json);

        Assert.False(configuration.Fractions[0].FormsPockets);
        Assert.True(configuration.Fractions[1].FormsPockets);
    }

    [Theory]
    [MemberData(nameof(ReferenceRuns.AllIds), MemberType = typeof(ReferenceRuns))]
    public void TheResolvedRecordRoundTripsThroughDisk(string id)
    {
        var configuration = ReferenceRuns.ById(id).ToConfiguration();
        var directory = Directory.CreateTempSubdirectory("propstruct-resolved-");
        try
        {
            var path = Path.Combine(directory.FullName, "structure_run.resolved.json");

            var written = ResolvedRunRecord.Write(configuration, path);
            var read = ResolvedRunRecord.Read(path);

            Assert.Equal(written, read);
            Assert.Equal(configuration, read.Configuration);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    /// <summary>
    /// The whole reason the record exists: the original's report carried neither
    /// <c>eta</c> nor <c>AK1..AK4</c>, so its output was not a record of its input.
    /// </summary>
    [Fact]
    public void TheResolvedRecordCarriesTheValuesTheOriginalDroppedOnTheFloor()
    {
        var directory = Directory.CreateTempSubdirectory("propstruct-resolved-");
        try
        {
            var path = Path.Combine(directory.FullName, "structure_run.resolved.json");
            var configuration = ReferenceRuns.ById("hp1").ToConfiguration() with
            {
                AgglomeratedOxideShare = 0.5,
            };

            ResolvedRunRecord.Write(configuration, path);
            var text = File.ReadAllText(path);

            Assert.Contains("\"agglomeratedOxideShare\": 0.5", text, StringComparison.Ordinal);
            Assert.Contains("\"ak1\"", text, StringComparison.Ordinal);
            Assert.Contains("\"ak2\"", text, StringComparison.Ordinal);
            Assert.Contains("\"ak3\"", text, StringComparison.Ordinal);
            Assert.Contains("\"ak4\"", text, StringComparison.Ordinal);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public void TheResolvedRecordNamesItsOwnShape()
    {
        var record = ResolvedRunRecord.Write(ReferenceRuns.ById("hp1").ToConfiguration(), path: null);

        Assert.Equal("propstruct-resolved-run/1", record.Schema);
    }

    /// <summary>
    /// Quantities go to disk as bare SI numbers. The original stored metres and
    /// printed micrometres and guessed between them with a <c>.ge. 0.1</c>
    /// heuristic; a field whose unit is fixed by its name cannot be guessed at.
    /// </summary>
    [Fact]
    public void QuantitiesAreWrittenAsBareSiNumbers()
    {
        var directory = Directory.CreateTempSubdirectory("propstruct-resolved-");
        try
        {
            var path = Path.Combine(directory.FullName, "structure_run.resolved.json");
            ResolvedRunRecord.Write(
                ReferenceRuns.ById("hp1").ToConfiguration() with
                {
                    MinimumParticleSize = Length.FromMicrometers(10),
                },
                path);

            var text = File.ReadAllText(path);

            // A bare number, not UnitsNet's {value, unit} shape - and in metres, so
            // 10 µm reads as 1e-5 rather than as 10.
            Assert.Matches(@"""minimumParticleSize"":\s*9\.99999[0-9]*E-06\s*[,}]", text);
            Assert.DoesNotContain("Micrometer", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("\"unit\"", text, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }
}
