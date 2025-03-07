using UnitsNet;

namespace ParametricCombustionModel.Computation.Models.KnownParams;

#region Utilization of Double

public readonly struct SkeletonLayerParamsByDoubles
{
    public required double Porosity { get; init; }

    public required double CondensedThermalConductivity { get; init; }
}

#endregion

#region Utilization of UnitsNet

public struct SkeletonLayerParamsByUnits
{
    public required Ratio Porosity { get; init; }

    public required ThermalConductivity CondensedThermalConductivity { get; init; }
}

#endregion
