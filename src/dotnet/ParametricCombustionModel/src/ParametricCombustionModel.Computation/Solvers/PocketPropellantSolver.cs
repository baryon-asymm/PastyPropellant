using System.Runtime.CompilerServices;
using ParametricCombustionModel.Computation.Common;
using ParametricCombustionModel.Computation.Models.ComputedParams;
using ParametricCombustionModel.Computation.Models.KnownParams;
using ParametricCombustionModel.Computation.Models.ProblemContexts;
using UnitsNet;

namespace ParametricCombustionModel.Computation.Solvers;

#region Kinetic Propellant Solvers for Pocket Combustion Model

/// <summary>
/// A concrete implementation of <see cref="BaseKineticPropellantSolver"/> designed to support the determination of kinetic flame parameters
/// within the "Skeleton" layer of the propellant combustion model. This class handles the computation of heat flux errors at the propellant surface
/// and the extraction of kinetic flame parameters specific to the Skeleton layer.
/// </summary>
public sealed class KineticSkeletonHelper : BaseKineticPropellantSolver
{
#region Publics

    /// <summary>
    /// Computes the kinetic flame heat flux based on provided combustion parameters and context bag, specifically for the "Skeleton" layer.
    /// </summary>
    /// <param name="pressure">
    /// The pressure in the combustion chamber of the rocket engine, provided as a <see cref="Pressure"/> object.
    /// </param>
    /// <param name="surfaceTemperature">
    /// The temperature of the propellant surface, provided as a <see cref="Temperature"/> object.
    /// </param>
    /// <param name="decomposeRate">
    /// The rate at which the propellant decomposes, provided as a <see cref="MassFlux"/> object.
    /// </param>
    /// <param name="solverParamsByUnits">
    /// The parameters related to the burn process, including enthalpy change and specific heat capacity, provided as a <see cref="CombustionSolverParamsByUnits"/> object.
    /// </param>
    /// <param name="kineticFlameParamsByUnits">
    /// The parameters specific to the kinetic flame, provided as a <see cref="KineticFlameParamsByUnits"/> object.
    /// </param>
    /// <param name="contextBag">
    /// A reference to the context bag containing kinetic flame combustion parameters for the Skeleton layer.
    /// </param>
    /// <returns>
    /// The calculated kinetic flame heat flux as a <see cref="HeatFlux"/> object.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public new HeatFlux GetKineticFlameHeatFlux(
        in Pressure pressure,
        in Temperature surfaceTemperature,
        in MassFlux decomposeRate,
        in CombustionSolverParamsByUnits solverParamsByUnits,
        in KineticFlameParamsByUnits kineticFlameParamsByUnits,
        ref KineticFlameCombustionParams contextBag) =>
        base.GetKineticFlameHeatFlux(pressure,
                                     surfaceTemperature,
                                     decomposeRate,
                                     solverParamsByUnits,
                                     kineticFlameParamsByUnits,
                                     ref contextBag);

    /// <summary>
    /// Computes the kinetic flame heat flux based on provided combustion parameters and context bag, specifically for the "Skeleton" layer.
    /// </summary>
    /// <param name="pressure">
    /// The pressure in the combustion chamber of the rocket engine, provided as a <see cref="double"/> value.
    /// </param>
    /// <param name="surfaceTemperature">
    /// The temperature of the propellant surface, provided as a <see cref="double"/> value.
    /// </param>
    /// <param name="decomposeRate">
    /// The rate at which the propellant decomposes, provided as a <see cref="double"/> value.
    /// </param>
    /// <param name="solverParams">
    /// The parameters related to the burn process, including enthalpy change and specific heat capacity, provided as a <see cref="CombustionSolverParamsByDoubles"/> object.
    /// </param>
    /// <param name="kineticFlameParams">
    /// The parameters specific to the kinetic flame, provided as a <see cref="KineticFlameParamsByDoubles"/> object.
    /// </param>
    /// <param name="contextBag">
    /// A reference to the context bag containing kinetic flame combustion parameters for the Skeleton layer.
    /// </param>
    /// <returns>
    /// The calculated kinetic flame heat flux as a <see cref="double"/> value.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public new double GetKineticFlameHeatFlux(
        double pressure,
        double surfaceTemperature,
        double decomposeRate,
        in CombustionSolverParamsByDoubles solverParams,
        in KineticFlameParamsByDoubles kineticFlameParams,
        ref KineticFlameCombustionParamsByDoubles contextBag) =>
        base.GetKineticFlameHeatFlux(pressure,
                                     surfaceTemperature,
                                     decomposeRate,
                                     solverParams,
                                     kineticFlameParams,
                                     ref contextBag);

#endregion

#region Overridden Methods

    public override void Visit(
        in CombustionSolverParamsByUnits solverParamsByUnits,
        ProblemContextByUnits context) =>
        throw new NotImplementedException();

    protected override HeatFlux GetSurfaceHeatFluxesError(
        in Temperature surfaceTemperature,
        in CombustionSolverParamsByUnits solverParamsByUnits,
        ProblemContextByUnits context) =>
        throw new NotImplementedException();

    /// <summary>
    /// Extracts the kinetic burn parameters specific to the Skeleton layer from the provided burn parameters.
    /// This method retrieves the pre-exponential factor and activation energy for the kinetic flame within the Skeleton layer.
    /// </summary>
    /// <param name="solverParamsByUnits">
    /// A reference to the parameters related to the burn process, provided as a <see cref="CombustionSolverParamsByUnits"/> object.
    /// </param>
    /// <param name="aKineticFlame">
    /// The pre-exponential factor for the kinetic flame, returned as a <see cref="Frequency"/> object.
    /// </param>
    /// <param name="eKineticFlame">
    /// The activation energy for the kinetic flame, returned as a <see cref="MolarEnergy"/> object.
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected override void ExtractKineticBurnParams(
        in CombustionSolverParamsByUnits solverParamsByUnits,
        out Frequency aKineticFlame,
        out MolarEnergy eKineticFlame,
        out double nu)
    {
        aKineticFlame = solverParamsByUnits.AKineticFlamePocketSkeleton;
        eKineticFlame = solverParamsByUnits.EKineticFlamePocketSkeleton;
        nu = solverParamsByUnits.NuPocketSkeleton;
    }

#endregion

#region Overridden Methods with Double Parameters

    public override void Visit(
        in CombustionSolverParamsByDoubles solverParams,
        ProblemContextByDoubles context) =>
        throw new NotImplementedException();

    protected override double GetSurfaceHeatFluxesError(
        double surfaceTemperature,
        in CombustionSolverParamsByDoubles solverParams,
        ProblemContextByDoubles context) =>
        throw new NotImplementedException();

    /// <summary>
    /// Extracts the kinetic burn parameters specific to the Skeleton layer from the provided burn parameters.
    /// This method retrieves the pre-exponential factor and activation energy for the kinetic flame within the Skeleton layer.
    /// </summary>
    /// <param name="solverParams">
    /// A reference to the parameters related to the burn process, provided as a <see cref="CombustionSolverParamsByDoubles"/> object.
    /// </param>
    /// <param name="aKineticFlame">
    /// The pre-exponential factor for the kinetic flame, returned as a <see cref="double"/> value.
    /// </param>
    /// <param name="eKineticFlame">
    /// The activation energy for the kinetic flame, returned as a <see cref="double"/> value.
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected override void ExtractKineticBurnParams(
        in CombustionSolverParamsByDoubles solverParams,
        out double aKineticFlame,
        out double eKineticFlame,
        out double nu)
    {
        aKineticFlame = solverParams.AKineticFlamePocketSkeleton;
        eKineticFlame = solverParams.EKineticFlamePocketSkeleton;
        nu = solverParams.NuPocketSkeleton;
    }

#endregion
}

/// <summary>
/// A concrete implementation of <see cref="BaseKineticPropellantSolver"/> designed to support the determination of kinetic flame parameters
/// in the "OutSkeleton" region of the propellant combustion model. This class handles the computation of heat flux errors at the propellant surface
/// and the extraction of kinetic flame parameters specific to the region outside the Skeleton layer but within the Pocket region.
/// </summary>
public sealed class KineticOutSkeletonHelper : BaseKineticPropellantSolver
{
#region Publics

    /// <summary>
    /// Computes the kinetic flame heat flux based on provided combustion parameters and context bag, specifically for the "OutSkeleton" region.
    /// </summary>
    /// <param name="pressure">
    /// The pressure in the combustion chamber of the rocket engine, provided as a <see cref="Pressure"/> object.
    /// </param>
    /// <param name="surfaceTemperature">
    /// The temperature of the propellant surface, provided as a <see cref="Temperature"/> object.
    /// </param>
    /// <param name="decomposeRate">
    /// The rate at which the propellant decomposes, provided as a <see cref="MassFlux"/> object.
    /// </param>
    /// <param name="solverParamsByUnits">
    /// The parameters related to the burn process, including enthalpy change and specific heat capacity, provided as a <see cref="CombustionSolverParamsByUnits"/> object.
    /// </param>
    /// <param name="kineticFlameParamsByUnits">
    /// The parameters specific to the kinetic flame, provided as a <see cref="KineticFlameParamsByUnits"/> object.
    /// </param>
    /// <param name="contextBag">
    /// A reference to the context bag containing kinetic flame combustion parameters for the OutSkeleton region.
    /// </param>
    /// <returns>
    /// The calculated kinetic flame heat flux as a <see cref="HeatFlux"/> object.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public new HeatFlux GetKineticFlameHeatFlux(
        in Pressure pressure,
        in Temperature surfaceTemperature,
        in MassFlux decomposeRate,
        in CombustionSolverParamsByUnits solverParamsByUnits,
        in KineticFlameParamsByUnits kineticFlameParamsByUnits,
        ref KineticFlameCombustionParams contextBag) =>
        base.GetKineticFlameHeatFlux(pressure,
                                     surfaceTemperature,
                                     decomposeRate,
                                     solverParamsByUnits,
                                     kineticFlameParamsByUnits,
                                     ref contextBag);

    /// <summary>
    /// Computes the kinetic flame heat flux based on provided combustion parameters and context bag, specifically for the "OutSkeleton" region.
    /// </summary>
    /// <param name="pressure">
    /// The pressure in the combustion chamber of the rocket engine, provided as a <see cref="double"/> value.
    /// </param>
    /// <param name="surfaceTemperature">
    /// The temperature of the propellant surface, provided as a <see cref="double"/> value.
    /// </param>
    /// <param name="decomposeRate">
    /// The rate at which the propellant decomposes, provided as a <see cref="double"/> value.
    /// </param>
    /// <param name="solverParams">
    /// The parameters related to the burn process, including enthalpy change and specific heat capacity, provided as a <see cref="CombustionSolverParamsByDoubles"/> object.
    /// </param>
    /// <param name="kineticFlameParams">
    /// The parameters specific to the kinetic flame, provided as a <see cref="KineticFlameParamsByDoubles"/> object.
    /// </param>
    /// <param name="contextBag">
    /// A reference to the context bag containing kinetic flame combustion parameters for the OutSkeleton region.
    /// </param>
    /// <returns>
    /// The calculated kinetic flame heat flux as a <see cref="double"/> value.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public new double GetKineticFlameHeatFlux(
        double pressure,
        double surfaceTemperature,
        double decomposeRate,
        in CombustionSolverParamsByDoubles solverParams,
        in KineticFlameParamsByDoubles kineticFlameParams,
        ref KineticFlameCombustionParamsByDoubles contextBag) =>
        base.GetKineticFlameHeatFlux(pressure,
                                     surfaceTemperature,
                                     decomposeRate,
                                     solverParams,
                                     kineticFlameParams,
                                     ref contextBag);

#endregion

#region Overridden Methods

    public override void Visit(
        in CombustionSolverParamsByUnits solverParamsByUnits,
        ProblemContextByUnits context) =>
        throw new NotImplementedException();

    protected override HeatFlux GetSurfaceHeatFluxesError(
        in Temperature surfaceTemperature,
        in CombustionSolverParamsByUnits solverParamsByUnits,
        ProblemContextByUnits context) =>
        throw new NotImplementedException();

    /// <summary>
    /// Extracts the kinetic burn parameters specific to the "OutSkeleton" region from the provided burn parameters.
    /// This method retrieves the pre-exponential factor and activation energy for the kinetic flame within the "OutSkeleton" region.
    /// </summary>
    /// <param name="solverParamsByUnits">
    /// A reference to the parameters related to the burn process, provided as a <see cref="CombustionSolverParamsByUnits"/> object.
    /// </param>
    /// <param name="aKineticFlame">
    /// The pre-exponential factor for the kinetic flame, returned as a <see cref="Frequency"/> object.
    /// </param>
    /// <param name="eKineticFlame">
    /// The activation energy for the kinetic flame, returned as a <see cref="MolarEnergy"/> object.
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected override void ExtractKineticBurnParams(
        in CombustionSolverParamsByUnits solverParamsByUnits,
        out Frequency aKineticFlame,
        out MolarEnergy eKineticFlame,
        out double nu)
    {
        aKineticFlame = solverParamsByUnits.AKineticFlamePocketOutSkeleton;
        eKineticFlame = solverParamsByUnits.EKineticFlamePocketOutSkeleton;
        nu = solverParamsByUnits.NuPocketOutSkeleton;
    }

#endregion

#region Overridden Methods with Double Parameters

    public override void Visit(
        in CombustionSolverParamsByDoubles solverParams,
        ProblemContextByDoubles context) =>
        throw new NotImplementedException();

    protected override double GetSurfaceHeatFluxesError(
        double surfaceTemperature,
        in CombustionSolverParamsByDoubles solverParams,
        ProblemContextByDoubles context) =>
        throw new NotImplementedException();

    /// <summary>
    /// Extracts the kinetic burn parameters specific to the "OutSkeleton" region from the provided burn parameters.
    /// This method retrieves the pre-exponential factor and activation energy for the kinetic flame within the "OutSkeleton" region.
    /// </summary>
    /// <param name="solverParams">
    /// A reference to the parameters related to the burn process, provided as a <see cref="CombustionSolverParamsByDoubles"/> object.
    /// </param>
    /// <param name="aKineticFlame">
    /// The pre-exponential factor for the kinetic flame, returned as a <see cref="double"/> value.
    /// </param>
    /// <param name="eKineticFlame">
    /// The activation energy for the kinetic flame, returned as a <see cref="double"/> value.
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected override void ExtractKineticBurnParams(
        in CombustionSolverParamsByDoubles solverParams,
        out double aKineticFlame,
        out double eKineticFlame,
        out double nu)
    {
        aKineticFlame = solverParams.AKineticFlamePocketOutSkeleton;
        eKineticFlame = solverParams.EKineticFlamePocketOutSkeleton;
        nu = solverParams.NuPocketOutSkeleton;
    }

#endregion
}

#endregion

/// <summary>
/// A concrete implementation of <see cref="BasePropellantSolver"/> designed to compute heat flux errors at the propellant surface
/// and determine various parameters in the combustion model of a rocket propellant within the "Pocket" region.
/// This class uses kinetic and diffusion models to accurately represent the combustion behavior of a propellant.
/// </summary>
public sealed class PocketPropellantSolver : BasePropellantSolver
{
#region Fields

    private readonly KineticSkeletonHelper _skeletonHelper;
    private readonly KineticOutSkeletonHelper _outSkeletonHelper;

#endregion

#region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="PocketPropellantSolver"/> class,
    /// and creates instances of <see cref="KineticSkeletonHelper"/> and <see cref="KineticOutSkeletonHelper"/>.
    /// </summary>
    public PocketPropellantSolver()
    {
        _skeletonHelper = new KineticSkeletonHelper();
        _outSkeletonHelper = new KineticOutSkeletonHelper();
    }

#endregion

#region Overridden Methods

    /// <summary>
    /// Visits the specified <see cref="ProblemContextByUnits"/> and updates the context with the calculated surface temperature,
    /// heat fluxes, and burn rate based on the provided <see cref="CombustionSolverParamsByUnits"/>.
    /// </summary>
    /// <param name="solverParamsByUnits">
    /// The parameters related to the burn process, including enthalpy change, specific heat capacity, and other combustion parameters.
    /// </param>
    /// <param name="context">
    /// The context containing the combustion parameters for the "Pocket" region, which will be updated with the computed values.
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public override void Visit(
        in CombustionSolverParamsByUnits solverParamsByUnits,
        ProblemContextByUnits context)
    {
        ref var contextBag = ref context.PocketCombustionParams;

        contextBag.BurnRateIsFound =
            TryGetSurfaceTemperature(solverParamsByUnits, context, out contextBag.SurfaceTemperature);

        if (contextBag.BurnRateIsFound)
        {
            // Update the context bags with the latest surface temperature
            contextBag.SurfaceHeatFluxesError = GetSurfaceHeatFluxesError(contextBag.SurfaceTemperature,
                                                                          solverParamsByUnits,
                                                                          context);
            // Get the burn rate using the updated context bags
            contextBag.BurnRate = GetBurnRate(contextBag.DecomposeRate, context.PropellantParamsByUnits);
        }
    }

    /// <summary>
    /// Calculates the error in heat fluxes at the propellant surface by comparing the combined kinetic and diffusion flame heat fluxes
    /// with the sublimation heat flux. This method uses the surface temperature, pressure, and burn parameters
    /// to compute the heat fluxes and their difference within the Pocket region.
    /// </summary>
    /// <param name="surfaceTemperature">
    /// The temperature of the propellant surface, provided as a <see cref="Temperature"/> object.
    /// </param>
    /// <param name="solverParamsByUnits">
    /// The parameters related to the burn process, including the enthalpy change and specific heat capacity.
    /// </param>
    /// <param name="context">
    /// The context containing the combustion parameters and other relevant details.
    /// </param>
    /// <returns>
    /// The difference between the combined kinetic and diffusion flame heat fluxes and the sublimation heat flux, returned as a <see cref="HeatFlux"/> object.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected override HeatFlux GetSurfaceHeatFluxesError(
        in Temperature surfaceTemperature,
        in CombustionSolverParamsByUnits solverParamsByUnits,
        ProblemContextByUnits context)
    {
        ref var contextBag = ref context.PocketCombustionParams;
        ref var skeletonKineticFlameParams = ref contextBag.SkeletonKineticFlameCombustionParams;
        ref var outSkeletonKineticFlameParams = ref contextBag.OutSkeletonKineticFlameCombustionParams;

        contextBag.DecomposeRate = GetDecomposeRate(surfaceTemperature,
                                                    solverParamsByUnits);

        skeletonKineticFlameParams.KineticFlameHeatFlux =
            _skeletonHelper.GetKineticFlameHeatFlux(context.Pressure,
                                                    surfaceTemperature,
                                                    contextBag.DecomposeRate,
                                                    solverParamsByUnits,
                                                    context.PocketSkeletonKineticFlameParamsByUnits,
                                                    ref skeletonKineticFlameParams);
        outSkeletonKineticFlameParams.KineticFlameHeatFlux =
            _outSkeletonHelper.GetKineticFlameHeatFlux(context.Pressure,
                                                       surfaceTemperature,
                                                       contextBag.DecomposeRate,
                                                       solverParamsByUnits,
                                                       context.PocketOutSkeletonKineticFlameParamsByUnits,
                                                       ref outSkeletonKineticFlameParams);

        contextBag.BurnRate = GetBurnRate(contextBag.DecomposeRate, context.PropellantParamsByUnits);
        contextBag.AverageMetalBurningTemperature =
            GetAverageMetalBurningTemperature(surfaceTemperature, context.PocketMetalCombustionParamsByUnits);
        contextBag.SkeletonLayerThickness = GetSkeletonLayerThickness(contextBag.BurnRate, solverParamsByUnits);
        contextBag.PoreDiameter = GetPoreDiameter(contextBag.BurnRate, solverParamsByUnits);
        contextBag.RadiativeThermalConductivity = GetRadiativeThermalConductivity(
            contextBag.AverageMetalBurningTemperature,
            contextBag.PoreDiameter,
            context.SkeletonLayerParamsByUnits);
        var minConductiveThermalConductivity = ThermalConductivity.FromWattsPerMeterKelvin(0.0);
        var maxConductiveThermalConductivity = ThermalConductivity.FromWattsPerMeterKelvin(100_000.0);
        contextBag.ConductiveThermalConductivity = GetConductiveThermalConductivityByBinarySearch(
            context.SkeletonLayerParamsByUnits,
            context.PocketDiffusionFlameParamsByUnits,
            ref minConductiveThermalConductivity,
            ref maxConductiveThermalConductivity,
            ThermalConductivity.FromWattsPerMeterKelvin(1e-6));
        contextBag.ConductiveThermalConductivityBalanceError = GetConductiveThermalConductivityError(
            contextBag.ConductiveThermalConductivity,
            context.SkeletonLayerParamsByUnits,
            context.PocketDiffusionFlameParamsByUnits);
        contextBag.EffectiveThermalConductivity = contextBag.RadiativeThermalConductivity + contextBag.ConductiveThermalConductivity;
        contextBag.MetalBurningHeatFlux = GetMetalBurningHeatFlux(
            surfaceTemperature, contextBag.SkeletonLayerThickness, contextBag.EffectiveThermalConductivity, context.PocketMetalCombustionParamsByUnits);

        contextBag.DiffusionFlameHeight = GetDiffusionFlameHeight(contextBag.DecomposeRate,
                                                                  solverParamsByUnits,
                                                                  context.PocketDiffusionFlameParamsByUnits,
                                                                  context.PropellantParamsByUnits);
        contextBag.DiffusionFlameHeatFlux = GetDiffusionFlameHeatFlux(surfaceTemperature,
                                                                      contextBag.DiffusionFlameHeight,
                                                                      context.PocketDiffusionFlameParamsByUnits);

        var fullRatio = Ratio.FromDecimalFractions(1.0);
        var outSkeletonSurfaceFraction = fullRatio - context.PropellantParamsByUnits.SkeletonSurfaceFraction;
        contextBag.OutSkeletonHeatFlux = outSkeletonSurfaceFraction.DecimalFractions
                                         * outSkeletonKineticFlameParams.KineticFlameHeatFlux;
        contextBag.SkeletonHeatFlux = context.PropellantParamsByUnits.SkeletonSurfaceFraction.DecimalFractions
                                      * (contextBag.MetalBurningHeatFlux
                                         + skeletonKineticFlameParams.KineticFlameHeatFlux);
        contextBag.ToSurfaceTotalHeatFlux = contextBag.OutSkeletonHeatFlux
                                            + contextBag.SkeletonHeatFlux
                                            + contextBag.DiffusionFlameHeatFlux;

        var enthalpyChange = context.PropellantParamsByUnits.SpecificHeatCapacity
                             * (surfaceTemperature - context.PropellantParamsByUnits.InitialTemperature)
                             + solverParamsByUnits.DeltaH;
        contextBag.SublimationHeatFlux = HeatFlux.FromWattsPerSquareMeter(
            contextBag.DecomposeRate.KilogramsPerSecondPerSquareMeter
            * enthalpyChange.JoulesPerKilogram);

        return contextBag.ToSurfaceTotalHeatFlux - contextBag.SublimationHeatFlux;
    }

#endregion

#region Computation Methods

    /// <summary>
    /// Calculates the average metal burning temperature based on the melting and boiling temperatures of the metal skeleton.
    /// </summary>
    /// <param name="metalCombustionParamsByUnits">
    /// The parameters related to metal combustion, including melting and boiling temperatures.
    /// </param>
    /// <returns>
    /// The average metal burning temperature as a <see cref="Temperature"/> object.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private Temperature GetAverageMetalBurningTemperature(
        in Temperature surfaceTemperature,
        in MetalCombustionParamsByUnits metalCombustionParamsByUnits)
    {
        var metalMeltingTemperatureDouble = metalCombustionParamsByUnits.MetalMeltingTemperature.Kelvins;

        var averageMetalBurningTemperature = Temperature.FromKelvins(
            metalMeltingTemperatureDouble - surfaceTemperature.Kelvins);

        return averageMetalBurningTemperature;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private Length GetSkeletonLayerThickness(
        Speed burnRate,
        CombustionSolverParamsByUnits solverParamsByUnits)
    {
        var aMetalBurningConstant = solverParamsByUnits.AMetalBurningConstant;

        return aMetalBurningConstant / burnRate;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private Length GetPoreDiameter(
        Speed burnRate,
        CombustionSolverParamsByUnits solverParamsByUnits)
    {
        var bMetalBurningConstant = solverParamsByUnits.BMetalBurningConstant;

        return bMetalBurningConstant / burnRate / burnRate;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private ThermalConductivity GetRadiativeThermalConductivity(
        in Temperature averageMetalBurningTemperature,
        in Length poreDiameter,
        in SkeletonLayerParamsByUnits skeletonLayerParamsByUnits)
    {
        const double stefanBoltzmannConstant = PhysicalConstants.StefanBoltzmannConstant;

        var beta = 3.0 * (1.0 - skeletonLayerParamsByUnits.Porosity.DecimalFractions) / poreDiameter.Meters;
        var radiativeThermalConductivityDouble = 16 * stefanBoltzmannConstant
                                                 * Math.Pow(averageMetalBurningTemperature.Kelvins, 3)
                                                 / beta;
                                                 
        return ThermalConductivity.FromWattsPerMeterKelvin(radiativeThermalConductivityDouble);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private ThermalConductivity GetConductiveThermalConductivityByBinarySearch(
        in SkeletonLayerParamsByUnits skeletonLayerParamsByUnits,
        in DiffusionFlameParamsByUnits diffusionFlameParamsByUnits,
        ref ThermalConductivity leftThermalConductivity,
        ref ThermalConductivity rightThermalConductivity,
        in ThermalConductivity tolerance)
    {
        if (leftThermalConductivity > rightThermalConductivity)
            (leftThermalConductivity, rightThermalConductivity) =
                (rightThermalConductivity, leftThermalConductivity);
        
        var leftValue = GetConductiveThermalConductivityError(
            leftThermalConductivity,
            skeletonLayerParamsByUnits,
            diffusionFlameParamsByUnits);
        var rightValue = GetConductiveThermalConductivityError(
            rightThermalConductivity,
            skeletonLayerParamsByUnits,
            diffusionFlameParamsByUnits);
        
        const double greaterThisNotExistSolution = 0.0;
        const double unavailableConductiveThermalConductivity = 0.0;
        if (leftValue * rightValue > greaterThisNotExistSolution)
            return ThermalConductivity.FromWattsPerMeterKelvin(unavailableConductiveThermalConductivity);
        
        var meanConductiveThermalConductivityDouble =
            (leftThermalConductivity.WattsPerMeterKelvin + rightThermalConductivity.WattsPerMeterKelvin) / 2.0;
        var meanConductiveThermalConductivity = ThermalConductivity.FromWattsPerMeterKelvin(meanConductiveThermalConductivityDouble);
        while (rightThermalConductivity - leftThermalConductivity > tolerance)
        {
            var middleValue = GetConductiveThermalConductivityError(
                meanConductiveThermalConductivity,
                skeletonLayerParamsByUnits,
                diffusionFlameParamsByUnits);
            
            if (middleValue * leftValue < 0.0)
            {
                rightThermalConductivity = meanConductiveThermalConductivity;
                // rightValue = middleValue;
            }
            else
            {
                leftThermalConductivity = meanConductiveThermalConductivity;
                leftValue = middleValue;
            }

            meanConductiveThermalConductivityDouble =
                (leftThermalConductivity.WattsPerMeterKelvin + rightThermalConductivity.WattsPerMeterKelvin) / 2.0;
            meanConductiveThermalConductivity = ThermalConductivity.FromWattsPerMeterKelvin(meanConductiveThermalConductivityDouble);
        }
        
        return meanConductiveThermalConductivity;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private double GetConductiveThermalConductivityError(
        in ThermalConductivity conductiveThermalConductivity,
        in SkeletonLayerParamsByUnits skeletonLayerParamsByUnits,
        in DiffusionFlameParamsByUnits diffusionFlameParamsByUnits)
    {
        var porosity = skeletonLayerParamsByUnits.Porosity;
        var lambdaGas = diffusionFlameParamsByUnits.ThermalConductivity;
        var lambdaCondensed = skeletonLayerParamsByUnits.CondensedThermalConductivity;
        var fullRatio = Ratio.FromDecimalFractions(1.0);

        var error = porosity * (
            (lambdaGas - conductiveThermalConductivity)
            / (lambdaGas + 2 * lambdaCondensed)
            )
            + (fullRatio - porosity) * (
                (lambdaCondensed - conductiveThermalConductivity)
                / (lambdaCondensed + 2 * lambdaGas)
            );
        
        return error.DecimalFractions;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static HeatFlux GetMetalBurningHeatFlux(
        in Temperature surfaceTemperature,
        in Length skeletonLayerThickness,
        in ThermalConductivity effectiveThermalConductivity,
        in MetalCombustionParamsByUnits metalCombustionParamsByUnits)
    {
        var metalMeltingTemperatureDouble = metalCombustionParamsByUnits.MetalMeltingTemperature.Kelvins;

        var heatFluxDouble = (metalMeltingTemperatureDouble - surfaceTemperature.Kelvins)
                             / skeletonLayerThickness.Meters
                             / effectiveThermalConductivity.WattsPerMeterKelvin;
        var heatFlux = HeatFlux.FromWattsPerSquareMeter(heatFluxDouble);

        return heatFlux;
    }

    /// <summary>
    /// Computes the height of the diffusion flame based on the decompose rate and burn parameters.
    /// </summary>
    /// <param name="decomposeRate">
    /// The mass flux rate at which the propellant decomposes, provided as a <see cref="MassFlux"/> object.
    /// </param>
    /// <param name="solverParamsByUnits">
    /// The parameters related to the burn process, including the enthalpy change and specific heat capacity.
    /// </param>
    /// <param name="diffusionFlameParamsByUnits">
    /// The parameters related to the diffusion flame, including volumetric specific heat capacity and thermal conductivity.
    /// </param>
    /// <param name="propellantParamsByUnits">
    /// The parameters related to the propellant, including the average oxidizer diameter.
    /// </param>
    /// <returns>
    /// The height of the diffusion flame as a <see cref="Length"/> object.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private Length GetDiffusionFlameHeight(
        in MassFlux decomposeRate,
        in CombustionSolverParamsByUnits solverParamsByUnits,
        in DiffusionFlameParamsByUnits diffusionFlameParamsByUnits,
        in PropellantParamsByUnits propellantParamsByUnits)
    {
        var averageOxidizerDiameter = propellantParamsByUnits.AverageOxidizerDiameter;
        var volumedSpecificHeatCapacity = diffusionFlameParamsByUnits.VolumetricSpecificHeatCapacity;
        var lambdaGas = diffusionFlameParamsByUnits.ThermalConductivity;

        var massFlow = decomposeRate * (averageOxidizerDiameter * averageOxidizerDiameter);
        var thermalConductanceDouble =
            volumedSpecificHeatCapacity.JoulesPerKilogramKelvin * massFlow.KilogramsPerSecond;
        var heightDouble = solverParamsByUnits.KDiffusionHeight
                           * thermalConductanceDouble
                           / lambdaGas.WattsPerMeterKelvin;

        return Length.FromMeters(heightDouble);
    }

    /// <summary>
    /// Computes the heat flux due to the diffusion flame based on the surface temperature and flame height.
    /// </summary>
    /// <param name="surfaceTemperature">
    /// The temperature of the propellant surface, provided as a <see cref="Temperature"/> object.
    /// </param>
    /// <param name="diffusionFlameHeight">
    /// The height of the diffusion flame, provided as a <see cref="Length"/> object.
    /// </param>
    /// <param name="diffusionFlameParamsByUnits">
    /// The parameters related to the diffusion flame, including final temperature and thermal conductivity.
    /// </param>
    /// <returns>
    /// The heat flux due to the diffusion flame as a <see cref="HeatFlux"/> object.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private HeatFlux GetDiffusionFlameHeatFlux(
        in Temperature surfaceTemperature,
        in Length diffusionFlameHeight,
        in DiffusionFlameParamsByUnits diffusionFlameParamsByUnits)
    {
        var lambdaGas = diffusionFlameParamsByUnits.ThermalConductivity;
        var diffusionFlameTemperature = diffusionFlameParamsByUnits.FinalTemperature;

        var heatFluxDouble = lambdaGas.WattsPerMeterKelvin
                             * (diffusionFlameTemperature - surfaceTemperature).Kelvins
                             / diffusionFlameHeight.Meters;

        return HeatFlux.FromWattsPerSquareMeter(heatFluxDouble);
    }

#endregion

#region Overridden Methods with Double Parameters

    /// <summary>
    /// Visits the specified <see cref="ProblemContextByDoubles"/> and updates the context with the calculated surface temperature,
    /// heat fluxes, and burn rate based on the provided <see cref="CombustionSolverParamsByDoubles"/>.
    /// </summary>
    /// <param name="solverParams">
    /// The parameters related to the burn process, including enthalpy change, specific heat capacity, and other combustion parameters.
    /// </param>
    /// <param name="context">
    /// The context containing the combustion parameters for the "Pocket" region, which will be updated with the computed values.
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public override void Visit(
        in CombustionSolverParamsByDoubles solverParams,
        ProblemContextByDoubles context)
    {
        ref var contextBag = ref context.PocketCombustionParams;

        contextBag.BurnRateIsFound = TryGetSurfaceTemperature(solverParams, context, out contextBag.SurfaceTemperature);

        if (contextBag.BurnRateIsFound)
        {
            // Update the context bags with the latest surface temperature
            contextBag.SurfaceHeatFluxesError = GetSurfaceHeatFluxesError(contextBag.SurfaceTemperature,
                                                                          solverParams,
                                                                          context);
            // Get the burn rate using the updated context bags
            contextBag.BurnRate = GetBurnRate(contextBag.DecomposeRate, context.PropellantParams);
        }
    }

    /// <summary>
    /// Calculates the error in heat fluxes at the propellant surface by comparing the combined kinetic and diffusion flame heat fluxes
    /// with the sublimation heat flux. This method uses the surface temperature, pressure, and burn parameters
    /// to compute the heat fluxes and their difference within the Pocket region.
    /// </summary>
    /// <param name="surfaceTemperature">
    /// The temperature of the propellant surface, provided as a <see cref="double"/> value in Kelvin.
    /// </param>
    /// <param name="solverParams">
    /// The parameters related to the burn process, including the enthalpy change and specific heat capacity.
    /// </param>
    /// <param name="context">
    /// The context containing the combustion parameters and other relevant details.
    /// </param>
    /// <returns>
    /// The difference between the combined kinetic and diffusion flame heat fluxes and the sublimation heat flux, returned as a <see cref="double"/> value.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected override double GetSurfaceHeatFluxesError(
        double surfaceTemperature,
        in CombustionSolverParamsByDoubles solverParams,
        ProblemContextByDoubles context)
    {
        ref var contextBag = ref context.PocketCombustionParams;
        ref var skeletonKineticFlameParams = ref contextBag.SkeletonKineticFlameCombustionParams;
        ref var outSkeletonKineticFlameParams = ref contextBag.OutSkeletonKineticFlameCombustionParams;

        contextBag.DecomposeRate = GetDecomposeRate(surfaceTemperature,
                                                    solverParams);

        skeletonKineticFlameParams.KineticFlameHeatFlux =
            _skeletonHelper.GetKineticFlameHeatFlux(context.Pressure,
                                                    surfaceTemperature,
                                                    contextBag.DecomposeRate,
                                                    solverParams,
                                                    context.PocketSkeletonKineticFlameParams,
                                                    ref skeletonKineticFlameParams);
        outSkeletonKineticFlameParams.KineticFlameHeatFlux =
            _outSkeletonHelper.GetKineticFlameHeatFlux(context.Pressure,
                                                       surfaceTemperature,
                                                       contextBag.DecomposeRate,
                                                       solverParams,
                                                       context.PocketOutSkeletonKineticFlameParams,
                                                       ref outSkeletonKineticFlameParams);

        contextBag.BurnRate = GetBurnRate(contextBag.DecomposeRate, context.PropellantParams);
        contextBag.AverageMetalBurningTemperature =
            GetAverageMetalBurningTemperature(surfaceTemperature, context.PocketMetalCombustionParams);
        contextBag.SkeletonLayerThickness = GetSkeletonLayerThickness(contextBag.BurnRate, solverParams);
        contextBag.PoreDiameter = GetPoreDiameter(contextBag.BurnRate, solverParams);
        contextBag.RadiativeThermalConductivity = GetRadiativeThermalConductivity(
            contextBag.AverageMetalBurningTemperature,
            contextBag.PoreDiameter,
            context.SkeletonLayerParams);
        var minConductiveThermalConductivity = 0.0;
        var maxConductiveThermalConductivity = 100_000.0;
        contextBag.ConductiveThermalConductivity = GetConductiveThermalConductivityByBinarySearch(
            context.SkeletonLayerParams,
            context.PocketDiffusionFlameParams,
            minConductiveThermalConductivity,
            maxConductiveThermalConductivity,
            1e-6);
        contextBag.ConductiveThermalConductivityBalanceError = GetConductiveThermalConductivityError(
            contextBag.ConductiveThermalConductivity,
            context.SkeletonLayerParams,
            context.PocketDiffusionFlameParams);
        contextBag.EffectiveThermalConductivity = contextBag.RadiativeThermalConductivity + contextBag.ConductiveThermalConductivity;
        contextBag.MetalBurningHeatFlux = GetMetalBurningHeatFlux(
            surfaceTemperature, contextBag.SkeletonLayerThickness, contextBag.EffectiveThermalConductivity, context.PocketMetalCombustionParams);

        contextBag.DiffusionFlameHeight = GetDiffusionFlameHeight(contextBag.DecomposeRate,
                                                                  solverParams,
                                                                  context.PocketDiffusionFlameParams,
                                                                  context.PropellantParams);
        contextBag.DiffusionFlameHeatFlux = GetDiffusionFlameHeatFlux(surfaceTemperature,
                                                                      contextBag.DiffusionFlameHeight,
                                                                      context.PocketDiffusionFlameParams);

        var fullRatio = 1.0;
        var outSkeletonSurfaceFraction = fullRatio - context.PropellantParams.SkeletonSurfaceFraction;
        contextBag.OutSkeletonHeatFlux = outSkeletonSurfaceFraction
                                         * outSkeletonKineticFlameParams.KineticFlameHeatFlux;
        contextBag.SkeletonHeatFlux = context.PropellantParams.SkeletonSurfaceFraction
                                      * (contextBag.MetalBurningHeatFlux
                                         + skeletonKineticFlameParams.KineticFlameHeatFlux);
        contextBag.ToSurfaceTotalHeatFlux = contextBag.OutSkeletonHeatFlux
                                            + contextBag.SkeletonHeatFlux
                                            + contextBag.DiffusionFlameHeatFlux;

        var enthalpyChange = context.PropellantParams.SpecificHeatCapacity
                             * (surfaceTemperature - context.PropellantParams.InitialTemperature)
                             + solverParams.DeltaH;
        contextBag.SublimationHeatFlux =
            contextBag.DecomposeRate
            * enthalpyChange;

        return contextBag.ToSurfaceTotalHeatFlux - contextBag.SublimationHeatFlux;
    }

#endregion

#region Computation Methods with Double Parameters

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private double GetAverageMetalBurningTemperature(
        double surfaceTemperature,
        in MetalCombustionParamsByDoubles metalCombustionParams)
    {
        var metalMeltingTemperatureDouble = metalCombustionParams.MetalMeltingTemperature;

        var averageMetalBurningTemperature = metalMeltingTemperatureDouble - surfaceTemperature;

        return averageMetalBurningTemperature;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private double GetSkeletonLayerThickness(
        double burnRate,
        CombustionSolverParamsByDoubles solverParamsByDoubles)
    {
        var aMetalBurningConstant = solverParamsByDoubles.AMetalBurningConstant;

        return aMetalBurningConstant / burnRate;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private double GetPoreDiameter(
        double burnRate,
        CombustionSolverParamsByDoubles solverParamsByDoubles)
    {
        var bMetalBurningConstant = solverParamsByDoubles.BMetalBurningConstant;

        return bMetalBurningConstant / burnRate / burnRate;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private double GetRadiativeThermalConductivity(
        double averageMetalBurningTemperature,
        double poreDiameter,
        in SkeletonLayerParamsByDoubles skeletonLayerParamsByDoubles)
    {
        const double stefanBoltzmannConstant = PhysicalConstants.StefanBoltzmannConstant;

        var beta = 3.0 * (1.0 - skeletonLayerParamsByDoubles.Porosity) / poreDiameter;
        var radiativeThermalConductivity = 16 * stefanBoltzmannConstant
                                                 * Math.Pow(averageMetalBurningTemperature, 3)
                                                 / beta;
                                                 
        return radiativeThermalConductivity;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private double GetConductiveThermalConductivityByBinarySearch(
        in SkeletonLayerParamsByDoubles skeletonLayerParamsByDoubles,
        in DiffusionFlameParamsByDoubles diffusionFlameParamsByDoubles,
        double leftThermalConductivity,
        double rightThermalConductivity,
        double tolerance)
    {
        if (leftThermalConductivity > rightThermalConductivity)
            (leftThermalConductivity, rightThermalConductivity) =
                (rightThermalConductivity, leftThermalConductivity);
        
        var leftValue = GetConductiveThermalConductivityError(
            leftThermalConductivity,
            skeletonLayerParamsByDoubles,
            diffusionFlameParamsByDoubles);
        var rightValue = GetConductiveThermalConductivityError(
            rightThermalConductivity,
            skeletonLayerParamsByDoubles,
            diffusionFlameParamsByDoubles);
        
        const double greaterThisNotExistSolution = 0.0;
        const double unavailableConductiveThermalConductivity = 0.0;
        if (leftValue * rightValue > greaterThisNotExistSolution)
            return unavailableConductiveThermalConductivity;
        
        var meanConductiveThermalConductivityDouble =
            (leftThermalConductivity + rightThermalConductivity) / 2.0;
        while (rightThermalConductivity - leftThermalConductivity > tolerance)
        {
            var middleValue = GetConductiveThermalConductivityError(
                meanConductiveThermalConductivityDouble,
                skeletonLayerParamsByDoubles,
                diffusionFlameParamsByDoubles);
            
            if (middleValue * leftValue < 0.0)
            {
                rightThermalConductivity = meanConductiveThermalConductivityDouble;
                // rightValue = middleValue;
            }
            else
            {
                leftThermalConductivity = meanConductiveThermalConductivityDouble;
                leftValue = middleValue;
            }

            meanConductiveThermalConductivityDouble =
                (leftThermalConductivity + rightThermalConductivity) / 2.0;
        }
        
        return meanConductiveThermalConductivityDouble;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private double GetConductiveThermalConductivityError(
        double conductiveThermalConductivity,
        in SkeletonLayerParamsByDoubles skeletonLayerParamsByDoubles,
        in DiffusionFlameParamsByDoubles diffusionFlameParamsByDoubles)
    {
        var porosity = skeletonLayerParamsByDoubles.Porosity;
        var lambdaGas = diffusionFlameParamsByDoubles.ThermalConductivity;
        var lambdaCondensed = skeletonLayerParamsByDoubles.CondensedThermalConductivity;

        var error = porosity * (
            (lambdaGas - conductiveThermalConductivity)
            / (lambdaGas + 2 * lambdaCondensed)
            )
            + (1.0 - porosity) * (
                (lambdaCondensed - conductiveThermalConductivity)
                / (lambdaCondensed + 2 * lambdaGas)
            );
        
        return error;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static double GetMetalBurningHeatFlux(
        double surfaceTemperature,
        double skeletonLayerThickness,
        double effectiveThermalConductivity,
        in MetalCombustionParamsByDoubles metalCombustionParamsByDoubles)
    {
        var metalMeltingTemperatureDouble = metalCombustionParamsByDoubles.MetalMeltingTemperature;

        var heatFluxDouble = (metalMeltingTemperatureDouble - surfaceTemperature)
                             / skeletonLayerThickness
                             / effectiveThermalConductivity;

        return heatFluxDouble;
    }

    /// <summary>
    /// Computes the height of the diffusion flame based on the decomposition rate, the provided combustion solver parameters,
    /// diffusion flame parameters, and propellant parameters.
    /// </summary>
    /// <param name="decomposeRate">
    /// The mass flux rate at which the propellant decomposes, provided as a <see cref="double"/> value in kilograms per second per square meter (kg/s/m²).
    /// </param>
    /// <param name="solverParams">
    /// The parameters related to the burn process, including the diffusion height coefficient.
    /// </param>
    /// <param name="diffusionFlameParams">
    /// The parameters related to the diffusion flame, including volumetric specific heat capacity and thermal conductivity.
    /// </param>
    /// <param name="propellantParams">
    /// The parameters related to the propellant, including the average oxidizer diameter.
    /// </param>
    /// <returns>
    /// The height of the diffusion flame as a <see cref="double"/> value in meters (m).
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private double GetDiffusionFlameHeight(
        double decomposeRate,
        in CombustionSolverParamsByDoubles solverParams,
        in DiffusionFlameParamsByDoubles diffusionFlameParams,
        in PropellantParamsByDoubles propellantParams)
    {
        var averageOxidizerDiameter = propellantParams.AverageOxidizerDiameter;
        var volumedSpecificHeatCapacity = diffusionFlameParams.VolumetricSpecificHeatCapacity;
        var lambdaGas = diffusionFlameParams.ThermalConductivity;

        var massFlow = decomposeRate * (averageOxidizerDiameter * averageOxidizerDiameter);
        var thermalConductanceDouble = volumedSpecificHeatCapacity * massFlow;
        var heightDouble = solverParams.KDiffusionHeight
                           * thermalConductanceDouble
                           / lambdaGas;

        return heightDouble;
    }

    /// <summary>
    /// Computes the heat flux due to the diffusion flame based on the surface temperature, the height of the diffusion flame,
    /// and the provided diffusion flame parameters.
    /// </summary>
    /// <param name="surfaceTemperature">
    /// The temperature of the propellant surface, provided as a <see cref="double"/> value in Kelvin.
    /// </param>
    /// <param name="diffusionFlameHeight">
    /// The height of the diffusion flame, provided as a <see cref="double"/> value in meters (m).
    /// </param>
    /// <param name="diffusionFlameParams">
    /// The parameters related to the diffusion flame, including final temperature and thermal conductivity.
    /// </param>
    /// <returns>
    /// The heat flux due to the diffusion flame as a <see cref="double"/> value in Watts per square meter (W/m²).
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private double GetDiffusionFlameHeatFlux(
        double surfaceTemperature,
        double diffusionFlameHeight,
        in DiffusionFlameParamsByDoubles diffusionFlameParams)
    {
        var lambdaGas = diffusionFlameParams.ThermalConductivity;
        var diffusionFlameTemperature = diffusionFlameParams.FinalTemperature;

        var heatFluxDouble = lambdaGas
                             * (diffusionFlameTemperature - surfaceTemperature)
                             / diffusionFlameHeight;

        return heatFluxDouble;
    }

#endregion
}
