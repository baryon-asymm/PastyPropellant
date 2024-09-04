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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public new HeatFlux GetKineticFlameHeatFlux(
        in Pressure pressure,
        in Temperature surfaceTemperature,
        in MassFlux decomposeRate,
        in CombustionSolverParams solverParams,
        in KineticFlameParams kineticFlameParams,
        ref KineticFlameCombustionParams contextBag) =>
        base.GetKineticFlameHeatFlux(pressure,
                                     surfaceTemperature,
                                     decomposeRate,
                                     solverParams,
                                     kineticFlameParams,
                                     ref contextBag);

#endregion

#region Overridden Methods

    public override void Visit(
        in CombustionSolverParams solverParams,
        ProblemContextByUnits context)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Calculates the error in heat fluxes at the propellant surface by comparing the kinetic flame heat flux
    /// with the sublimation heat flux. This method uses the surface temperature, pressure, and burn parameters
    /// to compute the heat fluxes and their difference within the Skeleton layer.
    /// </summary>
    /// <param name="pressure">
    /// The pressure in the combustion chamber of the rocket engine.
    /// </param>
    /// <param name="surfaceTemperature">
    /// The temperature of the propellant surface.
    /// </param>
    /// <param name="solverParams">
    /// The parameters related to the burn process, including the enthalpy change and specific heat capacity.
    /// </param>
    /// <returns>
    /// The difference between the kinetic flame heat flux and the sublimation heat flux as a <see cref="HeatFlux"/>.
    /// </returns>
    protected override HeatFlux GetSurfaceHeatFluxesError(
        in Temperature surfaceTemperature,
        in CombustionSolverParams solverParams,
        ProblemContextByUnits context)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Extracts the kinetic burn parameters specific to the Skeleton layer from the provided burn parameters.
    /// This method retrieves the pre-exponential factor and activation energy for the kinetic flame within the Skeleton layer.
    /// </summary>
    /// <param name="solverParams">
    /// A reference to the parameters related to the burn process, provided as a <see cref="CombustionSolverParams"/> object.
    /// </param>
    /// <param name="aKineticFlame">
    /// The pre-exponential factor for the kinetic flame, returned as a <see cref="Frequency"/> object.
    /// </param>
    /// <param name="eKineticFlame">
    /// The activation energy for the kinetic flame, returned as a <see cref="MolarEnergy"/> object.
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override void ExtractKineticBurnParams(
        in CombustionSolverParams solverParams,
        out Frequency aKineticFlame,
        out MolarEnergy eKineticFlame)
    {
        aKineticFlame = solverParams.AKineticFlamePocketSkeleton;
        eKineticFlame = solverParams.EKineticFlamePocketSkeleton;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public new HeatFlux GetKineticFlameHeatFlux(
        in Pressure pressure,
        in Temperature surfaceTemperature,
        in MassFlux decomposeRate,
        in CombustionSolverParams solverParams,
        in KineticFlameParams kineticFlameParams,
        ref KineticFlameCombustionParams contextBag) =>
        base.GetKineticFlameHeatFlux(pressure,
                                     surfaceTemperature,
                                     decomposeRate,
                                     solverParams,
                                     kineticFlameParams,
                                     ref contextBag);

#endregion

#region Overridden Methods

    public override void Visit(
        in CombustionSolverParams solverParams,
        ProblemContextByUnits context)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Calculates the error in heat fluxes at the propellant surface by comparing the kinetic flame heat flux
    /// with the sublimation heat flux. This method uses the surface temperature, pressure, and burn parameters
    /// to compute the heat fluxes and their difference within the "OutSkeleton" region.
    /// </summary>
    /// <param name="pressure">
    /// The pressure in the combustion chamber of the rocket engine.
    /// </param>
    /// <param name="surfaceTemperature">
    /// The temperature of the propellant surface.
    /// </param>
    /// <param name="solverParams">
    /// The parameters related to the burn process, including the enthalpy change and specific heat capacity.
    /// </param>
    /// <returns>
    /// The difference between the kinetic flame heat flux and the sublimation heat flux as a <see cref="HeatFlux"/>.
    /// </returns>
    protected override HeatFlux GetSurfaceHeatFluxesError(
        in Temperature surfaceTemperature,
        in CombustionSolverParams solverParams,
        ProblemContextByUnits context)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Extracts the kinetic burn parameters specific to the "OutSkeleton" region from the provided burn parameters.
    /// This method retrieves the pre-exponential factor and activation energy for the kinetic flame within the "OutSkeleton" region.
    /// </summary>
    /// <param name="solverParams">
    /// A reference to the parameters related to the burn process, provided as a <see cref="CombustionSolverParams"/> object.
    /// </param>
    /// <param name="aKineticFlame">
    /// The pre-exponential factor for the kinetic flame, returned as a <see cref="Frequency"/> object.
    /// </param>
    /// <param name="eKineticFlame">
    /// The activation energy for the kinetic flame, returned as a <see cref="MolarEnergy"/> object.
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override void ExtractKineticBurnParams(
        in CombustionSolverParams solverParams,
        out Frequency aKineticFlame,
        out MolarEnergy eKineticFlame)
    {
        aKineticFlame = solverParams.AKineticFlamePocketOutSkeleton;
        eKineticFlame = solverParams.EKineticFlamePocketOutSkeleton;
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

    public PocketPropellantSolver()
    {
        _skeletonHelper = new KineticSkeletonHelper();
        _outSkeletonHelper = new KineticOutSkeletonHelper();
    }

#endregion

#region Overridden Methods

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void Visit(
        in CombustionSolverParams solverParams,
        ProblemContextByUnits context)
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
    /// <param name="pressure">
    /// The pressure in the combustion chamber of the rocket engine.
    /// </param>
    /// <param name="surfaceTemperature">
    /// The temperature of the propellant surface.
    /// </param>
    /// <param name="solverParams">
    /// The parameters related to the burn process, including the enthalpy change and specific heat capacity.
    /// </param>
    /// <returns>
    /// The difference between the combined kinetic and diffusion flame heat fluxes and the sublimation heat flux as a <see cref="HeatFlux"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override HeatFlux GetSurfaceHeatFluxesError(
        in Temperature surfaceTemperature,
        in CombustionSolverParams solverParams,
        ProblemContextByUnits context)
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
            GetAverageMetalBurningTemperature(context.PocketMetalCombustionParams);
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

        var fullRatio = Ratio.FromDecimalFractions(1.0);
        var outSkeletonSurfaceFraction = fullRatio - context.PropellantParams.SkeletonSurfaceFraction;
        contextBag.OutSkeletonHeatFlux = outSkeletonSurfaceFraction.DecimalFractions
                                         * outSkeletonKineticFlameParams.KineticFlameHeatFlux;
        contextBag.SkeletonHeatFlux = context.PropellantParams.SkeletonSurfaceFraction.DecimalFractions
                                      * (contextBag.MetalBurningHeatFlux
                                         + skeletonKineticFlameParams.KineticFlameHeatFlux);
        contextBag.ToSurfaceTotalHeatFlux = contextBag.OutSkeletonHeatFlux
                                            + contextBag.SkeletonHeatFlux
                                            + contextBag.DiffusionFlameHeatFlux;

        var enthalpyChange = context.PropellantParams.SpecificHeatCapacity
                             * (surfaceTemperature - context.PropellantParams.InitialTemperature)
                             + solverParams.DeltaH;
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
    /// <returns>
    /// The average metal burning temperature as a <see cref="Temperature"/> object.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Temperature GetAverageMetalBurningTemperature(
        in MetalCombustionParams metalCombustionParams)
    {
        var metalMeltingTemperatureDouble = metalCombustionParams.MetalMeltingTemperature.Kelvins;
        var metalBoilingTemperatureDouble = metalCombustionParams.MetalBoilingTemperature.Kelvins;

        var averageMetalBurningTemperature = Temperature.FromKelvins(
            (metalMeltingTemperatureDouble + metalBoilingTemperatureDouble) / 2.0);

        return averageMetalBurningTemperature;
    }

    /// <summary>
    /// Computes the heat flux due to metal burning in the Skeleton layer.
    /// </summary>
    /// <param name="averageMetalBurningTemperature">
    /// The average temperature at which the metal burns.
    /// </param>
    /// <param name="solverParams">
    /// The parameters related to the burn process, including the enthalpy change and specific heat capacity.
    /// </param>
    /// <returns>
    /// The heat flux due to metal burning as a <see cref="HeatFlux"/> object.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static HeatFlux GetMetalBurningHeatFlux(
        in Temperature averageMetalBurningTemperature,
        in CombustionSolverParams solverParams)
    {
        var hMetalBurning = solverParams.HMetalBurning;
        var eMetalBurning = solverParams.EMetalBurning;
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
    /// The mass flux rate at which the propellant decomposes.
    /// </param>
    /// <param name="solverParams">
    /// The parameters related to the burn process, including the enthalpy change and specific heat capacity.
    /// </param>
    /// <returns>
    /// The height of the diffusion flame as a <see cref="Length"/> object.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Length GetDiffusionFlameHeight(
        in MassFlux decomposeRate,
        in CombustionSolverParams solverParams,
        in DiffusionFlameParams diffusionFlameParams,
        in PropellantParams propellantParams)
    {
        var averageOxidizerDiameter = propellantParams.AverageOxidizerDiameter;
        var volumedSpecificHeatCapacity = diffusionFlameParams.VolumetricSpecificHeatCapacity;
        var lambdaGas = diffusionFlameParams.ThermalConductivity;

        var massFlow = decomposeRate * (averageOxidizerDiameter * averageOxidizerDiameter);
        var thermalConductanceDouble =
            volumedSpecificHeatCapacity.JoulesPerKilogramKelvin * massFlow.KilogramsPerSecond;
        var heightDouble = solverParams.KDiffusionHeight
                           * thermalConductanceDouble
                           / lambdaGas.WattsPerMeterKelvin;

        return Length.FromMeters(heightDouble);
    }

    /// <summary>
    /// Computes the heat flux due to the diffusion flame based on the surface temperature and flame height.
    /// </summary>
    /// <param name="surfaceTemperature">
    /// The temperature of the propellant surface.
    /// </param>
    /// <param name="diffusionFlameHeight">
    /// The height of the diffusion flame.
    /// </param>
    /// <returns>
    /// The heat flux due to the diffusion flame as a <see cref="HeatFlux"/> object.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private HeatFlux GetDiffusionFlameHeatFlux(
        in Temperature surfaceTemperature,
        in Length diffusionFlameHeight,
        in DiffusionFlameParams diffusionFlameParams)
    {
        var lambdaGas = diffusionFlameParams.ThermalConductivity;
        var diffusionFlameTemperature = diffusionFlameParams.FinalTemperature;

        var heatFluxDouble = lambdaGas.WattsPerMeterKelvin
                             * (diffusionFlameTemperature - surfaceTemperature).Kelvins
                             / diffusionFlameHeight.Meters;

        return HeatFlux.FromWattsPerSquareMeter(heatFluxDouble);
    }

#endregion
}
