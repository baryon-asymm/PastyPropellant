using PastyPropellant.Core.Utils;
using PastyPropellant.Interop;
using PastyPropellant.PorosityCalculation.Interfaces;
using PastyPropellant.PorosityCalculation.Models;

namespace PastyPropellant.PorosityCalculation.Calculators;

public class PythonPorosityCalculator : IPorosityCalculator
{
    /// <summary>The interpreter that will be launched; see <see cref="PythonRuntime.InterpreterPath"/>.</summary>
    public static string PythonPath => PythonRuntime.InterpreterPath;

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

    private Task<OperationResult> ExecutePythonScriptAsync(
        string propellantsFilePath, string propellantName, string regionFilePath)
    {
        return PythonRuntime.RunScriptAsync(
            ScriptPath,
            "--propellants-file", propellantsFilePath,
            "--propellant-name", propellantName,
            "--region-file", regionFilePath);
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
