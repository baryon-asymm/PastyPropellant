using PastyPropellant.Core.Utils;
using PastyPropellant.ProcessHandling.ProcessHandlers;
using PastyPropellant.PropellantsPlotRendering.Interfaces;
using PastyPropellant.PropellantsPlotRendering.Models;

namespace PastyPropellant.PropellantsPlotRendering.Renderers;

public class PythonPlotsRenderer : IPlotsRenderer
{
    public const string PythonPath = "python3";

    public string ScriptPath { get; init; }

    public PythonPlotsRenderer(string scriptPath)
    {
        ScriptPath = scriptPath;
    }

    public async Task<OperationResult<PlotsResult>> RenderPlotsAsync(
        string propellantsFilePath)
    {
        try
        {
            return await TryRenderPlotsAsync(propellantsFilePath);
        }
        catch (Exception ex)
        {
            return new OperationResult<PlotsResult>(ex);
        }
    }

    private async Task<OperationResult<PlotsResult>> TryRenderPlotsAsync(
        string propellantsFilePath)
    {
        var operationResult = await ExecutePythonScriptAsync(propellantsFilePath);
        if (operationResult.IsSuccess == false)
            throw operationResult.Exception!;

        return new OperationResult<PlotsResult>(GetPlotsResult());
    }

    private async Task<OperationResult> ExecutePythonScriptAsync(
        string propellantsFilePath)
    {
        var command = $"{PythonPath}";
        var arguments = $"{ScriptPath} {propellantsFilePath}";
        return await ProcessHandler.RunProcessAsync(command, arguments);
    }

    private PlotsResult GetPlotsResult()
    {
        var directory = Directory.GetCurrentDirectory();
        if (directory == null)
            throw new InvalidOperationException("Directory is null.");
        
        var thermalConductivityPlotPath = Path.Combine(directory, "lambda_gas_plot.png");
        var averageMolecularWeightPlotPath = Path.Combine(directory, "average_molar_mass_plot.png");
        var constantVolumeSpecificHeatPlotPath = Path.Combine(directory, "c_volume_plot.png");
        var temperaturePlotPath = Path.Combine(directory, "temperatures_plot.png");
        var agglomerationFactorPlotPath = Path.Combine(directory, "agglomeration_fraction_plot.png");
        var skeletonSurfaceFactorPlotPath = Path.Combine(directory, "skeleton_surface_fraction_plot.png");
        
        return new PlotsResult(
            thermalConductivityPlotPath,
            averageMolecularWeightPlotPath,
            constantVolumeSpecificHeatPlotPath,
            temperaturePlotPath,
            agglomerationFactorPlotPath,
            skeletonSurfaceFactorPlotPath
        );
    }
}
