using PastyPropellant.Thermodynamics.Calculators;
using UnitsNet;

namespace PastyPropellant.Thermodynamics.Tests;

public class PythonThermodynamicsCalculatorTest
{
    public const string PyCalculatorDirectoryPath = "../../../../../externals/src/python/AerospacePropellantThermodynamics";
    public const string PyCalculatorScriptPath = PyCalculatorDirectoryPath + "/src/main.py";

    private readonly PythonThermodynamicsCalculator _pythonThermodynamicsCalculator;

    public PythonThermodynamicsCalculatorTest()
    {
        _pythonThermodynamicsCalculator = new PythonThermodynamicsCalculator(PyCalculatorScriptPath);
    }

    // Long running test
    [Fact]
    public async Task CalculateThermodynamicPropertiesAsync_ShouldSuccessReturnPropellantThermodynamics()
    {
        // Arrange
        var propellantFilePath = PyCalculatorDirectoryPath + "/data/propellant.json";
        var combustionProductsFilePath = PyCalculatorDirectoryPath + "/data/combustion_products.json";
        var pressure = Pressure.FromAtmospheres(40);

        // Act
        var result = await _pythonThermodynamicsCalculator.CalculateThermodynamicPropertiesAsync(
            propellantFilePath, combustionProductsFilePath, pressure);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        
        // Clean up
        File.Delete(result.Value.OutputFilePath);
    }

    [Fact]
    public async Task CalculateThermodynamicPropertiesAsync_ShouldReturnExceptionOperationResult()
    {
        // Arrange
        var propellantFilePath = PyCalculatorDirectoryPath + "/fake/propellant.json";
        var combustionProductsFilePath = PyCalculatorDirectoryPath + "/fake/combustion_products.json";
        var pressure = Pressure.FromAtmospheres(40);

        // Act
        var result = await _pythonThermodynamicsCalculator.CalculateThermodynamicPropertiesAsync(
            propellantFilePath, combustionProductsFilePath, pressure);

        // Assert
        Assert.False(result.IsSuccess);
    }
}
