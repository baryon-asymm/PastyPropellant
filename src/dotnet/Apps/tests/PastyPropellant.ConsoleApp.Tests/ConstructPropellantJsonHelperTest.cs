using System.Collections.ObjectModel;
using PastyPropellant.ConsoleApp.Helpers;
using PastyPropellant.Core.Utils;
using UnitsNet;

namespace PastyPropellant.ConsoleApp.Tests;

public class ConstructPropellantJsonHelperTest
{
    // Very long running test
    [Fact]
    public async Task ConstructAsync_ShouldSuccessReturnOperationResult()
    {
        // Arrange
        var artifactDirectoryPath = "../../../../../artifacts/output_construction";
        var propellantsFilePath = "../../../../../data/propellants.json";
        var componentsFilePath = "../../../../../data/propellant_components.json";
        var combustionProductsFilePath = "../../../../../externals/src/python/AerospacePropellantThermodynamics/data/combustion_products.json";
        var outputPropellantsFilePath = Path.Combine(artifactDirectoryPath, "propellants.json");
        var preparedDataResult = await GetPreparedPropellantDataAsync(
            artifactDirectoryPath, propellantsFilePath, componentsFilePath, combustionProductsFilePath);
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
        var pyMapperScriptPath = "../../../../../src/python/RegionMapper/src/main.py";
        var pyThermodynamicsScriptPath = "../../../../../externals/src/python/AerospacePropellantThermodynamics/src/main.py";
        var pressures = GetPressures();
        var preparePropellantHelper = new PreparePropellantDataHelper(
            artifactDirectoryPath, pyMapperScriptPath, pyThermodynamicsScriptPath, pressures);

        var result = await preparePropellantHelper.PrepareAsync(
            propellantsFilePath, componentsFilePath, combustionProductsFilePath);
        
        return result;
    }

    private ReadOnlyCollection<Pressure> GetPressures()
    {
        var maxPressure = Pressure.FromMegapascals(6.5);
        var minPressure = Pressure.FromMegapascals(1);
        const int pressurePoints = 20;
        return new ReadOnlyCollection<Pressure>(Enumerable.Range(0, pressurePoints)
            .Select(x => minPressure + (maxPressure - minPressure) / (pressurePoints - 1) * x)
            .ToArray());
    }
}
