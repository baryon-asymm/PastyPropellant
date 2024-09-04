using System.Collections.ObjectModel;
using ParametricCombustionModel.Computation.Builders;
using ParametricCombustionModel.Computation.Models.KnownParams;
using ParametricCombustionModel.Computation.Utils;
using ParametricCombustionModel.Core.Models;
using ParametricCombustionModel.ParamsRecording.ParamsRecorders;

namespace ParametricCombustionModel.ParamsRecording.Builders;

public class MixedSolverParamsRecordersBuilder : IBuilder<MixedSolverParamsRecorder>,
                                                 IRequiredPressures<MixedSolverParamsRecorder>
{
    private IEnumerable<double> _pressures;
    private readonly IEnumerable<Propellant> _propellants;

    private MixedSolverParamsRecordersBuilder(IEnumerable<Propellant> propellants)
    {
        _propellants = propellants;
    }

    public ReadOnlyCollection<ReadOnlyCollection<MixedSolverParamsRecorder>> Build()
    {
        var solvers = new List<ReadOnlyCollection<MixedSolverParamsRecorder>>();

        using var propellantsIterator = _propellants.GetEnumerator();

        while (propellantsIterator.MoveNext())
        {
            var solverByPressures = new List<MixedSolverParamsRecorder>();
            foreach (var pressure in _pressures)
                solverByPressures.Add(GetMixedSolverParameterRecorder(pressure, propellantsIterator.Current));
            solvers.Add(solverByPressures.AsReadOnly());
        }

        return solvers.AsReadOnly();
    }

    public IBuilder<MixedSolverParamsRecorder> ForPressures(IEnumerable<double> pressures)
    {
        _pressures = pressures;
        return this;
    }

    public static IRequiredPressures<MixedSolverParamsRecorder> FromPropellants(IEnumerable<Propellant> propellants)
    {
        return new MixedSolverParamsRecordersBuilder(propellants);
    }

    private MixedSolverParamsRecorder GetMixedSolverParameterRecorder(double pressure, Propellant propellant)
    {
        var interPocketVolumeFraction = propellant.GetInterPocketAreaVolumeFraction();
        var pocketVolumeFraction = propellant.GetPocketAreaVolumeFraction();

        var propellantParams = new PropellantParams
        {
            AverageOxidizerDiameter = propellant.GetAverageParticlesDiameter(),
            Density = propellant.Density,
            InitialTemperature = propellant.InitialTemperature,
            SkeletonSurfaceFraction = propellant.GetPocketSurfaceFraction(pressure),
            SpecificHeatCapacity = propellant.SpecificHeatCapacity
        };

        var interPocketGasPhase = propellant.InterPocketGasPhase;
        var interPocketParams = new KineticFlameParams
        {
            FinalTemperature = interPocketGasPhase.KineticFlameTemperature,
            AverageMolarMass = interPocketGasPhase.AverageMolarMass,
            ThermalConductivity = interPocketGasPhase.Lambda_Gas,
            VolumetricSpecificHeatCapacity = interPocketGasPhase.SpecificHeatCapacity_Volume
        };

        var pocketSkeletonGasPhase = propellant.PocketGasPhase.SkeletonGasPhase;
        var pocketSkeletonParams = new KineticFlameParams
        {
            FinalTemperature = pocketSkeletonGasPhase.KineticFlameTemperature,
            AverageMolarMass = pocketSkeletonGasPhase.AverageMolarMass,
            ThermalConductivity = pocketSkeletonGasPhase.Lambda_Gas,
            VolumetricSpecificHeatCapacity = pocketSkeletonGasPhase.SpecificHeatCapacity_Volume
        };

        var pocketOutSkeletonGasPhase = propellant.PocketGasPhase.OutSkeletonGasPhase;
        var pocketOutSkeletonParams = new KineticFlameParams
        {
            FinalTemperature = pocketOutSkeletonGasPhase.KineticFlameTemperature,
            AverageMolarMass = pocketOutSkeletonGasPhase.AverageMolarMass,
            ThermalConductivity = pocketOutSkeletonGasPhase.Lambda_Gas,
            VolumetricSpecificHeatCapacity = pocketOutSkeletonGasPhase.SpecificHeatCapacity_Volume
        };

        var pocketGasPhase = propellant.PocketGasPhase;
        var pocketParams = new DiffusionFlameParams
        {
            FinalTemperature = pocketGasPhase.DiffusionFlameTemperature,
            AverageMolarMass = pocketGasPhase.AverageMolarMass,
            ThermalConductivity = pocketGasPhase.Lambda_Gas,
            VolumetricSpecificHeatCapacity = pocketGasPhase.SpecificHeatCapacity_Volume
        };

        var metalSkeletonParams = new MetalCombustionParams
        {
            MetalBoilingTemperature = propellant.GetMetalBoilingTemperature(pressure),
            MetalMeltingTemperature = propellant.GetMetalMeltingTemperature()
        };

        var interPocketSolverParameterRecorder = new InterPocketSolverParamsRecorder(pressure,
                                                                                     propellantParams,
                                                                                     interPocketParams);
        var pocketSolverParameterRecorder = new PocketSolverParamsRecorder(pressure,
                                                                           propellantParams,
                                                                           pocketSkeletonParams,
                                                                           pocketOutSkeletonParams,
                                                                           pocketParams,
                                                                           metalSkeletonParams);
        return new MixedSolverParamsRecorder(pressure,
                                             interPocketVolumeFraction,
                                             pocketVolumeFraction,
                                             interPocketSolverParameterRecorder,
                                             pocketSolverParameterRecorder);
    }
}
