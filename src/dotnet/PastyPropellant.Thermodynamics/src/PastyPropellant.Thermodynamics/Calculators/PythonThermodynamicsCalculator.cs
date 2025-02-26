using PastyPropellant.Core.Utils;
using PastyPropellant.ProcessHandling.ProcessHandlers;
using PastyPropellant.Thermodynamics.Interfaces;
using PastyPropellant.Thermodynamics.Models;
using UnitsNet;

namespace PastyPropellant.Thermodynamics.Calculators;

public class PythonThermodynamicsCalculator : IThermodynamicsCalculator
{
    public const string PythonPath = "python3";

    public const string OutputFileExtension = ".tdc.json";

    public string ScriptPath { get; init; }

    public PythonThermodynamicsCalculator(string scriptPath)
    {
        ScriptPath = scriptPath;
    }

    public async Task<OperationResult<PropellantThermodynamics>> CalculateThermodynamicPropertiesAsync(
        string propellantFilePath, string combustionProductsFilePath, Pressure pressure)
    {
        try
        {
            return await TryCalculateThermodynamicPropertiesAsync(propellantFilePath, combustionProductsFilePath, pressure);
        }
        catch (Exception ex)
        {
            return new OperationResult<PropellantThermodynamics>(ex);
        }
    }

    private async Task<OperationResult<PropellantThermodynamics>> TryCalculateThermodynamicPropertiesAsync(
        string propellantFilePath, string combustionProductsFilePath, Pressure pressure)
    {
        var outputFilePath = Path.ChangeExtension(propellantFilePath, OutputFileExtension);
        var operationResult = await ExecutePythonScriptAsync(
            propellantFilePath, combustionProductsFilePath, pressure, outputFilePath);
        if (operationResult.IsSuccess == false)
            throw operationResult.Exception!;
        return new OperationResult<PropellantThermodynamics>(new PropellantThermodynamics(outputFilePath));
    }

    private Task<OperationResult> ExecutePythonScriptAsync(
        string propellantFilePath, string combustionProductsFilePath, Pressure pressure, string outputFilePath)
    {
        var command = $"{PythonPath}";
        var arguments = $"{ScriptPath} --propellant {propellantFilePath} --combustion-products {combustionProductsFilePath} --pressure {pressure.Pascals} --output-json {outputFilePath}";
        return ProcessHandler.RunProcessAsync(command, arguments);
    }
}
