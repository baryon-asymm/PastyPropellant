using UnitsNet;

namespace ParametricCombustionModel.Computation.Models.ComputedParams;

public struct KineticFlameCombustionParams
{
#region Fields

    public Length KineticFlameHeight;
    public Density AverageKineticFlameDensity;
    public Temperature AverageKineticFlameTemperature;
    public HeatFlux KineticFlameHeatFlux;

#endregion
}
