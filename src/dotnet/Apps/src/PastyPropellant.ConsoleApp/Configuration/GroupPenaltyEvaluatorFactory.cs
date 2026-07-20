using ParametricCombustionModel.Optimization.ConstraintPenaltyEvaluators;
using ParametricCombustionModel.Optimization.ConstraintPenaltyEvaluators.Interfaces;
using UnitsNet;

namespace PastyPropellant.ConsoleApp.Configuration;

/// <summary>
/// Single source of the physical-constraint penalty set applied to a group evaluation.
///
/// <para>This exists as one type with one method for a load-bearing reason: the optimisation run and
/// the <c>--forward-eval</c> replay must report <em>identical</em> penalties for the same point. If the
/// two paths ever built their own evaluator lists, a threshold changed on one side would silently make
/// a replayed vector score differently from the run that produced it, and the discrepancy would look
/// like a solver bug rather than a configuration split. Both paths call <see cref="Build"/>; neither
/// constructs an evaluator itself.</para>
///
/// <para>The thresholds here are model configuration, not tuning knobs to be varied per run.</para>
/// </summary>
public static class GroupPenaltyEvaluatorFactory
{
    /// <summary>
    /// Builds the five constraint-penalty evaluators, in the order the fitness aggregation applies them:
    /// pocket heat-flux ratio competition, inter-pocket faster burn, kinetic-flame heat flux,
    /// pore diameter, and large oxidiser particle size.
    /// </summary>
    public static IPenaltyEvaluator[] Build()
    {
        const double penaltyRate = 0.01;
        const double heatFluxRatioThreshold = 100.0;
        const double poreDiameterThreshold = 3.0;
        const double largeOxidizerParticleSizeThreshold = 1.0;

        var maxInterPocketKineticFlameHeatFlux = HeatFlux.FromWattsPerSquareMeter(1e9);
        var maxSkeletonKineticFlameHeatFlux = HeatFlux.FromWattsPerSquareMeter(1e8);
        var maxOutSkeletonKineticFlameHeatFlux = HeatFlux.FromWattsPerSquareMeter(1e8);

        return [
            new PocketHeatFluxRatioCompetitionPenaltyEvaluator(penaltyRate, heatFluxRatioThreshold),
            new InterPocketFasterBurnPenaltyEvaluator(penaltyRate),
            new KineticFlameHeatFluxPenaltyEvaluator(
                penaltyRate: penaltyRate,
                maxInterPocketKineticFlameHeatFlux: maxInterPocketKineticFlameHeatFlux,
                maxSkeletonKineticFlameHeatFlux: maxSkeletonKineticFlameHeatFlux,
                maxOutSkeletonKineticFlameHeatFlux: maxOutSkeletonKineticFlameHeatFlux),
            new PoreDiameterPenaltyEvaluator(penaltyRate, poreDiameterThreshold),
            new LargeOxidizerParticleSizePenaltyEvaluator(penaltyRate, largeOxidizerParticleSizeThreshold)
        ];
    }
}
