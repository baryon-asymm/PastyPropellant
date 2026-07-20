using PastyPropellant.ConsoleApp.Configuration;
using PastyPropellant.ConsoleApp.Io;
using PastyPropellant.ConsoleApp.Reporting;
using PastyPropellant.ConsoleApp.Runners;
using PastyPropellant.Core.Models;

namespace PastyPropellant.ConsoleApp.Workflows;

/// <summary>
/// Forward-eval: solve every (fuel, pressure) context for a SINGLE supplied group vector — no DE
/// search — and emit the same report/plots as an optimization run. Seconds instead of ~4 hours.
///
/// <para>This is the replay path for <c>best_vector.txt</c>, and its whole value rests on producing
/// bit-identical numbers to the run that wrote that file. It gets there by construction rather than by
/// agreement: the bounds, the penalty set, the scenario and the report all come from the same types the
/// optimisation path uses (<see cref="BoundsProvider"/>, <see cref="GroupPenaltyEvaluatorFactory"/>,
/// <see cref="GroupScenarioRunner"/>, <see cref="GroupReportGenerator"/>). Nothing about the evaluation
/// is configured here.</para>
/// </summary>
public static class ForwardEvalWorkflow
{
    /// <summary>Replays <paramref name="vectorFilePath"/> against <paramref name="inputFileName"/>.</summary>
    public static async Task RunAsync(string inputFileName, string vectorFilePath)
    {
        Console.WriteLine(new string('=', 90));
        Console.WriteLine("FORWARD EVALUATION (single parameter vector — no optimization)");
        Console.WriteLine(new string('=', 90) + "\n");

        var genes = VectorFileService.Read(vectorFilePath);
        if (genes.Length != BoundsProvider.GroupVectorLength)
            throw new ArgumentException(
                $"Vector file '{vectorFilePath}' has {genes.Length} values; expected {BoundsProvider.GroupVectorLength} (group vector).");

        var plan = GroupScenarioRunner.CreateForwardEvalPlan(inputFileName);

        Console.WriteLine($"- Input file: {inputFileName}");
        Console.WriteLine($"- Vector file: {vectorFilePath} ({genes.Length} parameters)\n");

        var result = GroupScenarioRunner.EvaluateVector(plan, genes);

        Console.WriteLine($"  Aggregated fitness: {result.AggregatedFitness:E4}");
        Console.WriteLine($"  Total aggregated penalty: {result.TotalAggregatedPenalty:E4}\n");

        var compositionNames = CompositionGroups.ConsoleNames;
        Console.WriteLine("Individual Results:");
        for (int i = 0; i < CompositionGroups.Count; i++)
        {
            Console.WriteLine($"  {compositionNames[i]}:");
            Console.WriteLine($"    - Fitness: {result.IndividualFitnesses[i]:E4}");
            Console.WriteLine($"    - Penalty: {result.IndividualPenalties[i]:E4}");
        }

        // Same report pipeline as the optimization path.
        Console.WriteLine("\nRendering burning rate plots...");
        ResultArtifactsRenderer.RenderBurnRatePlot(
            result, BurnRatePlotSettingsFactory.ForwardEvalRunLabel);
        ResultArtifactsRenderer.RenderBurnRateLogLogPlot(
            result, BurnRatePlotSettingsFactory.ForwardEvalRunLabel);

        Console.WriteLine("Rendering Python plots...");
        await ResultArtifactsRenderer.RenderPythonPlotsAsync(inputFileName);

        Console.WriteLine("Rendering skeleton-layer plots...");
        var skeletonPlotsResult = await ResultArtifactsRenderer.RenderSkeletonLayerPlotsAsync(result);
        if (!skeletonPlotsResult.IsSuccess)
            Console.WriteLine($"⚠ Skeleton-layer plots failed: {skeletonPlotsResult.Exception?.Message}");

        Console.WriteLine("Generating PDF report...");
        GroupReportGenerator.Generate(result, inputFileName, "en-US", "en",
            plan.Settings.DifferentialEvolutionSettings, plan.Meter);

        Console.WriteLine("\n✓ Forward-evaluation report generated\n");
    }
}
