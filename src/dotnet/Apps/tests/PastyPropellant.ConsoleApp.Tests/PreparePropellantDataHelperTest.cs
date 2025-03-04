using System.Collections.ObjectModel;
using PastyPropellant.ConsoleApp.Helpers;
using UnitsNet;

namespace PastyPropellant.ConsoleApp.Tests;

public class PreparePropellantDataHelperTest
{
    // Very long running test
    [Fact]
    public async Task PrepareAsync_ShouldSuccessReturnOperationResult()
    {
        // Arrange
        var artifactDirectoryPath = "../../../../../artifacts/output_prepare";
        var pyMapperScriptPath = "../../../../../src/python/RegionMapper/src/main.py";
        var pyThermodynamicsScriptPath = "../../../../../externals/src/python/AerospacePropellantThermodynamics/src/main.py";
        var pressures = GetPressures();
        var preparePropellantHelper = new PreparePropellantDataHelper(
            artifactDirectoryPath, pyMapperScriptPath, pyThermodynamicsScriptPath, pressures);

        var propellantsFilePath = "../../../../../src/python/RegionMapper/data/propellants.json";
        var componentsFilePath = "../../../../../src/python/RegionMapper/data/components.json";
        var combustionProductsFilePath = "../../../../../externals/src/python/AerospacePropellantThermodynamics/data/combustion_products.json";

        // Act
        var result = await preparePropellantHelper.PrepareAsync(
            propellantsFilePath, componentsFilePath, combustionProductsFilePath);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task PrepareAsync_ShouldExceptionReturnOperationResult()
    {
        // Arrange
        var artifactDirectoryPath = "../../../../../artifacts/output_prepare";
        var pyMapperScriptPath = "../../../../../src/python/RegionMapper/src/main.py";
        var pyThermodynamicsScriptPath = "../../../../../externals/src/python/AerospacePropellantThermodynamics/src/main.py";
        var pressures = GetPressures();
        var preparePropellantHelper = new PreparePropellantDataHelper(
            artifactDirectoryPath, pyMapperScriptPath, pyThermodynamicsScriptPath, pressures);

        var propellantsFilePath = "../../../../../src/python/RegionMapper/fake/propellants.json";
        var componentsFilePath = "../../../../../src/python/RegionMapper/fake/components.json";
        var combustionProductsFilePath = "../../../../../externals/src/python/AerospacePropellantThermodynamics/fake/combustion_products.json";

        // Act
        var result = await preparePropellantHelper.PrepareAsync(
            propellantsFilePath, componentsFilePath, combustionProductsFilePath);

        // Assert
        Assert.False(result.IsSuccess);
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
