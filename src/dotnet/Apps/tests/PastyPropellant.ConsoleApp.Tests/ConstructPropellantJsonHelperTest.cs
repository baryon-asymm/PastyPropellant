using System.Collections.ObjectModel;
using PastyPropellant.ConsoleApp.Helpers;
using PastyPropellant.Interop;
using PastyPropellant.Core.Utils;
using PastyPropellant.ProcessHandling.Models.Events.Logs;
using UnitsNet;

namespace PastyPropellant.ConsoleApp.Tests;

public class ConstructPropellantJsonHelperTest
{
    public static readonly string ArtifactDirectoryPath = PythonRuntime.ResolveRepositoryPath("artifacts/output_construct");
    public static readonly string PyMapperScriptPath = PythonRuntime.ResolveRepositoryPath("src/python/RegionMapper/src/main.py");
    public static readonly string PyThermodynamicsScriptPath = PythonRuntime.ResolveRepositoryPath("externals/src/python/AerospacePropellantThermodynamics/src/main.py");
    public static readonly string PyPorosityScriptPath = PythonRuntime.ResolveRepositoryPath("src/python/PorosityCalculation/src/main.py");

    // Very long running test
    [Trait("Category", "LongRunning")]
    [Fact]
    public async Task ConstructAsync_ShouldSuccessReturnOperationResult()
    {
        // Arrange
        var propellantsFilePath = PythonRuntime.ResolveRepositoryPath("data/propellants.json");
        var componentsFilePath = PythonRuntime.ResolveRepositoryPath("src/python/RegionMapper/data/components.json");
        var combustionProductsFilePath = PythonRuntime.ResolveRepositoryPath("externals/src/python/AerospacePropellantThermodynamics/data/combustion_products.json");
        var outputPropellantsFilePath = Path.Combine(ArtifactDirectoryPath, "propellants.json");
        var preparedDataResult = await GetPreparedPropellantDataAsync(
            ArtifactDirectoryPath, propellantsFilePath, componentsFilePath, combustionProductsFilePath);
        Assert.True(preparedDataResult.IsSuccess);
        var constructPropellantJsonHelper = new ConstructPropellantJsonHelper(propellantsFilePath, preparedDataResult.Value!);

        // Act
        var result = await constructPropellantJsonHelper.ConstructAsync(outputPropellantsFilePath);

        // Assert
        Assert.True(result.IsSuccess);
    }

    private async Task<OperationResult<ReadOnlyCollection<PreparedPropellantData>>> GetPreparedPropellantDataAsync(
        string artifactDirectoryPath, string propellantsFilePath, string componentsFilePath, string combustionProductsFilePath)
    {
        var pressures = GetPressures();
        var preparePropellantHelper = new PreparePropellantDataHelper(
            artifactDirectoryPath, PyMapperScriptPath, PyThermodynamicsScriptPath, PyPorosityScriptPath, pressures);

        var result = await preparePropellantHelper.PrepareAsync(
            propellantsFilePath, componentsFilePath, combustionProductsFilePath);
        
        return result;
    }

    private ReadOnlyCollection<Pressure> GetPressures()
    {
        var maxPressure = Pressure.FromMegapascals(6.5);
        var minPressure = Pressure.FromMegapascals(1);
        const int pressurePoints = 2;
        return new ReadOnlyCollection<Pressure>(Enumerable.Range(0, pressurePoints)
            .Select(x => minPressure + (maxPressure - minPressure) / (pressurePoints - 1) * x)
            .ToArray());
    }
}
