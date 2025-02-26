using PastyPropellant.ConsoleApp.Interfaces;
using PastyPropellant.ConsoleApp.Models;
using PastyPropellant.ConsoleApp.Utils.FileReaders;
using PastyPropellant.Core.Utils;

namespace PastyPropellant.ConsoleApp.Helpers.ParametricModel;

public class OptimizationTicketsHelper
{
    private readonly IModelReader<List<ParametricModelOptimizationTicket>> _modelReader;

    public OptimizationTicketsHelper(string filePath)
    {
        _modelReader = new BaseFileModelReader<List<ParametricModelOptimizationTicket>>(filePath);
    }

    public virtual Task<OperationResult<List<ParametricModelOptimizationTicket>>> GetAllAsync()
    {
        return _modelReader.ReadAllAsync();
    }
}
