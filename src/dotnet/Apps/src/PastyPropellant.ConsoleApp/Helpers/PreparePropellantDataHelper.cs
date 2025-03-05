using System.Collections.ObjectModel;
using PastyPropellant.Core.Models.Events.Logs;
using PastyPropellant.Core.Utils;
using PastyPropellant.PorosityCalculation.Calculators;
using PastyPropellant.PorosityCalculation.Models;
using PastyPropellant.ProcessHandling.Models.Events.Logs;
using PastyPropellant.RegionMapper.Mappers;
using PastyPropellant.RegionMapper.Models;
using PastyPropellant.Thermodynamics.Calculators;
using UnitsNet;

namespace PastyPropellant.ConsoleApp.Helpers;

public record PreparedPropellantData(
    string Name,
    ReadOnlyCollection<PressureFrameThermodynamics> PressureFrameThermodynamics,
    ReadOnlyCollection<PressureFramePorosity> PorosityPropellants
);

public record PorosityPropellantResult(
    string Name,
    ReadOnlyCollection<PressureFramePorosity> PressureFramePorosities
);

public record ThermodynamicsPropellantResult(
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

public record PressureFramePorosity(
    Pressure Pressure,
    string PorosityFilePath
);

public class PreparePropellantDataHelper
{
    private readonly object _lock = new();
    private readonly PythonRegionMapper _pythonRegionMapper;
    private readonly PythonThermodynamicsCalculator _pythonThermodynamicsCalculator;
    private readonly PythonPorosityCalculator _pythonPorosityCalculator;

    public PreparePropellantDataHelper(
        string artifactDirectoryPath,
        string pyMapperScriptPath,
        string pyThermodynamicsScriptPath,
        string pyPorosityScriptPath,
        IEnumerable<Pressure> pressures)
    {
        ArgumentNullException.ThrowIfNull(pressures, nameof(pressures));

        _pythonRegionMapper = new PythonRegionMapper(pyMapperScriptPath, artifactDirectoryPath, pressures);
        _pythonThermodynamicsCalculator = new PythonThermodynamicsCalculator(pyThermodynamicsScriptPath);
        _pythonPorosityCalculator = new PythonPorosityCalculator(pyPorosityScriptPath);
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

        EventBus<InfoLogEvent>.Publish(new InfoLogEvent("Calculating porosity...", nameof(PreparePropellantDataHelper)));
        EventBus<ProcessInfoLogEvent>.Subscribe(PorosityProcessInfoLogEventHandler);

        var regionMappeds = regionMapperResult.Value!;
        var porosityCalculatorResult = await RunPorosityCalculator(propellantsFilePath, regionMappeds);
        if (porosityCalculatorResult.IsSuccess == false)
            return new OperationResult<ReadOnlyCollection<PreparedPropellantData>>(porosityCalculatorResult.Exception);
        
        EventBus<ProcessInfoLogEvent>.Unsubscribe(PorosityProcessInfoLogEventHandler);

        EventBus<InfoLogEvent>.Publish(new InfoLogEvent("Calculating thermodynamic properties...", nameof(PreparePropellantDataHelper)));
        
        var thermodynamicsCalculatorResult = RunThermodynamicsCalculator(regionMappeds, combustionProductsFilePath);
        
        if (thermodynamicsCalculatorResult.IsSuccess == false)
            return new OperationResult<ReadOnlyCollection<PreparedPropellantData>>(thermodynamicsCalculatorResult.Exception);

        var preparedPropellantDatas = GetPreparedPropellantDataCollection(
            regionMappeds, thermodynamicsCalculatorResult.Value!, porosityCalculatorResult.Value!);

        return new OperationResult<ReadOnlyCollection<PreparedPropellantData>>(
            new ReadOnlyCollection<PreparedPropellantData>(preparedPropellantDatas));
    }

    private OperationResult<ReadOnlyCollection<ThermodynamicsPropellantResult>> RunThermodynamicsCalculator(
        IEnumerable<PropellantRegionMapped> regionMappeds,
        string combustionProductsFilePath)
    {
        var propellants = new List<ThermodynamicsPropellantResult>();
        foreach (var propellant in regionMappeds)
        {
            int pressureFramesCounter = 0;
            var pressureFrames = new PressureFrameThermodynamics[propellant.PressureFrames.Count];
            var dictPressureFrames = new Dictionary<string, int>();
            for (int i = 0; i < propellant.PressureFrames.Count; i++)
                dictPressureFrames.Add(propellant.PressureFrames[i].Pressure.Pascals.ToString(), i);
            
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 4 };

            Parallel.ForEach(propellant.PressureFrames, parallelOptions, pressureFrame =>
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

            propellants.Add(new ThermodynamicsPropellantResult(
                propellant.Name, new ReadOnlyCollection<PressureFrameThermodynamics>(pressureFrames)));
        }

        return new OperationResult<ReadOnlyCollection<ThermodynamicsPropellantResult>>(
            new ReadOnlyCollection<ThermodynamicsPropellantResult>(propellants));
    }

    private async Task<OperationResult<ReadOnlyCollection<PorosityPropellantResult>>> RunPorosityCalculator(
        string propellantsFilePath,
        IEnumerable<PropellantRegionMapped> regionMappeds)
    {
        var propellants = new List<PorosityPropellantResult>();
        foreach (var propellant in regionMappeds)
        {
            var porosityPropellants = new List<PressureFramePorosity>();
            foreach (var pressureFrame in propellant.PressureFrames)
            {
                var porosityResult = await _pythonPorosityCalculator.CalculatePorosityAsync(
                    propellantsFilePath, propellant.Name, pressureFrame.PocketWithoutSkeletonFilePath);

                if (porosityResult.IsSuccess == false)
                    throw new InvalidOperationException("Failed to calculate porosity.");
                
                porosityPropellants.Add(new PressureFramePorosity(pressureFrame.Pressure, porosityResult.Value!.FilePath));
            }

            propellants.Add(new PorosityPropellantResult(propellant.Name, new ReadOnlyCollection<PressureFramePorosity>(porosityPropellants)));
        }

        return new OperationResult<ReadOnlyCollection<PorosityPropellantResult>>(
            new ReadOnlyCollection<PorosityPropellantResult>(propellants));
    }

    private ReadOnlyCollection<PreparedPropellantData> GetPreparedPropellantDataCollection(
        ReadOnlyCollection<PropellantRegionMapped> regionMappeds,
        ReadOnlyCollection<ThermodynamicsPropellantResult> thermodynamicsCalculatorResult,
        ReadOnlyCollection<PorosityPropellantResult> porosityCalculatorResult)
    {
        var preparedPropellantDatas = new List<PreparedPropellantData>();
        foreach (var propellantMapped in regionMappeds)
        {
            var thermodynamicsPropellant = thermodynamicsCalculatorResult.First(x => x.Name == propellantMapped.Name);
            var porosityPropellant = porosityCalculatorResult.First(x => x.Name == propellantMapped.Name);
            preparedPropellantDatas.Add(new PreparedPropellantData(
                propellantMapped.Name,
                thermodynamicsPropellant.PressureFrameThermodynamics,
                porosityPropellant.PressureFramePorosities
            ));
        }

        return new ReadOnlyCollection<PreparedPropellantData>(preparedPropellantDatas);
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

    private void PorosityProcessInfoLogEventHandler(ProcessInfoLogEvent e)
    {
        EventBus<InfoLogEvent>.Publish(new InfoLogEvent(e.Message, nameof(PythonPorosityCalculator)));
    }
}
