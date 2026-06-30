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
        contextBag.AverageMetalBurningTemperature = Temperature.Zero;
        //    GetAverageMetalBurningTemperature(surfaceTemperature, context.PocketMetalCombustionParamsByUnits);
        contextBag.SkeletonLayerThickness = GetSkeletonLayerThickness(contextBag.BurnRate, solverParamsByUnits);
        contextBag.PoreDiameter = GetPoreDiameter(contextBag.BurnRate, solverParamsByUnits);
        var averageRadiativeTemperatureDouble = solverParamsByUnits.KCoefficientRadiationTemperature * surfaceTemperature.Kelvins +
            (1.0 - solverParamsByUnits.KCoefficientRadiationTemperature) * context.PocketSkeletonKineticFlameParamsByUnits.FinalTemperature.Kelvins;
        contextBag.RadiativeThermalConductivity = GetRadiativeThermalConductivity(
            Temperature.FromKelvins(averageRadiativeTemperatureDouble),
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

        contextBag.DiffusionFlameHeatFlux = GetDiffusionFlameHeatFlux(surfaceTemperature,
                                                                      contextBag.DecomposeRate,
                                                                      context.Pressure,
                                                                      solverParamsByUnits,
                                                                      context.PocketDiffusionFlameParamsByUnits,
                                                                      context.PropellantParamsByUnits,
                                                                      out var diffusionFlameHeight);
        contextBag.DiffusionFlameHeight = diffusionFlameHeight;

        var fullRatio = Ratio.FromDecimalFractions(1.0);
        var outSkeletonSurfaceFraction = fullRatio - context.PropellantParamsByUnits.SkeletonSurfaceFraction;
        contextBag.OutSkeletonHeatFlux = outSkeletonSurfaceFraction.DecimalFractions
                                         * outSkeletonKineticFlameParams.KineticFlameHeatFlux;
        contextBag.SkeletonHeatFlux = context.PropellantParamsByUnits.SkeletonSurfaceFraction.DecimalFractions
                                      * (contextBag.MetalBurningHeatFlux
                                         + skeletonKineticFlameParams.KineticFlameHeatFlux);
        // AP self-deflagration (monopropellant premixed) flame: a parallel near-surface heat source that, unlike
        // the surface-attached diffusion/kinetic flames, keeps rising with pressure (premixed) — it de-saturates the
        // high-pressure burn rate of the fine-AP-dominated compositions. Weighted by (1−f_c) and gated above the
        // fixed 2 MPa AP deflagration limit (Boggs 1970); q_AP ≡ 0 when K_AP = 0. The bimodal-packing factor carries
        // a (p_ref/p)^a_pack pressure decay (Step 11; LEF importance falls with pressure) for the bimodal compositions.
        contextBag.ToSurfaceTotalHeatFlux = (contextBag.OutSkeletonHeatFlux
                                            + contextBag.SkeletonHeatFlux
                                            + contextBag.DiffusionFlameHeatFlux)
                                            * GetBimodalPackingFactor(
                                                context.Pressure.Pascals,
                                                context.PropellantParamsByUnits.CoarseFraction,
                                                solverParamsByUnits.KBimodalPackingFactor,
                                                solverParamsByUnits.KBimodalPackingPressureExponent)
                                            + HeatFlux.FromWattsPerSquareMeter(GetApPremixedFlameHeatFlux(
                                                context.Pressure.Pascals,
                                                surfaceTemperature.Kelvins,
                                                context.PropellantParamsByUnits.CoarseFraction,
                                                solverParamsByUnits.KApPremixedFactor,
                                                solverParamsByUnits.KApPressureExponent));

        // WSB condensed-phase reaction supplies a fraction θ(p) of the sensible enthalpy (de-saturates the
        // high-pressure burn rate once kinetic flames go surface-attached); θ ≡ 0 when K_wsb = 0.
        var wsbCompleteness = GetWsbCondensedCompleteness(
            context.Pressure.Pascals,
            solverParamsByUnits.KCondensedReactionFactor);
        var enthalpyChange = context.PropellantParamsByUnits.SpecificHeatCapacity
                             * (surfaceTemperature - context.PropellantParamsByUnits.InitialTemperature)
                             * (1.0 - wsbCompleteness)
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
            0.5 * (metalMeltingTemperatureDouble - surfaceTemperature.Kelvins));

        return averageMetalBurningTemperature;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private Length GetSkeletonLayerThickness(
        Speed burnRate,
        CombustionSolverParamsByUnits solverParamsByUnits)
    {
        var aMetalBurningConstant = solverParamsByUnits.AMetalBurningConstant;

        return aMetalBurningConstant / Speed.FromMetersPerSecond(
        Math.Pow(burnRate.MetersPerSecond, solverParamsByUnits.APowOrder));
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private Length GetPoreDiameter(
        Speed burnRate,
        CombustionSolverParamsByUnits solverParamsByUnits)
    {
        var bMetalBurningConstant = solverParamsByUnits.BMetalBurningConstant;

        var poreDiameter = Length.FromMeters(bMetalBurningConstant.CubicMetersPerSquareSecond
            / Math.Pow(burnRate.MetersPerSecond, solverParamsByUnits.BPowOrder));

        return poreDiameter;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private ThermalConductivity GetRadiativeThermalConductivity(
        in Temperature averageMetalBurningTemperature,
        in Length poreDiameter,
        in SkeletonLayerParamsByUnits skeletonLayerParamsByUnits)
    {
        const double stefanBoltzmannConstant = PhysicalConstants.StefanBoltzmannConstant;

        // Rosseland diffusion approximation for radiative thermal conductivity in an
        // optically thick porous medium: λ_r = 16·σ·T³ / (3·β).
        // β is the Rosseland mean extinction coefficient. For a packed bed of opaque
        // particles (Goldsmith–Larkin; Modest, "Radiative Heat Transfer"; Kuo,
        // "Principles of Combustion") it is β = 3·(1 - φ) / d_p,
        // where φ is porosity and d_p is the pore/particle diameter.
        var beta = 3.0 * (1.0 - skeletonLayerParamsByUnits.Porosity.DecimalFractions) / poreDiameter.Meters;
        var radiativeThermalConductivityDouble = 16.0 * stefanBoltzmannConstant
                                                 * Math.Pow(averageMetalBurningTemperature.Kelvins, 3)
                                                 / (3.0 * beta);

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
            / (lambdaGas + 2 * conductiveThermalConductivity)
            )
            + (fullRatio - porosity) * (
                (lambdaCondensed - conductiveThermalConductivity)
                / (lambdaCondensed + 2 * conductiveThermalConductivity)
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
                             * effectiveThermalConductivity.WattsPerMeterKelvin;
        var heatFlux = HeatFlux.FromWattsPerSquareMeter(heatFluxDouble);

        return heatFlux;
    }

    /// <summary>
    /// Computes the diffusion-flame heat flux to the surface using the two-mode petite-ensemble model
    /// (Beckstead–Derr–Price 1970; Cohen &amp; Strand 1982; Glick 1974). A polydisperse AP propellant is
    /// treated as a parallel ensemble of a coarse mode (mass fraction f_c) and a fine mode (1−f_c), each
    /// with its own diffusion standoff h(d,p) = K_h·c_v·ṁ·d²/λ·[1 + C_rxn·(d/d_ref)^m·(p_ref/p)²]:
    ///     q_diff,eff = f_c·λ(T_diff−T_s)/h(d_coarse,p) + (1−f_c)·λ(T_diff−T_s)/h(d_fine,p).
    /// The fine mode (small d) carries a large, weakly pressure-dependent flux that dominates bimodal
    /// blends (raising rate, lowering ν), while the coarse mode (large d) is slow and strongly
    /// pressure-sensitive (steep ν). With C_rxn = 0 every mode reduces to the original laminar standoff.
    /// See docs/research/ap_size_pressure_exponent.md.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private HeatFlux GetDiffusionFlameHeatFlux(
        in Temperature surfaceTemperature,
        in MassFlux decomposeRate,
        in Pressure pressure,
        in CombustionSolverParamsByUnits solverParamsByUnits,
        in DiffusionFlameParamsByUnits diffusionFlameParamsByUnits,
        in PropellantParamsByUnits propellantParamsByUnits,
        out Length effectiveHeight)
    {
        var lambdaGas = diffusionFlameParamsByUnits.ThermalConductivity.WattsPerMeterKelvin;
        var deltaTemperature = (diffusionFlameParamsByUnits.FinalTemperature - surfaceTemperature).Kelvins;
        var volumedSpecificHeatCapacity = diffusionFlameParamsByUnits.VolumetricSpecificHeatCapacity.JoulesPerKilogramKelvin;
        var massFlux = decomposeRate.KilogramsPerSecondPerSquareMeter;
        var kDiffusionHeight = solverParamsByUnits.KDiffusionHeight;
        var kDiffusionPressureFactor = solverParamsByUnits.KDiffusionPressureFactor;
        var sizeExponent = solverParamsByUnits.KDiffusionSizeExponent;
        var pressureExponent = solverParamsByUnits.KDiffusionPressureExponent;
        var pressurePascals = pressure.Pascals;

        GetOxidizerModeDiameters(
            propellantParamsByUnits.AverageOxidizerDiameter.Meters,
            propellantParamsByUnits.CoarseFraction,
            out var coarseDiameter,
            out var fineDiameter,
            out var coarseMassFraction,
            out var fineMassFraction);

        // Mass-fraction weighting of the two flame modes.
        var heatFluxDouble = 0.0;
        if (coarseMassFraction > 0.0)
            heatFluxDouble += coarseMassFraction * ModeHeatFlux(coarseDiameter);
        if (fineMassFraction > 0.0)
            heatFluxDouble += fineMassFraction * ModeHeatFlux(fineDiameter);

        effectiveHeight = Length.FromMeters(
            heatFluxDouble > 0.0 ? lambdaGas * deltaTemperature / heatFluxDouble : double.PositiveInfinity);

        return HeatFlux.FromWattsPerSquareMeter(heatFluxDouble);

        double ModeHeatFlux(double diameter)
        {
            var laminarHeight = kDiffusionHeight * volumedSpecificHeatCapacity * massFlux * diameter * diameter / lambdaGas;
            var height = laminarHeight * GetDiffusionPressureFactor(
                pressurePascals, diameter, kDiffusionPressureFactor, sizeExponent, pressureExponent);
            return lambdaGas * deltaTemperature / height;
        }
    }

    /// <summary>
    /// Splits a propellant's mass-mean oxidizer diameter into a coarse and a fine sub-population for the
    /// two-mode petite-ensemble diffusion model. Pure compositions (f_c = 0 or 1) use the stored average
    /// directly as the single present mode; bimodal blends reconstruct the coarse-class diameter from the
    /// mass-mixing rule d_avg = f_c·d_coarse + (1−f_c)·d_fine with a fixed fine-class diameter. Shared by
    /// the ByUnits and ByDoubles paths so both produce identical numbers.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void GetOxidizerModeDiameters(
        double averageDiameterMeters,
        double coarseFraction,
        out double coarseDiameterMeters,
        out double fineDiameterMeters,
        out double coarseWeight,
        out double fineWeight)
    {
        // Fine AP class diameter of the Babuk pasty-propellant series (50 µm) — the same fine population
        // that, taken pure, forms Bas_3. Used only to split a bimodal average into its sub-populations
        // (user-confirmed blend: 65 %·180 µm + 35 %·50 µm). It is a normalisation constant, not a model
        // parameter, and is never written back to the propellant input data.
        const double fineClassDiameterMeters = 5.0e-5;

        if (coarseFraction <= 0.0)
        {
            // Pure fine population (e.g. Bas_3): the stored average IS the fine-class diameter.
            fineDiameterMeters = averageDiameterMeters;
            coarseDiameterMeters = averageDiameterMeters;
            fineWeight = 1.0;
            coarseWeight = 0.0;
        }
        else if (coarseFraction >= 1.0)
        {
            // Pure coarse population (e.g. Bas_4): the stored average IS the coarse-class diameter.
            coarseDiameterMeters = averageDiameterMeters;
            fineDiameterMeters = averageDiameterMeters;
            coarseWeight = 1.0;
            fineWeight = 0.0;
        }
        else
        {
            // Bimodal blend (e.g. Bas_2/Bas_1/Bas_0): reconstruct the coarse-class diameter from the
            // mass-mixing rule using the fixed fine-class diameter.
            fineDiameterMeters = fineClassDiameterMeters;
            coarseDiameterMeters = (averageDiameterMeters - (1.0 - coarseFraction) * fineDiameterMeters) / coarseFraction;
            if (coarseDiameterMeters < fineDiameterMeters)
                coarseDiameterMeters = fineDiameterMeters;
            coarseWeight = coarseFraction;
            fineWeight = 1.0 - coarseFraction;
        }
    }

    /// <summary>
    /// Per-mode pressure/size modulation of the diffusion-flame standoff (BDP / Lengellé), shared by the
    /// ByUnits and ByDoubles paths so both produce identical numbers:
    ///     factor = 1 + C_rxn · (d / d_ref)^m · (p_ref / p)^n_p,    p_ref = 7 MPa,  d_ref = 100 µm.
    /// With C_rxn (KDiffusionPressureFactor) = 0 the factor is identically 1 — the original laminar standoff.
    /// The size exponent m (KDiffusionSizeExponent) makes coarse modes collapse faster with pressure than
    /// fine modes, giving the size-dependent burn-rate pressure exponent. The pressure exponent n_p
    /// (KDiffusionPressureExponent, bounds [2,4]) sets how steeply the reaction/turbulent standoff contracts
    /// with pressure — n_p = 2 reproduces the original fixed quadratic law, while larger n_p sharpens the
    /// pure-mode burn-rate pressure exponents without restructuring the model (Lengellé–Duterque–Trubert 2000,
    /// regime-dependent standoff exponent; BDP 1970). See docs/research/ap_size_pressure_exponent.md.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static double GetDiffusionPressureFactor(
        double pressurePascals,
        double diameterMeters,
        double kDiffusionPressureFactor,
        double sizeExponent,
        double pressureExponent)
    {
        const double referencePressurePascals = 7.0e6;
        const double referenceDiameterMeters = 1.0e-4; // 100 µm normalisation scale
        var guardedPressure = pressurePascals < 1.0e3 ? 1.0e3 : pressurePascals;
        var pressureRatio = referencePressurePascals / guardedPressure;
        var sizeFactor = Math.Pow(diameterMeters / referenceDiameterMeters, sizeExponent);
        return 1.0 + kDiffusionPressureFactor * sizeFactor * Math.Pow(pressureRatio, pressureExponent);
    }

    /// <summary>
    /// Bimodal-packing heat-feedback enhancement, shared by the ByUnits and ByDoubles paths so both produce
    /// identical numbers:  factor = 1 + K_pack · f_c · (1 − f_c) · (p_ref / p)^a_pack,  p_ref = 1 MPa.
    /// The bimodality measure f_c·(1−f_c) vanishes for monomodal AP (pure coarse f_c = 1 or pure fine
    /// f_c = 0) and is maximal for a balanced blend, so the factor scales up the surface heat feedback —
    /// and hence the burn-rate magnitude — only for bimodal compositions (denser bimodal packing → more
    /// intense, closer flames; Miller 1982; Kubota, "Propellants and Explosives"). Step 11: the enhancement
    /// is given a pressure decay (p_ref/p)^a_pack — the bimodal leading-edge flame (LEF) structure dominates
    /// the heat feedback only while the diffusion flames stand off (low pressure); as pressure rises the flames
    /// collapse toward the surface and the near-surface LEF loses its relative importance, so the bimodal
    /// enhancement weakens (Beckstead–Derr–Price 1970, AIAA J 8(12):2200, doi 10.2514/3.6087; plateau /
    /// particle-size literature: Combust. Expl. Shock Waves 43:436 (2007), doi 10.1007/s10573-007-0059-5;
    /// Combust. Flame 1987 doi 10.1016/0010-2180(87)90099-X and 2015 doi 10.1016/j.combustflame.2015.10.017).
    /// This lifts the LOW-pressure magnitude of the bimodal compositions (pure-bimodal Bas_2 is under-predicted
    /// at 1 MPa) while RELAXING the over-prediction at 4 MPa — a slope correction the pressure-independent factor
    /// cannot make. a_pack (bimodalPackingPressureExponent) ≥ 0 is the shared decay exponent; a_pack = 0
    /// reproduces the pressure-independent factor exactly (cannot regress), and the whole bimodal term is
    /// identically 0 for monomodal Bas_3 (f_c = 0) and Bas_4 (f_c = 1) so they are untouched. With K_pack = 0 the
    /// factor is identically 1. See docs/research/bimodal_packing_pressure_decay.md.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static double GetBimodalPackingFactor(
        double pressurePascals,
        double coarseFraction,
        double kBimodalPackingFactor,
        double bimodalPackingPressureExponent)
    {
        const double referencePressurePascals = 1.0e6; // 1 MPa: low-pressure anchor of the data range
        var guardedPressure = pressurePascals < 1.0e3 ? 1.0e3 : pressurePascals;
        var pressureDecay = Math.Pow(referencePressurePascals / guardedPressure, bimodalPackingPressureExponent);
        return 1.0 + kBimodalPackingFactor * coarseFraction * (1.0 - coarseFraction) * pressureDecay;
    }

    /// <summary>
    /// WSB condensed-phase reaction completeness θ ∈ [0,1), shared by the ByUnits and ByDoubles paths so both
    /// produce identical numbers:  θ = Da / (1 + Da),  Da = K_wsb · (p / p_ref)²,  p_ref = 1 MPa.
    /// The exothermic condensed-phase reaction supplies a fraction θ of the sensible enthalpy, so the net surface
    /// heat demand becomes ṁ·[c_s·(T_s − T_0)·(1 − θ) + ΔH]. As pressure rises the second-order (p²) Damköhler
    /// number grows and θ → 1, letting the burn rate keep climbing once the kinetic flames go surface-attached —
    /// which otherwise saturates the high-pressure (4–6.5 MPa) burn rate. With K_wsb = 0, θ ≡ 0 and the surface
    /// energy balance is unchanged (cannot regress). θ &lt; 1 always, so the sensible sink stays positive and the
    /// surface-temperature bracketing root is preserved. The p² Damköhler is the WSB / Zenin second-order
    /// gas-condensed coupling (Ward–Son–Brewster 1998, Combust. Flame 114:556; Zenin 1995, J. Propul. Power 11:752).
    /// See docs/research/wsb_condensed_phase_closure.md.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static double GetWsbCondensedCompleteness(double pressurePascals, double kCondensedReactionFactor)
    {
        if (kCondensedReactionFactor <= 0.0)
            return 0.0;

        const double referencePressurePascals = 1.0e6; // 1 MPa: low-pressure anchor of the data range
        var guardedPressure = pressurePascals < 1.0e3 ? 1.0e3 : pressurePascals;
        var pressureRatio = guardedPressure / referencePressurePascals;
        var damkohler = kCondensedReactionFactor * pressureRatio * pressureRatio;
        return damkohler / (1.0 + damkohler);
    }

    /// <summary>
    /// Ammonium-perchlorate self-deflagration (monopropellant premixed-flame) heat flux [W·m⁻²] fed back to the
    /// burning surface, shared by the ByUnits and ByDoubles paths so both produce identical numbers:
    ///   q_AP = K_AP · (1 − f_c) · max(0, T_AP − T_s) · max(0, (p / p_dl)^n_AP − 1).
    /// Fine AP self-deflagrates as a monopropellant premixed flame that — unlike the surface-attached
    /// diffusion/kinetic flames — does NOT saturate with pressure, so it de-saturates the 4–6.5 MPa burn rate of the
    /// fine-AP-dominated compositions (pure-fine Bas_3, the post-WSB residual). The gate max(0,(p/p_dl)^n_AP − 1) is
    /// referenced to the fixed AP deflagration limit p_dl = 2 MPa (≈ 20 atm; Boggs 1970, AIAA J 8:867; Price 1984):
    /// it vanishes at/below 2 MPa so the already-correct low-pressure region is untouched. (Step 10 trialled a fitted
    /// slope-break reference p_break here; it was a confirmed null — the optimiser left it at the 2 MPa floor — so the
    /// reference is fixed back to 2 MPa.) The (1 − f_c) weight is the fine-AP surface fraction: coarse AP (f_c = 1,
    /// Bas_4) is diffusion-controlled and gets ZERO contribution, pure-fine AP (f_c = 0, Bas_3) gets the full term.
    /// n_AP (apPressureExponent) is the AP monopropellant pressure exponent, floor 0.77 (Guirao &amp; Williams 1971,
    /// AIAA J 9:1345). T_AP = 1400 K is the AP monopropellant flame temperature (Boggs; Price 1984); on this branch
    /// T_s ≤ 900 K so (T_AP − T_s) &gt; 0. K_AP is a lumped premixed-flame conductance λ_g/δ_AP [W·m⁻²·K⁻¹]; K_AP = 0 ⇒
    /// q_AP ≡ 0 and the model is unchanged (cannot regress the already-fit Bas_4/Bas_2/Bas_1/Bas_0). Added as a
    /// parallel near-surface heat source (outside the bimodal-packing factor).
    /// See docs/research/ap_monopropellant_premixed_flame.md.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static double GetApPremixedFlameHeatFlux(
        double pressurePascals,
        double surfaceTemperatureKelvins,
        double coarseFraction,
        double kApPremixedFactor,
        double apPressureExponent)
    {
        if (kApPremixedFactor <= 0.0)
            return 0.0;

        var fineFraction = 1.0 - coarseFraction;
        if (fineFraction <= 0.0)
            return 0.0;

        const double apFlameTemperatureKelvins = 1400.0; // AP monopropellant flame temperature (Boggs; Price 1984)
        const double deflagrationLimitPascals = 2.0e6;   // AP self-deflagration limit ≈ 20 atm (Boggs 1970)

        var pressureGate = Math.Pow(pressurePascals / deflagrationLimitPascals, apPressureExponent) - 1.0;
        if (pressureGate <= 0.0)
            return 0.0;

        var temperatureDifference = apFlameTemperatureKelvins - surfaceTemperatureKelvins;
        if (temperatureDifference <= 0.0)
            return 0.0;

        return kApPremixedFactor * fineFraction * temperatureDifference * pressureGate;
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
        contextBag.AverageMetalBurningTemperature = Temperature.Zero.Kelvins;
        //    GetAverageMetalBurningTemperature(surfaceTemperature, context.PocketMetalCombustionParams);
        contextBag.SkeletonLayerThickness = GetSkeletonLayerThickness(contextBag.BurnRate, solverParams);
        contextBag.PoreDiameter = GetPoreDiameter(contextBag.BurnRate, solverParams);
        var averageRadiativeTemperatureDouble = solverParams.KCoefficientRadiationTemperature * surfaceTemperature +
            (1.0 - solverParams.KCoefficientRadiationTemperature) * context.PocketSkeletonKineticFlameParams.FinalTemperature;
        contextBag.RadiativeThermalConductivity = GetRadiativeThermalConductivity(
            averageRadiativeTemperatureDouble,
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

        contextBag.DiffusionFlameHeatFlux = GetDiffusionFlameHeatFlux(surfaceTemperature,
                                                                      contextBag.DecomposeRate,
                                                                      context.Pressure,
                                                                      solverParams,
                                                                      context.PocketDiffusionFlameParams,
                                                                      context.PropellantParams,
                                                                      out var diffusionFlameHeight);
        contextBag.DiffusionFlameHeight = diffusionFlameHeight;

        var fullRatio = 1.0;
        var outSkeletonSurfaceFraction = fullRatio - context.PropellantParams.SkeletonSurfaceFraction;
        contextBag.OutSkeletonHeatFlux = outSkeletonSurfaceFraction
                                         * outSkeletonKineticFlameParams.KineticFlameHeatFlux;
        contextBag.SkeletonHeatFlux = context.PropellantParams.SkeletonSurfaceFraction
                                      * (contextBag.MetalBurningHeatFlux
                                         + skeletonKineticFlameParams.KineticFlameHeatFlux);
        // AP self-deflagration (monopropellant premixed) flame: a parallel near-surface heat source that, unlike
        // the surface-attached diffusion/kinetic flames, keeps rising with pressure (premixed) — it de-saturates the
        // high-pressure burn rate of the fine-AP-dominated compositions. Weighted by (1−f_c) and gated above the
        // fixed 2 MPa AP deflagration limit (Boggs 1970); q_AP ≡ 0 when K_AP = 0. The bimodal-packing factor carries
        // a (p_ref/p)^a_pack pressure decay (Step 11; LEF importance falls with pressure) for the bimodal compositions.
        contextBag.ToSurfaceTotalHeatFlux = (contextBag.OutSkeletonHeatFlux
                                            + contextBag.SkeletonHeatFlux
                                            + contextBag.DiffusionFlameHeatFlux)
                                            * GetBimodalPackingFactor(
                                                context.Pressure,
                                                context.PropellantParams.CoarseFraction,
                                                solverParams.KBimodalPackingFactor,
                                                solverParams.KBimodalPackingPressureExponent)
                                            + GetApPremixedFlameHeatFlux(
                                                context.Pressure,
                                                surfaceTemperature,
                                                context.PropellantParams.CoarseFraction,
                                                solverParams.KApPremixedFactor,
                                                solverParams.KApPressureExponent);

        // WSB condensed-phase reaction supplies a fraction θ(p) of the sensible enthalpy (de-saturates the
        // high-pressure burn rate once kinetic flames go surface-attached); θ ≡ 0 when K_wsb = 0.
        var wsbCompleteness = GetWsbCondensedCompleteness(
            context.Pressure,
            solverParams.KCondensedReactionFactor);
        var enthalpyChange = context.PropellantParams.SpecificHeatCapacity
                             * (surfaceTemperature - context.PropellantParams.InitialTemperature)
                             * (1.0 - wsbCompleteness)
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

        var averageMetalBurningTemperature = 0.5 * (metalMeltingTemperatureDouble - surfaceTemperature);

        return averageMetalBurningTemperature;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private double GetSkeletonLayerThickness(
        double burnRate,
        CombustionSolverParamsByDoubles solverParamsByDoubles)
    {
        var aMetalBurningConstant = solverParamsByDoubles.AMetalBurningConstant;

        return aMetalBurningConstant / Math.Pow(burnRate, solverParamsByDoubles.APowOrder);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private double GetPoreDiameter(
        double burnRate,
        CombustionSolverParamsByDoubles solverParamsByDoubles)
    {
        var bMetalBurningConstant = solverParamsByDoubles.BMetalBurningConstant;

        return bMetalBurningConstant / Math.Pow(burnRate, solverParamsByDoubles.BPowOrder);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private double GetRadiativeThermalConductivity(
        double averageMetalBurningTemperature,
        double poreDiameter,
        in SkeletonLayerParamsByDoubles skeletonLayerParamsByDoubles)
    {
        const double stefanBoltzmannConstant = PhysicalConstants.StefanBoltzmannConstant;

        // Rosseland diffusion approximation for radiative thermal conductivity in an
        // optically thick porous medium: λ_r = 16·σ·T³ / (3·β).
        // β is the Rosseland mean extinction coefficient. For a packed bed of opaque
        // particles (Goldsmith–Larkin; Modest, "Radiative Heat Transfer"; Kuo,
        // "Principles of Combustion") it is β = 3·(1 - φ) / d_p,
        // where φ is porosity and d_p is the pore/particle diameter.
        var beta = 3.0 * (1.0 - skeletonLayerParamsByDoubles.Porosity) / poreDiameter;
        var radiativeThermalConductivity = 16.0 * stefanBoltzmannConstant
                                                 * Math.Pow(averageMetalBurningTemperature, 3)
                                                 / (3.0 * beta);

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
            / (lambdaGas + 2 * conductiveThermalConductivity)
            )
            + (1.0 - porosity) * (
                (lambdaCondensed - conductiveThermalConductivity)
                / (lambdaCondensed + 2 * conductiveThermalConductivity)
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
                             * effectiveThermalConductivity;

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
    /// <summary>
    /// Two-mode petite-ensemble diffusion-flame heat flux (ByDoubles mirror of the ByUnits overload) —
    /// see that method and docs/research/ap_size_pressure_exponent.md for the full description. Uses the
    /// same shared static helpers <see cref="GetOxidizerModeDiameters"/> and
    /// <see cref="GetDiffusionPressureFactor"/> so the two paths produce identical numbers (§17.8 parity).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private double GetDiffusionFlameHeatFlux(
        double surfaceTemperature,
        double decomposeRate,
        double pressure,
        in CombustionSolverParamsByDoubles solverParams,
        in DiffusionFlameParamsByDoubles diffusionFlameParams,
        in PropellantParamsByDoubles propellantParams,
        out double effectiveHeight)
    {
        var lambdaGas = diffusionFlameParams.ThermalConductivity;
        var deltaTemperature = diffusionFlameParams.FinalTemperature - surfaceTemperature;
        var volumedSpecificHeatCapacity = diffusionFlameParams.VolumetricSpecificHeatCapacity;
        var massFlux = decomposeRate;
        var kDiffusionHeight = solverParams.KDiffusionHeight;
        var kDiffusionPressureFactor = solverParams.KDiffusionPressureFactor;
        var sizeExponent = solverParams.KDiffusionSizeExponent;
        var pressureExponent = solverParams.KDiffusionPressureExponent;

        GetOxidizerModeDiameters(
            propellantParams.AverageOxidizerDiameter,
            propellantParams.CoarseFraction,
            out var coarseDiameter,
            out var fineDiameter,
            out var coarseMassFraction,
            out var fineMassFraction);

        // Mass-fraction weighting of the two flame modes.
        var heatFluxDouble = 0.0;
        if (coarseMassFraction > 0.0)
            heatFluxDouble += coarseMassFraction * ModeHeatFlux(coarseDiameter);
        if (fineMassFraction > 0.0)
            heatFluxDouble += fineMassFraction * ModeHeatFlux(fineDiameter);

        effectiveHeight = heatFluxDouble > 0.0
            ? lambdaGas * deltaTemperature / heatFluxDouble
            : double.PositiveInfinity;

        return heatFluxDouble;

        double ModeHeatFlux(double diameter)
        {
            var laminarHeight = kDiffusionHeight * volumedSpecificHeatCapacity * massFlux * diameter * diameter / lambdaGas;
            var height = laminarHeight * GetDiffusionPressureFactor(
                pressure, diameter, kDiffusionPressureFactor, sizeExponent, pressureExponent);
            return lambdaGas * deltaTemperature / height;
        }
    }

#endregion
}
