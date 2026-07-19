using PastyPropellant.Core.Utils;
using PastyPropellant.Interop;
using PastyPropellant.Thermodynamics.Interfaces;
using PastyPropellant.Thermodynamics.Models;
using UnitsNet;

namespace PastyPropellant.Thermodynamics.Calculators;

public class PythonThermodynamicsCalculator : IThermodynamicsCalculator
{
    /// <summary>The interpreter that will be launched; see <see cref="PythonRuntime.InterpreterPath"/>.</summary>
    public static string PythonPath => PythonRuntime.InterpreterPath;

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
        return PythonRuntime.RunScriptAsync(
            ScriptPath,
            "--propellant", propellantFilePath,
            "--combustion-products", combustionProductsFilePath,
            "--pressure", pressure.Pascals.ToString(),
            "--output-json", outputFilePath);
    }
}
