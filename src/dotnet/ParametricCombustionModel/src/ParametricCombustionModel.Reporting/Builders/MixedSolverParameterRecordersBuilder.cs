using System.Collections.ObjectModel;
using ParametricCombustionModel.Computation.Builders;
using ParametricCombustionModel.Computation.Models.ComputationsParams;
using ParametricCombustionModel.Computation.Utils;
using ParametricCombustionModel.Core.Models;
using ParametricCombustionModel.Reporting.SolverParameterRecorders;

namespace ParametricCombustionModel.Reporting.Builders;

public class MixedSolverParameterRecordersBuilder : IBuilder<MixedSolverParameterRecorder>, IPressuresRequirable<MixedSolverParameterRecorder>
{
    private IEnumerable<double> _pressures;
    private IEnumerable<Propellant> _propellants;

    private MixedSolverParameterRecordersBuilder(IEnumerable<Propellant> propellants) =>
        _propellants = propellants;

    public static IPressuresRequirable<MixedSolverParameterRecorder> FromPropellants(IEnumerable<Propellant> propellants) =>
        new MixedSolverParameterRecordersBuilder(propellants);

    public IBuilder<MixedSolverParameterRecorder> ForPressures(IEnumerable<double> pressures)
    {
        _pressures = pressures;
        return this;
    }

    public ReadOnlyCollection<ReadOnlyCollection<MixedSolverParameterRecorder>> Build()
    {
        var solvers = new List<ReadOnlyCollection<MixedSolverParameterRecorder>>();

        using var propellantsIterator = _propellants.GetEnumerator();

        while (propellantsIterator.MoveNext())
        {
            var solverByPressures = new List<MixedSolverParameterRecorder>();
            foreach (var pressure in _pressures)
                solverByPressures.Add(GetMixedSolverParameterRecorder(pressure, propellantsIterator.Current));
            solvers.Add(solverByPressures.AsReadOnly());
        }

        return solvers.AsReadOnly();
    }

    private MixedSolverParameterRecorder GetMixedSolverParameterRecorder(double pressure, Propellant propellant)
    {
        double interPocketVolumeFraction = propellant.GetInterPocketAreaVolumeFraction();
        double pocketVolumeFraction = 1 - interPocketVolumeFraction;

        var propellantParams = new PropellantParams
        {
            AverageOxidizerDiameter = propellant.GetAverageParticlesDiameter(),
            Density = propellant.Density,
            InitialTemperature = propellant.InitialTemperature,
            PocketSurfaceFraction = propellant.GetPocketSurfaceFraction(),
            SpecificHeatCapacity = propellant.SpecificHeatCapacity
        };

        var interPocketGasPhase = propellant.InterPocketGasPhase;
        var interPocketParams = new KineticThermodynamicParams
        {
            KineticFlameTemperature = interPocketGasPhase.KineticFlameTemperature,
            AverageMolarMass = interPocketGasPhase.AverageMolarMass,
            Lambda_Gas = interPocketGasPhase.Lambda_Gas,
            SpecificHeatCapacity_Volume = interPocketGasPhase.SpecificHeatCapacity_Volume
        };

        var pocketSkeletonGasPhase = propellant.PocketGasPhase.SkeletonGasPhase;
        var pocketSkeletonParams = new KineticThermodynamicParams
        {
            KineticFlameTemperature = pocketSkeletonGasPhase.KineticFlameTemperature,
            AverageMolarMass = pocketSkeletonGasPhase.AverageMolarMass,
            Lambda_Gas = pocketSkeletonGasPhase.Lambda_Gas,
            SpecificHeatCapacity_Volume = pocketSkeletonGasPhase.SpecificHeatCapacity_Volume
        };

        var pocketOutSkeletonGasPhase = propellant.PocketGasPhase.OutSkeletonGasPhase;
        var pocketOutSkeletonParams = new KineticThermodynamicParams
        {
            KineticFlameTemperature = pocketOutSkeletonGasPhase.KineticFlameTemperature,
            AverageMolarMass = pocketOutSkeletonGasPhase.AverageMolarMass,
            Lambda_Gas = pocketOutSkeletonGasPhase.Lambda_Gas,
            SpecificHeatCapacity_Volume = pocketOutSkeletonGasPhase.SpecificHeatCapacity_Volume
        };

        var pocketGasPhase = propellant.PocketGasPhase;
        var pocketParams = new HeterogeneousThermodynamicParams
        {
            DiffusionFlameTemperature = pocketGasPhase.DiffusionFlameTemperature,
            AverageMolarMass = pocketGasPhase.AverageMolarMass,
            Lambda_Gas = pocketGasPhase.Lambda_Gas,
            SpecificHeatCapacity_Volume = pocketGasPhase.SpecificHeatCapacity_Volume
        };

        var metalSkeletonParams = new MetalThermodynamicParams
        {
            MetalBoilingTemperature = propellant.GetMetalBoilingTemperature(pressure),
            MetalMeltingTemperature = propellant.GetMetalMeltingTemperature()
        };

        var interPocketSolverParameterRecorder = new InterPocketSolverParameterRecorder(pressure,
                                                                                        propellantParams,
                                                                                        interPocketParams);
        var pocketSolverParameterRecorder = new PocketSolverParameterRecorder(pressure,
                                                                              propellantParams,
                                                                              pocketSkeletonParams,
                                                                              pocketOutSkeletonParams,
                                                                              pocketParams,
                                                                              metalSkeletonParams);
        return new MixedSolverParameterRecorder(pressure,
                                                interPocketVolumeFraction,
                                                pocketVolumeFraction,
                                                interPocketSolverParameterRecorder,
                                                pocketSolverParameterRecorder);
    }
}
