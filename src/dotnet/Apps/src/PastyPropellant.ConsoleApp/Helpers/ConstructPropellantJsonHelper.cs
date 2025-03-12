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
            
            var pressureFrameThermodynamicsList = preparedData.PressureFrameThermodynamics.ToList();
            var pressureFramePorositiesList = preparedData.PorosityPropellants.ToList();

            if (pressureFrameThermodynamicsList.Count != pressureFramePorositiesList.Count)
                throw new InvalidOperationException("The number of pressure frame thermodynamics and porosities do not match.");
            
            var pressureFrames = new List<PressureFrame>();
            for (int j = 0; j < pressureFrameThermodynamicsList.Count; j++)
            {
                var pressure = pressureFrameThermodynamicsList[j].Pressure;

                var interPocketThermodynamicsJson = ExtractObjectFromJsonFile<ThermodynamicsJson>(pressureFrameThermodynamicsList[j].InterPocketFilePath);
                var pocketWithoutSkeletonThermodynamicsJson = ExtractObjectFromJsonFile<ThermodynamicsJson>(pressureFrameThermodynamicsList[j].PocketWithoutSkeletonFilePath);
                var pocketWithSkeletonThermodynamicsJson = ExtractObjectFromJsonFile<ThermodynamicsJson>(pressureFrameThermodynamicsList[j].PocketWithSkeletonFilePath);
                var diffusionThermodynamicsJson = ExtractObjectFromJsonFile<ThermodynamicsJson>(pressureFrameThermodynamicsList[j].DiffusionFilePath);
                
                var porosityJson = ExtractObjectFromJsonFile<PorosityJson>(pressureFramePorositiesList[j].PorosityFilePath);

                var interPocketGasPhase = new HomogeneousGasPhase(
                    KineticFlameTemperature: interPocketThermodynamicsJson.Temperature,
                    AverageMolarMass: interPocketThermodynamicsJson.AverageMolarMass,
                    Lambda_Gas: 0.1,
                    SpecificHeatCapacity_Volume: interPocketThermodynamicsJson.SpecificHeatCapacity_Volume
                );

                var pocketGasPhase = new HeterogeneousGasPhase(
                    DiffusionFlameTemperature: diffusionThermodynamicsJson.Temperature,
                    AverageMolarMass: diffusionThermodynamicsJson.AverageMolarMass,
                    Lambda_Gas: 0.1,
                    SpecificHeatCapacity_Volume: diffusionThermodynamicsJson.SpecificHeatCapacity_Volume,
                    SkeletonGasPhase: new HomogeneousGasPhase(
                        KineticFlameTemperature: pocketWithSkeletonThermodynamicsJson.Temperature,
                        AverageMolarMass: pocketWithSkeletonThermodynamicsJson.AverageMolarMass,
                        Lambda_Gas: 0.1,
                        SpecificHeatCapacity_Volume: pocketWithSkeletonThermodynamicsJson.SpecificHeatCapacity_Volume
                    ),
                    OutSkeletonGasPhase: new HomogeneousGasPhase(
                        KineticFlameTemperature: pocketWithoutSkeletonThermodynamicsJson.Temperature,
                        AverageMolarMass: pocketWithoutSkeletonThermodynamicsJson.AverageMolarMass,
                        Lambda_Gas: 0.1,
                        SpecificHeatCapacity_Volume: pocketWithoutSkeletonThermodynamicsJson.SpecificHeatCapacity_Volume
                    )
                );

                var pressureFrame = new PressureFrame(
                    Pressure: pressure.Pascals,
                    Porosity: porosityJson.Porosity,
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
        double Temperature,
        [property: JsonPropertyName("specific_heat_capacity_volumetric")]
        double SpecificHeatCapacity_Volume,
        [property: JsonPropertyName("gas_average_molar_mass")]
        double AverageMolarMass
    );

    private record PorosityJson(
        [property: JsonPropertyName("porosity")]
        double Porosity
    );

    private static T ExtractObjectFromJsonFile<T>(string filePath) where T : class
    {
        using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return JsonSerializer.DeserializeAsync<T>(fileStream).Result ?? throw new JsonException();
    }
}
