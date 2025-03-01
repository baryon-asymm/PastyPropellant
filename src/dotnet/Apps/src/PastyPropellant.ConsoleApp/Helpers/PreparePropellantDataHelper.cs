using System.Collections.ObjectModel;
using PastyPropellant.Core.Models.Events.Logs;
using PastyPropellant.Core.Utils;
using PastyPropellant.ProcessHandling.Models.Events.Logs;
using PastyPropellant.RegionMapper.Mappers;
using PastyPropellant.RegionMapper.Models;
using PastyPropellant.Thermodynamics.Calculators;
using UnitsNet;

namespace PastyPropellant.ConsoleApp.Helpers;

public record PreparedPropellantData(
    string Name,
    ReadOnlyCollection<PressureFrameThermodynamics> PressureFrameThermodynamics
);

public record PressureFrameThermodynamics(
    Pressure Pressure,
    string InterPocketFilePath,
    string PocketWithoutSkeletonFilePath,
    string PocketWithSkeletonFilePath,
    string DiffusionFilePath
);

public class PreparePropellantDataHelper
{
    private readonly object _lock = new();
    private readonly PythonRegionMapper _pythonRegionMapper;
    private readonly PythonThermodynamicsCalculator _pythonThermodynamicsCalculator;

    public PreparePropellantDataHelper(
        string artifactDirectoryPath,
        string pyMapperScriptPath,
        string pyThermodynamicsScriptPath,
        IEnumerable<Pressure> pressures)
    {
        ArgumentNullException.ThrowIfNull(pressures, nameof(pressures));

        _pythonRegionMapper = new PythonRegionMapper(pyMapperScriptPath, artifactDirectoryPath, pressures);
        _pythonThermodynamicsCalculator = new PythonThermodynamicsCalculator(pyThermodynamicsScriptPath);
    }

    public async Task<OperationResult<ReadOnlyCollection<PreparedPropellantData>>> PrepareAsync(
        string propellantsFilePath, string componentsFilePath, string combustionProductsFilePath)
    {
        try
        {
            return await TryPrepareAsync(propellantsFilePath, componentsFilePath, combustionProductsFilePath);
        }
        catch (Exception ex)
        {
            return new OperationResult<ReadOnlyCollection<PreparedPropellantData>>(ex);
        }
    }

    private async Task<OperationResult<ReadOnlyCollection<PreparedPropellantData>>> TryPrepareAsync(
        string propellantsFilePath, string componentsFilePath, string combustionProductsFilePath)
    {
        EventBus<InfoLogEvent>.Publish(new InfoLogEvent("Mapping regions...", nameof(PreparePropellantDataHelper)));
        EventBus<ProcessInfoLogEvent>.Subscribe(RegionMapperProcessInfoLogEventHandler);

        var regionMapperResult = await _pythonRegionMapper.MapRegionAsync(propellantsFilePath, componentsFilePath);
        if (regionMapperResult.IsSuccess == false)
            return new OperationResult<ReadOnlyCollection<PreparedPropellantData>>(regionMapperResult.Exception);
        
        EventBus<ProcessInfoLogEvent>.Unsubscribe(RegionMapperProcessInfoLogEventHandler);
        EventBus<InfoLogEvent>.Publish(new InfoLogEvent("Calculating thermodynamic properties...", nameof(PreparePropellantDataHelper)));
        
        var regionMappeds = regionMapperResult.Value!;
        var thermodynamicsCalculatorResult = RunThermodynamicsCalculator(regionMappeds, combustionProductsFilePath);
        return thermodynamicsCalculatorResult;
    }

    private OperationResult<ReadOnlyCollection<PreparedPropellantData>> RunThermodynamicsCalculator(
        IEnumerable<PropellantRegionMapped> regionMappeds,
        string combustionProductsFilePath)
    {
        var propellants = new List<PreparedPropellantData>();
        foreach (var propellant in regionMappeds)
        {
            int pressureFramesCounter = 0;
            var pressureFrames = new PressureFrameThermodynamics[propellant.PressureFrames.Count];
            var dictPressureFrames = new Dictionary<string, int>();
            for (int i = 0; i < propellant.PressureFrames.Count; i++)
                dictPressureFrames.Add(propellant.PressureFrames[i].Pressure.Pascals.ToString(), i);
            propellant.PressureFrames.AsParallel().ForAll(pressureFrame =>
            {
                var interPocketResult = _pythonThermodynamicsCalculator.CalculateThermodynamicPropertiesAsync(
                    pressureFrame.InterPocketFilePath, combustionProductsFilePath, pressureFrame.Pressure);
                var pocketWithoutSkeletonResult = _pythonThermodynamicsCalculator.CalculateThermodynamicPropertiesAsync(
                    pressureFrame.PocketWithoutSkeletonFilePath, combustionProductsFilePath, pressureFrame.Pressure);
                var pocketWithSkeletonResult = _pythonThermodynamicsCalculator.CalculateThermodynamicPropertiesAsync(
                    pressureFrame.PocketWithSkeletonFilePath, combustionProductsFilePath, pressureFrame.Pressure);
                var diffusionResult = _pythonThermodynamicsCalculator.CalculateThermodynamicPropertiesAsync(
                    pressureFrame.DiffusionFilePath, combustionProductsFilePath, pressureFrame.Pressure);
                
                Task.WaitAll(interPocketResult, pocketWithoutSkeletonResult, pocketWithSkeletonResult, diffusionResult);

                if (interPocketResult.Result.IsSuccess == false ||
                    pocketWithoutSkeletonResult.Result.IsSuccess == false ||
                    pocketWithSkeletonResult.Result.IsSuccess == false ||
                    diffusionResult.Result.IsSuccess == false)
                {
                    throw new InvalidOperationException("Failed to calculate thermodynamic properties.");
                }

                pressureFrames[dictPressureFrames[pressureFrame.Pressure.Pascals.ToString()]] = new PressureFrameThermodynamics(
                    pressureFrame.Pressure,
                    interPocketResult.Result.Value!.OutputFilePath,
                    pocketWithoutSkeletonResult.Result.Value!.OutputFilePath,
                    pocketWithSkeletonResult.Result.Value!.OutputFilePath,
                    diffusionResult.Result.Value!.OutputFilePath
                );

                var localCounter = Interlocked.Increment(ref pressureFramesCounter);
                var workProgress = (double)localCounter / propellant.PressureFrames.Count * 100;
                PublishThermodynamicsInfoLog($"Calculated thermodynamic properties for pressure frame {pressureFrame.Pressure}.\n" +
                                             $"Inter-pocket: {interPocketResult.Result.Value!.OutputFilePath}\n" +
                                             $"Pocket without skeleton: {pocketWithoutSkeletonResult.Result.Value!.OutputFilePath}\n" +
                                             $"Pocket with skeleton: {pocketWithSkeletonResult.Result.Value!.OutputFilePath}\n" +
                                             $"Diffusion: {diffusionResult.Result.Value!.OutputFilePath}\n" +
                                             $"Progress for a <{propellant.Name}> propellant: {workProgress:F2}%");
            });

            propellants.Add(new PreparedPropellantData(propellant.Name, new ReadOnlyCollection<PressureFrameThermodynamics>(pressureFrames)));
        }

        return new OperationResult<ReadOnlyCollection<PreparedPropellantData>>(
            new ReadOnlyCollection<PreparedPropellantData>(propellants));
    }

    private void RegionMapperProcessInfoLogEventHandler(ProcessInfoLogEvent e)
    {
        EventBus<InfoLogEvent>.Publish(new InfoLogEvent(e.Message, nameof(PythonRegionMapper)));
    }

    private void PublishThermodynamicsInfoLog(string message)
    {
        lock (_lock)
            EventBus<InfoLogEvent>.Publish(new InfoLogEvent(message, nameof(PythonThermodynamicsCalculator)));
    }
}
