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

        contextBag.AverageMetalBurningTemperature =
            GetAverageMetalBurningTemperature(solverParamsByUnits, context.PocketMetalCombustionParamsByUnits);
        contextBag.MetalBurningHeatFlux =
            GetMetalBurningHeatFlux(contextBag.AverageMetalBurningTemperature,
                                    solverParamsByUnits);

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
        in CombustionSolverParamsByUnits solverParams,
        in MetalCombustionParamsByUnits metalCombustionParamsByUnits)
    {
        var metalMeltingTemperatureDouble = metalCombustionParamsByUnits.MetalMeltingTemperature.Kelvins;
        var metalBoilingTemperatureDouble = metalCombustionParamsByUnits.MetalBoilingTemperature.Kelvins;

        var k = solverParams.KMetalTemperature.DecimalFractions;
        var averageMetalBurningTemperature = Temperature.FromKelvins(
            k * metalMeltingTemperatureDouble + (1.0 - k) * metalBoilingTemperatureDouble);

        return averageMetalBurningTemperature;
    }

    /// <summary>
    /// Computes the heat flux due to metal burning in the Skeleton layer.
    /// </summary>
    /// <param name="averageMetalBurningTemperature">
    /// The average temperature at which the metal burns, provided as a <see cref="Temperature"/> object.
    /// </param>
    /// <param name="solverParamsByUnits">
    /// The parameters related to the burn process, including the enthalpy change and specific heat capacity.
    /// </param>
    /// <returns>
    /// The heat flux due to metal burning as a <see cref="HeatFlux"/> object.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static HeatFlux GetMetalBurningHeatFlux(
        in Temperature averageMetalBurningTemperature,
        in CombustionSolverParamsByUnits solverParamsByUnits)
    {
        var hMetalBurning = solverParamsByUnits.HMetalBurning;
        var eMetalBurning = solverParamsByUnits.EMetalBurning;
        const double gasConstant = PhysicalConstants.UniversalGasConstant;

        var molarEnergy = MolarEnergy.FromJoulesPerMole(
            gasConstant * averageMetalBurningTemperature.Kelvins);

        return hMetalBurning
               * Math.Exp(-eMetalBurning / molarEnergy)
               * averageMetalBurningTemperature;
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

        contextBag.AverageMetalBurningTemperature =
            GetAverageMetalBurningTemperature(solverParams, context.PocketMetalCombustionParams);
        contextBag.MetalBurningHeatFlux =
            GetMetalBurningHeatFlux(contextBag.AverageMetalBurningTemperature,
                                    solverParams);

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

    /// <summary>
    /// Calculates the average metal burning temperature based on the melting and boiling temperatures of the metal.
    /// </summary>
    /// <param name="metalCombustionParams">
    /// The parameters related to metal combustion, including melting and boiling temperatures.
    /// </param>
    /// <returns>
    /// The average metal burning temperature in Kelvin as a <see cref="double"/> value.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private double GetAverageMetalBurningTemperature(
        in CombustionSolverParamsByDoubles solverParams,
        in MetalCombustionParamsByDoubles metalCombustionParams)
    {
        var metalMeltingTemperatureDouble = metalCombustionParams.MetalMeltingTemperature;
        var metalBoilingTemperatureDouble = metalCombustionParams.MetalBoilingTemperature;

        var k = solverParams.KMetalTemperature;
        var averageMetalBurningTemperature =
            k * metalMeltingTemperatureDouble + (1.0 - k) * metalBoilingTemperatureDouble;

        return averageMetalBurningTemperature;
    }

    /// <summary>
    /// Computes the heat flux due to metal burning based on the average metal burning temperature
    /// and the provided combustion solver parameters.
    /// </summary>
    /// <param name="averageMetalBurningTemperature">
    /// The average temperature at which the metal burns, provided as a <see cref="double"/> value in Kelvin.
    /// </param>
    /// <param name="solverParams">
    /// The parameters related to the burn process, including the metal burning enthalpy and activation energy.
    /// </param>
    /// <returns>
    /// The heat flux due to metal burning as a <see cref="double"/> value in Watts per square meter (W/m²).
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static double GetMetalBurningHeatFlux(
        double averageMetalBurningTemperature,
        in CombustionSolverParamsByDoubles solverParams)
    {
        var hMetalBurning = solverParams.HMetalBurning;
        var eMetalBurning = solverParams.EMetalBurning;
        const double gasConstant = PhysicalConstants.UniversalGasConstant;

        var molarEnergy = gasConstant * averageMetalBurningTemperature;

        return hMetalBurning
               * Math.Exp(-eMetalBurning / molarEnergy)
               * averageMetalBurningTemperature;
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
