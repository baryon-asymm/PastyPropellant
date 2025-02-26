using System.Collections.ObjectModel;
using System.Text.Json;
using PastyPropellant.Core.Utils;
using PastyPropellant.ProcessHandling.ProcessHandlers;
using PastyPropellant.RegionMapper.Interfaces;
using PastyPropellant.RegionMapper.Models;
using UnitsNet;

namespace PastyPropellant.RegionMapper.Mappers;

public class PythonRegionMapper : IRegionMapper
{
    public const string PythonPath = "python3";

    public string ScriptPath { get; init; }

    public string OutputDirectoryPath { get; init; }

    public ReadOnlyCollection<Pressure> Pressures { get; init; }

    public PythonRegionMapper(
        string scriptPath, string outputDirectoryPath, IEnumerable<Pressure> pressures)
    {
        ScriptPath = scriptPath;
        OutputDirectoryPath = outputDirectoryPath;
        Pressures = pressures.ToList().AsReadOnly();
    }

    public async Task<OperationResult<ReadOnlyCollection<PropellantRegionMapped>>> MapRegionAsync(
        string propellantsFilePath, string componentsFilePath)
    {
        try
        {
            return await TryMapRegionAsync(propellantsFilePath, componentsFilePath);
        }
        catch (Exception ex)
        {
            return new OperationResult<ReadOnlyCollection<PropellantRegionMapped>>(ex);
        }
    }

    private async Task<OperationResult<ReadOnlyCollection<PropellantRegionMapped>>> TryMapRegionAsync(
        string propellantsFilePath, string componentsFilePath)
    {
        var operationResult = await ExecutePythonScriptAsync(
            propellantsFilePath, componentsFilePath);
        if (operationResult.IsSuccess == false)
            throw operationResult.Exception!;

        return await GetPropellantRegionMapped(propellantsFilePath);
    }

    private async Task<OperationResult> ExecutePythonScriptAsync(
        string propellantsFilePath, string componentsFilePath)
    {
        foreach (var pressure in Pressures)
        {
            var command = $"{PythonPath}";
            var outputDirectoryPath = Path.Combine(OutputDirectoryPath, pressure.Pascals.ToString());
            var arguments = $"{ScriptPath} --propellants {propellantsFilePath} --components {componentsFilePath} --pressure {pressure.Pascals} --output-dir {outputDirectoryPath}";
            var operationResult = await ProcessHandler.RunProcessAsync(command, arguments);
            if (operationResult.IsSuccess == false)
            {
                return operationResult;
            }
        }

        return new OperationResult();
    }

    private async Task<OperationResult<ReadOnlyCollection<PropellantRegionMapped>>> GetPropellantRegionMapped(
        string propellantsFilePath)
    {
        var propellants = await GetPropellantsAsync(propellantsFilePath);

        var mappedPropellants = new List<PropellantRegionMapped>();
        foreach (var propellant in propellants)
        {
            var pressureFrames = new List<PressureFrame>();
            foreach (var pressure in Pressures)
            {
                var pressurePath = Path.Combine(OutputDirectoryPath, pressure.Pascals.ToString());
                var pressureFrame = new PressureFrame(
                    pressure,
                    Path.Combine(pressurePath, propellant.Name, "inter_pocket.json"),
                    Path.Combine(pressurePath, propellant.Name, "pocket_without_skeleton.json"),
                    Path.Combine(pressurePath, propellant.Name, "pocket_with_skeleton.json"),
                    Path.Combine(pressurePath, propellant.Name, "diffusion.json"));
                pressureFrames.Add(pressureFrame);
            }

            var propellantRegionMapped = new PropellantRegionMapped(propellant.Name, pressureFrames.AsReadOnly());
            mappedPropellants.Add(propellantRegionMapped);
        }

        return new OperationResult<ReadOnlyCollection<PropellantRegionMapped>>(mappedPropellants.AsReadOnly());
    }

    private async Task<List<Propellant>> GetPropellantsAsync(string propellantsFilePath)
    {
        using var fileStream = new FileStream(
            propellantsFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var propellants = await JsonSerializer.DeserializeAsync<List<Propellant>>(fileStream);
        return propellants ?? throw new InvalidOperationException("Failed to deserialize propellants from file.");
    }
}
