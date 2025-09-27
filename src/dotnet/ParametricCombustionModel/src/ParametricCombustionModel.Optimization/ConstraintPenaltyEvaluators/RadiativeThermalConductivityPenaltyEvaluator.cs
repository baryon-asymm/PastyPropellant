using System.Runtime.CompilerServices;
using ParametricCombustionModel.Computation.Models.ProblemContexts;

namespace ParametricCombustionModel.Optimization.ConstraintPenaltyEvaluators;

public sealed class RadiativeThermalConductivityPenaltyEvaluator : BaseConstraintPenaltyEvaluator
{
    public RadiativeThermalConductivityPenaltyEvaluator(
        double penaltyRate)
        : base(penaltyRate) {}

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public override double GetPenaltyValue(
        ProblemContextByUnits updatedProblemContext)
    {
        var radiativeThermalConductivity = updatedProblemContext.PocketCombustionParams.RadiativeThermalConductivity.WattsPerMeterKelvin;
        var conductiveThermalConductivity = updatedProblemContext.PocketCombustionParams.ConductiveThermalConductivity.WattsPerMeterKelvin;
        if (radiativeThermalConductivity < conductiveThermalConductivity)
            return PenaltyRate * (conductiveThermalConductivity / radiativeThermalConductivity);

        return ZeroPenaltyValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public override double GetPenaltyValue(
        ProblemContextByDoubles updatedProblemContext)
    {
        var radiativeThermalConductivity = updatedProblemContext.PocketCombustionParams.RadiativeThermalConductivity;
        var conductiveThermalConductivity = updatedProblemContext.PocketCombustionParams.ConductiveThermalConductivity;
        if (radiativeThermalConductivity < conductiveThermalConductivity)
            return PenaltyRate * (conductiveThermalConductivity / radiativeThermalConductivity);

        return ZeroPenaltyValue;
    }
}
