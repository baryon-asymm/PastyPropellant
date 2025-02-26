using ParametricCombustionModel.Core.Models;
using PastyPropellant.ConsoleApp.Interfaces;
using PastyPropellant.ConsoleApp.Utils.FileReaders;
using PastyPropellant.Core.Utils;

namespace PastyPropellant.ConsoleApp.Helpers.ParametricModel;

public class PropellantsHelper
{
    private readonly IModelReader<List<Propellant>> _modelReader;

    public PropellantsHelper(string filePath)
    {
        _modelReader = new BaseFileModelReader<List<Propellant>>(filePath);
    }

    public virtual Task<OperationResult<List<Propellant>>> GetAllAsync()
    {
        return _modelReader.ReadAllAsync();
    }
}
