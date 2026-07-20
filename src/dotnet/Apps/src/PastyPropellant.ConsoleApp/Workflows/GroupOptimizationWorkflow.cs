using PastyPropellant.ConsoleApp.Io;
using PastyPropellant.ConsoleApp.Reporting;
using PastyPropellant.ConsoleApp.Runners;
using PastyPropellant.Core.Models;

namespace PastyPropellant.ConsoleApp.Workflows;

/// <summary>
/// The full optimisation campaign as the console host performs it: configure and run the search,
/// persist the winning vector, then render every output artefact.
///
/// <para>This type owns the console narration and nothing else — every decision it reports was made by
/// <see cref="GroupScenarioRunner"/>, and every artefact it announces is rendered by
/// <see cref="ResultArtifactsRenderer"/>. The console text and its ordering are the host's observable
/// contract (run logs from earlier campaigns are compared against new ones), so this reads as a
/// deliberately literal transcript rather than a loop over a list of steps.</para>
/// </summary>
public static class GroupOptimizationWorkflow
{
    /// <summary>File the winning 32-vector is persisted to, and the argument <c>--forward-eval</c> replays.</summary>
    public const string BestVectorFileName = "best_vector.txt";

    /// <summary>Runs the campaign end to end against <paramref name="inputFileName"/>.</summary>
    public static async Task RunAsync(string inputFileName)
    {
        Console.WriteLine(new string('=', 90));
        Console.WriteLine("GROUP OPTIMIZATION: BAS_0, BAS_1, BAS_2+BAS_3+BAS_4 (SIMULTANEOUS)");
        Console.WriteLine(new string('=', 90) + "\n");

        var plan = GroupScenarioRunner.CreateOptimizationPlan(inputFileName);
        WriteRunBanner(plan, inputFileName);

        var operationResult = await GroupScenarioRunner.RunAsync(plan);

        if (operationResult == null || !operationResult.IsSuccess)
        {
            Console.WriteLine("\n❌ Group optimization failed:");
            if (operationResult != null)
                Console.WriteLine(operationResult.Exception);
            return;
        }

        var groupResult = operationResult.Value;

        Console.WriteLine("\n✓ Group optimization completed successfully");
        Console.WriteLine($"  Aggregated fitness: {groupResult!.AggregatedFitness:E4}");
        Console.WriteLine($"  Total aggregated penalty: {groupResult.TotalAggregatedPenalty:E4}\n");

        // Display results for each composition (order matches the raw 32-vector layout)
        var compositionNames = CompositionGroups.ConsoleNames;
        Console.WriteLine("Individual Results:");
        for (int i = 0; i < CompositionGroups.Count; i++)
        {
            Console.WriteLine($"  {compositionNames[i]}:");
            Console.WriteLine($"    - Fitness: {groupResult.IndividualFitnesses[i]:E4}");
            Console.WriteLine($"    - Penalty: {groupResult.IndividualPenalties[i]:E4}");
        }

        // Persist the best vector at full precision so this exact run can be replayed via --forward-eval.
        VectorFileService.Write(BestVectorFileName, groupResult.BestParams);
        Console.WriteLine($"\n✓ Best vector saved to {BestVectorFileName} (replay: --forward-eval {BestVectorFileName})\n");

        // Render burning rate plots for each group
        Console.WriteLine("\nRendering burning rate plots...");
        ResultArtifactsRenderer.RenderBurnRatePlot(
            groupResult, BurnRatePlotSettingsFactory.OptimizationRunLabel);
        Console.WriteLine("✓ Group burning rate plot rendered\n");

        ResultArtifactsRenderer.RenderBurnRateLogLogPlot(
            groupResult, BurnRatePlotSettingsFactory.OptimizationRunLabel);
        Console.WriteLine("✓ Group burning rate log-log plot rendered\n");

        // Render Python plots
        Console.WriteLine("Rendering Python plots...");
        await ResultArtifactsRenderer.RenderPythonPlotsAsync(inputFileName);
        Console.WriteLine("✓ Python plots rendered\n");

        // Render skeleton-layer plots from the fitted solver outputs (JSON sidecar)
        Console.WriteLine("Rendering skeleton-layer plots...");
        var skeletonPlotsResult = await ResultArtifactsRenderer.RenderSkeletonLayerPlotsAsync(groupResult);
        if (skeletonPlotsResult.IsSuccess)
            Console.WriteLine("✓ Skeleton-layer plots rendered\n");
        else
            Console.WriteLine($"⚠ Skeleton-layer plots failed: {skeletonPlotsResult.Exception?.Message}\n");

        // Generate PDF report
        Console.WriteLine("Generating PDF report...");
        GroupReportGenerator.Generate(groupResult, inputFileName, "en-US", "en",
            plan.Settings.DifferentialEvolutionSettings, plan.Meter);
        Console.WriteLine("✓ PDF report generated\n");

        WriteSummary();
    }

    /// <summary>Echoes the configuration the run is actually about to use.</summary>
    private static void WriteRunBanner(GroupOptimizationPlan plan, string inputFileName)
    {
        Console.WriteLine("Starting group optimization (Bas_0, Bas_1, Bas_2+Bas_3+Bas_4):");
        Console.WriteLine($"- Input file: {inputFileName}");
        Console.WriteLine($"- Algorithm: {plan.Settings.DifferentialEvolutionSettings.Strategy}");
        Console.WriteLine($"- Population size: {plan.PopulationSize}");
        if (plan.MaxEvaluationNumber.HasValue)
            Console.WriteLine($"- Evaluation budget: {plan.MaxEvaluationNumber.Value:N0}");
        if (plan.PBestRate.HasValue && plan.ArchiveSizeRate.HasValue && plan.MemorySize.HasValue)
            Console.WriteLine($"- L-SHADE control: p-best={plan.PBestRate.Value}, archiveRate={plan.ArchiveSizeRate.Value}, memory={plan.MemorySize.Value}");
        Console.WriteLine($"- Parameter vector size: 32 (11 shared + 7×3 specific)");
        Console.WriteLine($"- Processors: {plan.ProcessorsCount}");

        var nelderMeadRefinement = plan.NelderMeadRefinement;
        if (nelderMeadRefinement.Enabled)
        {
            var stages = new List<string>();
            if (nelderMeadRefinement.MemeticInLoop)
                stages.Add($"memetic every {nelderMeadRefinement.EveryNGenerations} gen ({nelderMeadRefinement.MemeticMaxEvaluationsPerCall} evals/call)");
            if (nelderMeadRefinement.FinalPolish)
                stages.Add($"final polish ({nelderMeadRefinement.FinalPolishMaxEvaluations:N0} evals)");
            Console.WriteLine($"- Nelder–Mead refinement: {string.Join(" + ", stages)}");
        }
        else
        {
            Console.WriteLine("- Nelder–Mead refinement: off");
        }
        Console.WriteLine();
    }

    private static void WriteSummary()
    {
        Console.WriteLine(new string('=', 90));
        Console.WriteLine("GROUP OPTIMIZATION COMPLETED SUCCESSFULLY");
        Console.WriteLine(new string('=', 90));
        Console.WriteLine("Summary:");
        Console.WriteLine("  ✓ Simultaneous optimization of 3 composition groups");
        Console.WriteLine("  ✓ 32-parameter unified vector");
        Console.WriteLine("  ✓ 11 shared parameters optimized once");
        Console.WriteLine("  ✓ 7 specific parameters for each composition");
        Console.WriteLine("  ✓ Bas_2 group includes Bas_3 and Bas_4 (same specific parameters)");
        Console.WriteLine("  ✓ Burning rate plots rendered for each composition");
        Console.WriteLine("  ✓ Python visualization plots generated");
        Console.WriteLine("  ✓ PDF report generated with group parameters");
        Console.WriteLine(new string('=', 90) + "\n");
    }
}
