using UnitsNet;

namespace ParametricCombustionModel.Computation.Models.ComputedParams;

public struct InterPocketCombustionParams
{
#region Fields

    public bool BurnRateIsFound;

    public Temperature SurfaceTemperature;
    public Speed BurnRate;
    public MassFlux DecomposeRate;

    public KineticFlameCombustionParams KineticFlameCombustionParams;

    public HeatFlux SublimationHeatFlux;
    
    public HeatFlux SurfaceHeatFluxesError;

#endregion
}
