using System.Runtime.CompilerServices;
using ParametricCombustionModel.Computation.Models.ProblemContexts;
using UnitsNet;

namespace ParametricCombustionModel.Optimization.ConstraintPenaltyEvaluators;

public class KineticFlameHeatFluxPenaltyEvaluator : BaseConstraintPenaltyEvaluator
{
#region Properties

    public double MaxInterPocketKineticFlameHeatFlux { get; init; }
    public double MaxSkeletonKineticFlameHeatFlux { get; init; }
    public double MaxOutSkeletonKineticFlameHeatFlux { get; init; }

#endregion

#region Constructors

    public KineticFlameHeatFluxPenaltyEvaluator(
        double penaltyRate,
        HeatFlux maxInterPocketKineticFlameHeatFlux,
        HeatFlux maxSkeletonKineticFlameHeatFlux,
        HeatFlux maxOutSkeletonKineticFlameHeatFlux)
        : base(penaltyRate)
    {
        if (maxInterPocketKineticFlameHeatFlux.WattsPerSquareMeter <= 0)
            throw new ArgumentException("Max inter-pocket kinetic flame heat flux must be greater than 0");

        if (maxSkeletonKineticFlameHeatFlux.WattsPerSquareMeter <= 0)
            throw new ArgumentException("Max skeleton kinetic flame heat flux must be greater than 0");

        if (maxOutSkeletonKineticFlameHeatFlux.WattsPerSquareMeter <= 0)
            throw new ArgumentException("Max out-skeleton kinetic flame heat flux must be greater than 0");

        MaxInterPocketKineticFlameHeatFlux = maxInterPocketKineticFlameHeatFlux.WattsPerSquareMeter;
        MaxSkeletonKineticFlameHeatFlux = maxSkeletonKineticFlameHeatFlux.WattsPerSquareMeter;
        MaxOutSkeletonKineticFlameHeatFlux = maxOutSkeletonKineticFlameHeatFlux.WattsPerSquareMeter;
    }

    public KineticFlameHeatFluxPenaltyEvaluator(
        double penaltyRate,
        double maxInterPocketKineticFlameHeatFlux,
        double maxSkeletonKineticFlameHeatFlux,
        double maxOutSkeletonKineticFlameHeatFlux)
        : base(penaltyRate)
    {
        if (maxInterPocketKineticFlameHeatFlux <= 0)
            throw new ArgumentException("Max inter-pocket kinetic flame heat flux must be greater than 0");

        if (maxSkeletonKineticFlameHeatFlux <= 0)
            throw new ArgumentException("Max skeleton kinetic flame heat flux must be greater than 0");

        if (maxOutSkeletonKineticFlameHeatFlux <= 0)
            throw new ArgumentException("Max out-skeleton kinetic flame heat flux must be greater than 0");

        MaxInterPocketKineticFlameHeatFlux = maxInterPocketKineticFlameHeatFlux;
        MaxSkeletonKineticFlameHeatFlux = maxSkeletonKineticFlameHeatFlux;
        MaxOutSkeletonKineticFlameHeatFlux = maxOutSkeletonKineticFlameHeatFlux;
    }

#endregion

#region Overridden Methods

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public override double GetPenaltyValue(
        ProblemContextByUnits updatedProblemContext)
    {
        var penaltyValue = ZeroPenaltyValue;
        ref var interPocketParams = ref updatedProblemContext.InterPocketCombustionParams;
        ref var interPocketKineticFlameParams = ref interPocketParams.KineticFlameCombustionParams;

        ref var pocketParams = ref updatedProblemContext.PocketCombustionParams;
        ref var skeletonKineticFlameParams = ref pocketParams.SkeletonKineticFlameCombustionParams;
        ref var outSkeletonKineticFlameParams = ref pocketParams.OutSkeletonKineticFlameCombustionParams;

        HandleKineticFlameHeatFluxAndPenaltyValue(
            interPocketKineticFlameParams.KineticFlameHeatFlux.WattsPerSquareMeter,
            MaxInterPocketKineticFlameHeatFlux,
            ref penaltyValue);
        HandleKineticFlameHeatFluxAndPenaltyValue(
            skeletonKineticFlameParams.KineticFlameHeatFlux.WattsPerSquareMeter,
            MaxSkeletonKineticFlameHeatFlux,
            ref penaltyValue);
        HandleKineticFlameHeatFluxAndPenaltyValue(
            outSkeletonKineticFlameParams.KineticFlameHeatFlux.WattsPerSquareMeter,
            MaxOutSkeletonKineticFlameHeatFlux,
            ref penaltyValue);

        return penaltyValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public override double GetPenaltyValue(
        ProblemContextByDoubles updatedProblemContext)
    {
        var penaltyValue = ZeroPenaltyValue;
        ref var interPocketParams = ref updatedProblemContext.InterPocketCombustionParams;
        ref var interPocketKineticFlameParams = ref interPocketParams.KineticFlameCombustionParams;

        ref var pocketParams = ref updatedProblemContext.PocketCombustionParams;
        ref var skeletonKineticFlameParams = ref pocketParams.SkeletonKineticFlameCombustionParams;
        ref var outSkeletonKineticFlameParams = ref pocketParams.OutSkeletonKineticFlameCombustionParams;

        HandleKineticFlameHeatFluxAndPenaltyValue(
            interPocketKineticFlameParams.KineticFlameHeatFlux,
            MaxInterPocketKineticFlameHeatFlux,
            ref penaltyValue);
        HandleKineticFlameHeatFluxAndPenaltyValue(
            skeletonKineticFlameParams.KineticFlameHeatFlux,
            MaxSkeletonKineticFlameHeatFlux,
            ref penaltyValue);
        HandleKineticFlameHeatFluxAndPenaltyValue(
            outSkeletonKineticFlameParams.KineticFlameHeatFlux,
            MaxOutSkeletonKineticFlameHeatFlux,
            ref penaltyValue);

        return penaltyValue;
    }

#endregion

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private void HandleKineticFlameHeatFluxAndPenaltyValue(
        double kineticFlameHeatFlux,
        double maxKineticFlameHeatFlux,
        ref double penaltyValue)
    {
        if (kineticFlameHeatFlux > maxKineticFlameHeatFlux)
            penaltyValue += PenaltyRate * (kineticFlameHeatFlux / maxKineticFlameHeatFlux);
    }
}
