using UnitsNet;

namespace ParametricCombustionModel.Computation.Models.ComputedParams;

public struct PocketCombustionParams
{
#region Fields

    public bool BurnRateIsFound;

    public Temperature SurfaceTemperature;
    public Speed BurnRate;
    public MassFlux DecomposeRate;

    public KineticFlameCombustionParams OutSkeletonKineticFlameCombustionParams;

    public KineticFlameCombustionParams SkeletonKineticFlameCombustionParams;

#region Inside Skeleton Metal Burning Parameters

    public Temperature AverageMetalBurningTemperature;
    public HeatFlux MetalBurningHeatFlux;

#endregion

#region Diffusion Flame Parameters

    public Length DiffusionFlameHeight;
    public HeatFlux DiffusionFlameHeatFlux;

#endregion

    public HeatFlux OutSkeletonHeatFlux;
    public HeatFlux SkeletonHeatFlux;

    public HeatFlux ToSurfaceTotalHeatFlux;
    public HeatFlux SublimationHeatFlux;
    
    public HeatFlux SurfaceHeatFluxesError;

#endregion
}
