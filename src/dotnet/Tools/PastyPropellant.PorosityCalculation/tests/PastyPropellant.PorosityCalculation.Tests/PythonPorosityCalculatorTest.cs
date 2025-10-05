using PastyPropellant.PorosityCalculation.Calculators;

namespace PastyPropellant.PorosityCalculation.Tests;

public class PythonPorosityCalculatorTest
{
    public const string PyCalculatorDirectoryPath = "../../../../../src/python/PorosityCalculation";
    public const string PyCalculatorScriptPath = PyCalculatorDirectoryPath + "/src/main.py";

    private readonly PythonPorosityCalculator _calculator;

    public PythonPorosityCalculatorTest()
    {
        _calculator = new PythonPorosityCalculator(PyCalculatorScriptPath);
    }

    [Fact]
    public async Task CalculatePorosityAsync_ShouldReturnPorosityPropellant()
    {
        // Arrange
        var propellantsFilePath = Path.GetFullPath("propellants.json");
        var propellantName = "Bas_1";
        var regionFilePath = Path.GetFullPath("pocket_without_skeleton.json");
        
        // Act
        var result = await _calculator.CalculatePorosityAsync(
            propellantsFilePath, propellantName, regionFilePath);
        
        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
    }

    [Fact]
    public async Task CalculatePorosityAsync_ShouldReturnExceptionOperationResult()
    {
        // Arrange
        var propellantsFilePath = Path.GetFullPath("fake/propellants.json");
        var propellantName = "Bas_1";
        var regionFilePath = Path.GetFullPath("fake/pocket_without_skeleton.json");
        
        // Act
        var result = await _calculator.CalculatePorosityAsync(
            propellantsFilePath, propellantName, regionFilePath);
        
        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Exception);
    }
}
