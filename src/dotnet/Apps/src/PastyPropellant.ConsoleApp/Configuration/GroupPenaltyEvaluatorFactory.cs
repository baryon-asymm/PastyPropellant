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
/// <para>The thresholds are model configuration, not tuning knobs to be varied per run. They are
/// nonetheless overridable through <see cref="PenaltyConfiguration"/> so that an experiment does not
/// require a recompile — the defaults on that record are the historical literals, and whatever a run
/// actually used is recorded in the resolved-run sidecar and the PDF report.</para>
/// </summary>
public static class GroupPenaltyEvaluatorFactory
{
    /// <summary>
    /// Builds the five constraint-penalty evaluators, in the order the fitness aggregation applies them:
    /// pocket heat-flux ratio competition, inter-pocket faster burn, kinetic-flame heat flux,
    /// pore diameter, and large oxidiser particle size.
    /// </summary>
    /// <param name="configuration">
    /// Thresholds to apply. Omit for the built-in defaults, which are the values that were previously
    /// hardcoded here.
    /// </param>
    public static IPenaltyEvaluator[] Build(PenaltyConfiguration? configuration = null)
    {
        var penalties = configuration ?? new PenaltyConfiguration();

        var penaltyRate = penalties.PenaltyRate;
        var heatFluxRatioThreshold = penalties.HeatFluxRatioThreshold;
        var poreDiameterThreshold = penalties.PoreDiameterThreshold;
        var largeOxidizerParticleSizeThreshold = penalties.LargeOxidizerParticleSizeThreshold;

        var maxInterPocketKineticFlameHeatFlux = HeatFlux.FromWattsPerSquareMeter(
            penalties.MaxInterPocketKineticFlameHeatFluxWattsPerSquareMeter);
        var maxSkeletonKineticFlameHeatFlux = HeatFlux.FromWattsPerSquareMeter(
            penalties.MaxSkeletonKineticFlameHeatFluxWattsPerSquareMeter);
        var maxOutSkeletonKineticFlameHeatFlux = HeatFlux.FromWattsPerSquareMeter(
            penalties.MaxOutSkeletonKineticFlameHeatFluxWattsPerSquareMeter);

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
