using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using ParametricCombustionModel.Core.Models;
using ParametricCombustionModel.Core.Models.GasPhases;
using PastyPropellant.Core.Utils;

namespace PastyPropellant.ConsoleApp.Helpers;

public class ConstructPropellantJsonHelper
{
    private readonly string _originPropellantsFilePath;
    private readonly ReadOnlyCollection<PreparedPropellantData> _preparedPropellantData;
    
    public ConstructPropellantJsonHelper(string originPropellantsFilePath, IEnumerable<PreparedPropellantData> preparedPropellantData)
    {
        ArgumentNullException.ThrowIfNull(originPropellantsFilePath, nameof(originPropellantsFilePath));
        ArgumentNullException.ThrowIfNull(preparedPropellantData, nameof(preparedPropellantData));

        _originPropellantsFilePath = originPropellantsFilePath;
        _preparedPropellantData = preparedPropellantData.ToArray().AsReadOnly();
    }

    public async Task<OperationResult> ConstructAsync(string outputFilePath)
    {
        try
        {
            return await TryConstructAsync(outputFilePath);
        }
        catch (Exception ex)
        {
            return new OperationResult(ex);
        }
    }

    private async Task<OperationResult> TryConstructAsync(string outputFilePath)
    {
        var propellants = ExtractObjectFromJsonFile<IEnumerable<Propellant>>(_originPropellantsFilePath).ToList();

        if (propellants.Count != _preparedPropellantData.Count)
            throw new InvalidOperationException("The number of propellants and prepared propellant data do not match.");

        for (var i = 0; i < propellants.Count; i++)
        {
            var propellant = propellants[i];
            var preparedData = _preparedPropellantData[i];

            if (propellant.Name != preparedData.Name)
                throw new InvalidOperationException("The propellant names do not match.");
            
            var pressureFrames = new List<PressureFrame>();
            foreach (var pressureFrameThermodynamics in preparedData.PressureFrameThermodynamics)
            {
                var interPocketThermodynamicsJson = ExtractObjectFromJsonFile<ThermodynamicsJson>(pressureFrameThermodynamics.InterPocketFilePath);
                var pocketWithoutSkeletonThermodynamicsJson = ExtractObjectFromJsonFile<ThermodynamicsJson>(pressureFrameThermodynamics.PocketWithoutSkeletonFilePath);
                var pocketWithSkeletonThermodynamicsJson = ExtractObjectFromJsonFile<ThermodynamicsJson>(pressureFrameThermodynamics.PocketWithSkeletonFilePath);
                var diffusionThermodynamicsJson = ExtractObjectFromJsonFile<ThermodynamicsJson>(pressureFrameThermodynamics.DiffusionFilePath);

                var interPocketGasPhase = new HomogeneousGasPhase(
                    KineticFlameTemperature: interPocketThermodynamicsJson.Temperature,
                    AverageMolarMass: 32.7e-3,
                    Lambda_Gas: 0.1,
                    SpecificHeatCapacity_Volume: 635.6
                );

                var pocketGasPhase = new HeterogeneousGasPhase(
                    DiffusionFlameTemperature: diffusionThermodynamicsJson.Temperature,
                    AverageMolarMass: 32.7e-3,
                    Lambda_Gas: 0.1,
                    SpecificHeatCapacity_Volume: 635.6,
                    SkeletonGasPhase: new HomogeneousGasPhase(
                        KineticFlameTemperature: pocketWithSkeletonThermodynamicsJson.Temperature,
                        AverageMolarMass: 32.7e-3,
                        Lambda_Gas: 0.1,
                        SpecificHeatCapacity_Volume: 635.6
                    ),
                    OutSkeletonGasPhase: new HomogeneousGasPhase(
                        KineticFlameTemperature: pocketWithoutSkeletonThermodynamicsJson.Temperature,
                        AverageMolarMass: 32.7e-3,
                        Lambda_Gas: 0.1,
                        SpecificHeatCapacity_Volume: 635.6
                    )
                );

                var pressureFrame = new PressureFrame(
                    Pressure: pressureFrameThermodynamics.Pressure.Pascals,
                    InterPocketGasPhase: interPocketGasPhase,
                    PocketGasPhase: pocketGasPhase
                );

                pressureFrames.Add(pressureFrame);
            }

            propellant = propellant with { PressureFrames = pressureFrames };
            propellants[i] = propellant;
        }

        await using var fileStream = File.Create(outputFilePath);
        await JsonSerializer.SerializeAsync(fileStream, propellants);

        return new OperationResult();
    }

    private record ThermodynamicsJson(
        [property: JsonPropertyName("temperature")]
        double Temperature
    );

    private static T ExtractObjectFromJsonFile<T>(string filePath) where T : class
    {
        using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return JsonSerializer.DeserializeAsync<T>(fileStream).Result ?? throw new JsonException();
    }
}
