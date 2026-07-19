using System.Globalization;
using ParametricCombustionModel.Optimization.Results;
using ParametricCombustionModel.ReportMaking.Enums;
using ParametricCombustionModel.ReportMaking.Interfaces;
using ParametricCombustionModel.ReportMaking.PdfOperations;

namespace ParametricCombustionModel.ReportMaking.Reports.Pdf;

/// <summary>
/// Prints, for every fuel at every pressure, the flame structure the solver produced: the heat-flux
/// decomposition of the total feedback to the surface, the flame heights, and the surface / metal
/// temperatures. This is the human-facing view of the same fields that
/// <c>SkeletonLayerPlotsHelper</c> serialises to <c>skeleton_layer.json</c> and plots — but printed
/// as numbers, per point, in the PDF so the report is self-contained.
///
/// The heat feedback to the surface splits exactly into three additive competing-flame pathways
/// (see <c>PocketPropellantSolver</c>: ToSurfaceTotal = OutSkeleton + Skeleton + Diffusion), so the
/// three shares printed below sum to 100% by construction. "Skeleton" is the surface-fraction-weighted
/// sum of metal burning and the skeleton kinetic flame inside the pores; "out-kin" is the out-of-skeleton
/// kinetic flame; "diffusion" is the diffusion flame above the pocket. Sublimation is the surface heat
/// sink and is not part of the feedback split. The per-fuel "mean shares" line is the pressure-average of
/// each pathway's share — the competing-flames verdict for that fuel.
///
/// Works on the grouped result directly so it uses the same composition ordering as the rest of the group
/// report.
/// </summary>
public class FlameStructureReport : PerCompositionPerFuelPdfReport
{
    // Same composition ordering / naming as GroupCombustionSolverParamsReport / BurnRateErrorReport.
    private static readonly string[] CompositionNamesValue =
        ["Bas_2 (includes Bas_3, Bas_4)", "Bas_1", "Bas_0"];

    public FlameStructureReport(GroupOptimizationResult groupResult) : base(groupResult)
    {
    }

    protected override IReadOnlyList<string> CompositionNames => CompositionNamesValue;

    protected override string Title => "Flame Structure & Heat-Flux Decomposition";

    protected override string Introduction =>
        "q = total heat feedback to the surface (MW/m2). It splits exactly into three competing-flame "
        + "pathways whose shares sum to 100%: out-kin = out-of-skeleton kinetic flame; skeleton = "
        + "surface-weighted (metal burning + skeleton kinetic flame in the pores); diff = diffusion "
        + "flame. h = flame heights (um). Ts_p / Ts_i = pocket / inter-pocket surface temperature "
        + "(the two-temperature surface). Mean shares are the pressure-average per fuel. Numbers below "
        + "reflect this report's input vector.";

    protected override string CompositionSectionSuffix => "Flame Structure";

    protected override void AppendFuel(
        Queue<IPdfOperation> operations,
        Optimization.Models.OptimizationProblemByUnits context,
        int fuel)
    {
        operations.Enqueue(new PrintTextOperation(
            context.ProblemContextMatrix[fuel, 0].Propellant.Name, TextStyle.Italic));
        operations.Enqueue(new LineBreakOperation());

        var sumOutShare = 0.0;
        var sumSkelShare = 0.0;
        var sumDiffShare = 0.0;
        var convergedPointCount = 0;

        for (var pressure = 0; pressure < context.PressureCount; pressure++)
        {
            var ctx = context.ProblemContextMatrix[fuel, pressure];
            var pocket = ctx.PocketCombustionParams;
            var pressureMpa = ctx.Pressure.As(UnitsNet.Units.PressureUnit.Megapascal);

            var total = pocket.ToSurfaceTotalHeatFlux.WattsPerSquareMeter;
            operations.Enqueue(new AddTabOperation());

            if (double.IsFinite(total) == false || total <= 0.0)
            {
                operations.Enqueue(new PrintTextOperation(
                    string.Format(CultureInfo.InvariantCulture,
                        "p {0:0.###} MPa: non-converged (no surface heat feedback)", pressureMpa),
                    TextStyle.None));
                operations.Enqueue(new LineBreakOperation());
                continue;
            }

            var outShare = pocket.OutSkeletonHeatFlux.WattsPerSquareMeter / total * 100.0;
            var skelShare = pocket.SkeletonHeatFlux.WattsPerSquareMeter / total * 100.0;
            var diffShare = pocket.DiffusionFlameHeatFlux.WattsPerSquareMeter / total * 100.0;

            sumOutShare += outShare;
            sumSkelShare += skelShare;
            sumDiffShare += diffShare;
            convergedPointCount++;

            // Headline: pressure, total feedback, three competing-flame shares (sum to 100%).
            operations.Enqueue(new PrintTextOperation(
                string.Format(CultureInfo.InvariantCulture,
                    "p {0:0.###} MPa: q = {1:0.00} MW/m2; out-kin {2:0.0}%, skeleton {3:0.0}%, diff {4:0.0}%",
                    pressureMpa, total / 1e6, outShare, skelShare, diffShare),
                TextStyle.None));
            operations.Enqueue(new LineBreakOperation());

            // Structure: flame heights and surface / metal temperatures.
            operations.Enqueue(new AddTabOperation());
            operations.Enqueue(new PrintTextOperation(
                string.Format(CultureInfo.InvariantCulture,
                    "h[um] skel-kin {0:0.0}, out-kin {1:0.0}, diff {2:0.0}; Ts_p {3:0} K, Ts_i {4:0} K",
                    pocket.SkeletonKineticFlameCombustionParams.KineticFlameHeight.Micrometers,
                    pocket.OutSkeletonKineticFlameCombustionParams.KineticFlameHeight.Micrometers,
                    pocket.DiffusionFlameHeight.Micrometers,
                    pocket.SurfaceTemperature.Kelvins,
                    ctx.InterPocketCombustionParams.SurfaceTemperature.Kelvins),
                TextStyle.None));
            operations.Enqueue(new LineBreakOperation());
        }

        operations.Enqueue(new AddTabOperation());
        if (convergedPointCount == 0)
        {
            operations.Enqueue(new PrintTextOperation(
                "mean shares: non-converged (no surface heat feedback at any pressure)", TextStyle.Bold));
        }
        else
        {
            operations.Enqueue(new PrintTextOperation(
                string.Format(CultureInfo.InvariantCulture,
                    "mean shares: out-kin {0:0.0}%, skeleton {1:0.0}%, diffusion {2:0.0}%",
                    sumOutShare / convergedPointCount,
                    sumSkelShare / convergedPointCount,
                    sumDiffShare / convergedPointCount),
                TextStyle.Bold));
        }

        operations.Enqueue(new LineBreakOperation());
    }
}
