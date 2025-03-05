using System.Collections.ObjectModel;
using PastyPropellant.ConsoleApp.Helpers;
using PastyPropellant.Core.Utils;
using PastyPropellant.ProcessHandling.Models.Events.Logs;
using UnitsNet;

namespace PastyPropellant.ConsoleApp.Tests;

public class ConstructPropellantJsonHelperTest
{
    public const string ArtifactDirectoryPath = "../../../../../artifacts/output_construct";
    public const string PyMapperScriptPath = "../../../../../src/python/RegionMapper/src/main.py";
    public const string PyThermodynamicsScriptPath = "../../../../../externals/src/python/AerospacePropellantThermodynamics/src/main.py";
    public const string PyPorosityScriptPath = "../../../../../src/python/PorosityCalculation/src/main.py";

    // Very long running test
    [Fact]
    public async Task ConstructAsync_ShouldSuccessReturnOperationResult()
    {
        // Arrange
        var propellantsFilePath = "../../../../../data/propellants.json";
        var componentsFilePath = "../../../../../src/python/RegionMapper/data/components.json";
        var combustionProductsFilePath = "../../../../../externals/src/python/AerospacePropellantThermodynamics/data/combustion_products.json";
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
