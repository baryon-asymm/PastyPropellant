using PastyPropellant.Core.Utils;
using PastyPropellant.PorosityCalculation.Interfaces;
using PastyPropellant.PorosityCalculation.Models;
using PastyPropellant.ProcessHandling.ProcessHandlers;

namespace PastyPropellant.PorosityCalculation.Calculators;

public class PythonPorosityCalculator : IPorosityCalculator
{
    public const string PythonPath = "python3";

    public string ScriptPath { get; init; }

    public PythonPorosityCalculator(string scriptPath)
    {
        ScriptPath = scriptPath;
    }

    public async Task<OperationResult<PorosityPropellant>> CalculatePorosityAsync(
        string propellantsFilePath, string propellantName, string regionFilePath)
    {
        try
        {
            return await TryCalculatePorosityAsync(propellantsFilePath, propellantName, regionFilePath);
        }
        catch (Exception ex)
        {
            return new OperationResult<PorosityPropellant>(ex);
        }
    }

    private async Task<OperationResult<PorosityPropellant>> TryCalculatePorosityAsync(
        string propellantsFilePath, string propellantName, string regionFilePath)
    {
        var operationResult = await ExecutePythonScriptAsync(
            propellantsFilePath, propellantName, regionFilePath);
        if (operationResult.IsSuccess == false)
            throw operationResult.Exception!;

        return GetPorosityPropellant(propellantName, regionFilePath);
    }

    private async Task<OperationResult> ExecutePythonScriptAsync(
        string propellantsFilePath, string propellantName, string regionFilePath)
    {
        var command = $"{PythonPath}";
        var arguments = $"{ScriptPath} --propellants-file {propellantsFilePath} --propellant-name {propellantName} --region-file {regionFilePath}";
        return await ProcessHandler.RunProcessAsync(command, arguments);
    }

    private OperationResult<PorosityPropellant> GetPorosityPropellant(string propellantName, string regionFilePath)
    {
        var directory = Path.GetDirectoryName(regionFilePath);
        if (directory == null)
            throw new InvalidOperationException("Directory is null.");
        
        var porosityFilePath = Path.Combine(directory, "porosity.json");
        var porosityPropellant = new PorosityPropellant(propellantName, porosityFilePath);
        
        return new OperationResult<PorosityPropellant>(porosityPropellant);
    }
}
