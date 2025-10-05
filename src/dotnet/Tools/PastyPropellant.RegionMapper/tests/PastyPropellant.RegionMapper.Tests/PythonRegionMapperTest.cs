using PastyPropellant.RegionMapper.Mappers;
using UnitsNet;

namespace PastyPropellant.RegionMapper.Tests;

public class PythonRegionMapperTest
{
    public const string PyMapperDirectoryPath = "../../../../../src/python/RegionMapper";
    public const string PyMapperScriptPath = PyMapperDirectoryPath + "/src/main.py";
    public const string PyMapperOutputDirectoryPath = "output";

    private readonly PythonRegionMapper _pythonRegionMapper;

    public PythonRegionMapperTest()
    {
        var pressures = new List<Pressure> { Pressure.FromPascals(101325) };
        _pythonRegionMapper = new PythonRegionMapper(PyMapperScriptPath, PyMapperOutputDirectoryPath, pressures);
    }

    [Fact]
    public async Task MapRegionAsync_ShouldReturnSuccessOperationResult()
    {
        var propellantsFilePath = PyMapperDirectoryPath + "/data/propellants.json";
        var componentsFilePath = PyMapperDirectoryPath + "/data/components.json";

        var result = await _pythonRegionMapper.MapRegionAsync(propellantsFilePath, componentsFilePath);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task MapRegionAsync_ShouldReturnExceptionOperationResult()
    {
        var propellantsFilePath = PyMapperDirectoryPath + "/fake/propellants.json";
        var componentsFilePath = PyMapperDirectoryPath + "/fake/components.json";

        var result = await _pythonRegionMapper.MapRegionAsync(propellantsFilePath, componentsFilePath);

        Assert.False(result.IsSuccess);
    }
}
