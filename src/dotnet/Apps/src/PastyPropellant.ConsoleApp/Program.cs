using System.Globalization;
using DotNetDifferentialEvolution.TerminationStrategies;
using ParametricCombustionModel.Core.Models;
using ParametricCombustionModel.Optimization.ConstraintPenaltyEvaluators;
using ParametricCombustionModel.Optimization.ConstraintPenaltyEvaluators.Interfaces;
using ParametricCombustionModel.Optimization.Results;
using ParametricCombustionModel.Optimization.Settings;
using ParametricCombustionModel.Optimization.Utils;
using ParametricCombustionModel.PlotRenderer.Models;
using ParametricCombustionModel.PlotRenderer.Renderers;
using ParametricCombustionModel.ReportMaking.Models;
using ParametricCombustionModel.ReportMaking.ReportMakers;
using ParametricCombustionModel.Telemetry;
using PastyPropellant.ConsoleApp;
using PastyPropellant.ConsoleApp.Helpers;
using PastyPropellant.ConsoleApp.Scenarios;
using PastyPropellant.ConsoleApp.Scenarios.Settings;
using PastyPropellant.Core.Models;
using PastyPropellant.Core.Models.Events.Logs;
using PastyPropellant.Core.Utils;
using PastyPropellant.Interop;
using PDFsharp.Api.Adapters;
using UnitsNet;

void GenerateGroupReport(
    GroupOptimizationResult groupOptimizationResult,
    string inputFileName,
    string cultureName,
    string reportSuffix,
    DifferentialEvolutionSettings? settings = null,
    PerformanceMeter? meter = null)
{
    // Set culture for localization
    var culture = new CultureInfo(cultureName);
    CultureInfo.DefaultThreadCurrentCulture = culture;
    CultureInfo.DefaultThreadCurrentUICulture = culture;
    
    // Generate report filename with culture suffix
    string pdfOutputFileName = $"propellants.group_optimization.report.{reportSuffix}.pdf";
    
    var pdfGeneratorAdapter = new PdfSharpAdapter(pdfOutputFileName);
    var reportContextDto = new GroupReportContextDto(
        groupOptimizationResult,
        inputFileName,
        settings,
        meter);
    
    var pdfReportMaker = new GroupPdfReportMaker(reportContextDto, pdfGeneratorAdapter);
    pdfReportMaker.MakeReport();
    
    Console.WriteLine($"Group report generated in {cultureName} culture: {pdfOutputFileName}");
}

double[] GetLowerBound()
{
    return [
        1.0,    // [0]  ADecompose                       kg/(m²·s)
        5e4,    // [1]  EDecompose                       J/mol
        1e5,    // [2]  AKineticFlameInterPocket         1/s
        5e4,    // [3]  EKineticFlameInterPocket         J/mol
        1e5,    // [4]  AKineticFlamePocketOutSkeleton   1/s
        5e4,    // [5]  EKineticFlamePocketOutSkeleton   J/mol
        1e5,    // [6]  AKineticFlamePocketSkeleton      1/s
        5e4,    // [7]  EKineticFlamePocketSkeleton      J/mol
        0.0,    // [8]  NuInterPocket
        0.0,    // [9]  NuPocketOutSkeleton
        0.0,    // [10] NuPocketSkeleton
        1e-10,  // [11] AMetalBurningConstant            m²/s
        1e-12,  // [12] BMetalBurningConstant            m³/s²
        -1e7,   // [13] DeltaH                           J/kg
        1e-3,   // [14] KDiffusionHeight
        0.0,    // [15] APowOrder                        (degenerate dimensional const = 1)
        0.0,    // [16] BPowOrder                        (degenerate dimensional const = 2)
        0.0     // [17] KCoefficientRadiationTemperature opened to [0,1] so DE can explore the radiative-temperature closure
    ];
}

double[] GetUpperBound()
{
    return [
        1e13,    // [0]  ADecompose  (user-chosen: jDE reaches the clean basin and avoids the degenerate corner; Bas_0 was pinned at the old 1e9 ceiling, so this can push lower)
        3e5,    // [1]  EDecompose
        1e13,   // [2]  AKineticFlameInterPocket
        2.5e5,  // [3]  EKineticFlameInterPocket
        1e13,   // [4]  AKineticFlamePocketOutSkeleton
        2.5e5,  // [5]  EKineticFlamePocketOutSkeleton
        1e13,   // [6]  AKineticFlamePocketSkeleton
        2.5e5,  // [7]  EKineticFlamePocketSkeleton
        2.5,    // [8]  NuInterPocket
        2.5,    // [9]  NuPocketOutSkeleton
        2.5,    // [10] NuPocketSkeleton
        1e-3,   // [11] AMetalBurningConstant
        1e-5,   // [12] BMetalBurningConstant
        1e7,    // [13] DeltaH
        1e1,    // [14] KDiffusionHeight
        3.0,    // [15] APowOrder
        3.0,    // [16] BPowOrder
        1.0     // [17] KCoefficientRadiationTemperature (was [0,0], now [0,1])
    ];
}

// ========== GROUP OPTIMIZATION BOUNDS (32-ELEMENT VECTOR) ==========
// Structure: [0-10] shared, [11-17] Bas_2, [18-24] Bas_1, [25-31] Bas_0
double[] GetGroupLowerBound()
{
    var originalLower = GetLowerBound();
    var groupBounds = new double[32];
    
    // Map shared parameters [0-10] from original indices [2, 3, 4, 5, 8, 9, 13, 14, 15, 16, 17]
    int[] commonIndices = [2, 3, 4, 5, 8, 9, 13, 14, 15, 16, 17];
    for (int i = 0; i < commonIndices.Length; i++)
        groupBounds[i] = originalLower[commonIndices[i]];
    
    // Map specific parameters for each composition [11-31] from original indices [0, 1, 6, 7, 10, 11, 12]
    int[] specificIndices = [0, 1, 6, 7, 10, 11, 12];
    for (int composition = 0; composition < 3; composition++)
    {
        int blockStart = 11 + composition * 7;
        for (int i = 0; i < specificIndices.Length; i++)
            groupBounds[blockStart + i] = originalLower[specificIndices[i]];
    }
    
    return groupBounds;
}

double[] GetGroupUpperBound()
{
    var originalUpper = GetUpperBound();
    var groupBounds = new double[32];
    
    // Map shared parameters [0-10] from original indices [2, 3, 4, 5, 8, 9, 13, 14, 15, 16, 17]
    int[] commonIndices = [2, 3, 4, 5, 8, 9, 13, 14, 15, 16, 17];
    for (int i = 0; i < commonIndices.Length; i++)
        groupBounds[i] = originalUpper[commonIndices[i]];
    
    // Map specific parameters for each composition [11-31] from original indices [0, 1, 6, 7, 10, 11, 12]
    int[] specificIndices = [0, 1, 6, 7, 10, 11, 12];
    for (int composition = 0; composition < 3; composition++)
    {
        int blockStart = 11 + composition * 7;
        for (int i = 0; i < specificIndices.Length; i++)
            groupBounds[blockStart + i] = originalUpper[specificIndices[i]];
    }
    
    return groupBounds;
}

// Shared physical-constraint penalty evaluators, single-sourced so the optimization run and the
// forward-eval path report identical penalties for the same point.
IPenaltyEvaluator[] BuildGroupPenaltyEvaluators()
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

async Task<(OperationResult<GroupOptimizationResult>? result, PerformanceMeter meter, DifferentialEvolutionSettings? deSettings)> RunGroupOptimizationAsync(string inputFileName)
{
    var meter = new PerformanceMeter();

    var penaltyEvaluators = BuildGroupPenaltyEvaluators();

    var groupLowerBound = GetGroupLowerBound();
    var groupUpperBound = GetGroupUpperBound();
    var dimensions = groupLowerBound.Length; // 32

    // jDE (self-adaptive rand/1) is the winning strategy on this branch: its exploration reaches the
    // clean basin (obj 0.157 / penalty 0), whereas current-to-pbest variants either trap in a penalized
    // degenerate corner (SHADE/L-SHADE → 0.92) or converge prematurely to a worse optimum (JADE → 0.42).
    var strategy = DifferentialEvolutionStrategy.Jde;
    var maxAvailableProcessors = Math.Max(1, Environment.ProcessorCount - 1);
    var safetyTimeout = new TimeoutTerminationStrategy(TimeSpan.FromHours(9));

    int populationSize;
    int processorsCount;
    OrTerminationStrategy terminationStrategy;
    long? maxEvaluationNumber = null;
    // L-SHADE control parameters (left null for the fixed-population variants, which ignore them).
    double? pBestRate = null;
    double? archiveSizeRate = null;
    int? memorySize = null;

    if (strategy == DifferentialEvolutionStrategy.LShade)
    {
        // L-SHADE: a larger initial population (18*D) that shrinks linearly to ~4 across a fixed
        // evaluation budget; the budget both terminates the run and defines the reduction schedule.
        // Size it to what is actually achievable so the 576→4 schedule completes — a huge budget just
        // back-loads convergence (the 9 h timeout would fire long before LPSR reaches the floor).
        // NOTE: the memetic Nelder–Mead evaluations count toward this same budget.
        populationSize = dimensions * 18; // 32 * 18 = 576
        processorsCount = maxAvailableProcessors; // population shrinks, so the divisibility heuristic does not apply
        maxEvaluationNumber = 5_000_000;
        // Canonical L-SHADE control parameters (Tanabe & Fukunaga 2014): p-best 0.11, archive rate 2.6, memory 6.
        pBestRate = 0.11;
        archiveSizeRate = 2.6;
        memorySize = 6;
        terminationStrategy = new OrTerminationStrategy(
            new LimitEvaluationNumberTerminationStrategy(maxEvaluationNumber.Value),
            safetyTimeout);
    }
    else
    {
        // Fixed-population variants (Classic / jDE / JADE / SHADE): keep the stagnation+timeout
        // run control and pick a worker count that divides the population evenly for balanced load.
        populationSize = dimensions * 12; // 32 * 12 = 384 (jDE production population — reaches obj 0.157 / penalty 0)
        processorsCount = maxAvailableProcessors;
        for (; processorsCount >= 14; processorsCount--)
            if (populationSize % processorsCount == 0)
                break;
        terminationStrategy = new OrTerminationStrategy(
            new CustomStagnationStreakTerminationStrategy(
                maxStagnationStreak: 5_000,
                relativeStagnationThreshold: 1e-6),
            safetyTimeout);
    }

    // Nelder–Mead refinement layered on top of the DE search (DotNetNelderMead.DifferentialEvolution):
    // an in-loop memetic refiner every N generations plus a final sequential polish of the converged best.
    // Surfaced here like `strategy`; set Enabled = false to fall back to plain DE.
    var nelderMeadRefinement = new NelderMeadRefinementSettings
    {
        Enabled = true, // final-polish-only (memetic stays off); guarded, so it can only improve the jDE best
        // Memetic in-loop NM is OFF. Tested with jDE (2026-06-04): it does NOT collapse — jDE stays in the
        // clean penalty-0 basin, so the memetic polish reached 0.15742 / penalty 0, basically the same point
        // as final-polish-only (0.15721) but a hair worse and ~138k generations earlier (it trims diversity
        // and accelerates stagnation for no gain). The earlier SHADE/L-SHADE memetic collapse (0.921099 /
        // penalty 0.5) was current-to-pbest already sitting in the penalized corner, not an intrinsic flaw.
        MemeticInLoop = false,
        EveryNGenerations = 50,
        MemeticMaxEvaluationsPerCall = 200,
        // Final polish runs once after L-SHADE converges (outside the budget) — give it room.
        FinalPolish = true,
        FinalPolishMaxEvaluations = 100_000,
        AdaptiveCoefficients = true,
        DomainTolerance = 1e-8,
        FunctionTolerance = 1e-8,
        Restarts = 2,
    };

    var settingsBuilder = DifferentialEvolutionScenarioSettings
                    .CreateBuilder()
                    .WithMeter(meter)
                    .WithPropellantsFromFile(inputFileName)
                    .WithPopulationSize(populationSize)
                    .WithLowerBound(groupLowerBound)
                    .WithUpperBound(groupUpperBound)
                    .WithStrategy(strategy)
                    // F/CR below are only consumed by the Classic and jDE strategies; the adaptive variants self-tune them.
                    .WithMutationForce(0.5)
                    .WithCrossoverProbability(0.9)
                    .WithTerminationStrategy(terminationStrategy)
                    .AddPenaltyEvaluators(penaltyEvaluators)
                    .WithProcessorsCount(processorsCount)
                    .WithNelderMeadRefinement(nelderMeadRefinement);

    if (maxEvaluationNumber.HasValue)
        settingsBuilder = settingsBuilder.WithMaxEvaluationNumber(maxEvaluationNumber.Value);

    if (pBestRate.HasValue && archiveSizeRate.HasValue && memorySize.HasValue)
        settingsBuilder = settingsBuilder.WithShadeParameters(pBestRate.Value, archiveSizeRate.Value, memorySize.Value);

    var settings = settingsBuilder.Build();

    var scenario = new GroupDifferentialEvolutionScenario(settings);

    Console.WriteLine("Starting group optimization (Bas_0, Bas_1, Bas_2+Bas_3+Bas_4):");
    Console.WriteLine($"- Input file: {inputFileName}");
    Console.WriteLine($"- Algorithm: {settings.DifferentialEvolutionSettings.Strategy}");
    Console.WriteLine($"- Population size: {populationSize}");
    if (maxEvaluationNumber.HasValue)
        Console.WriteLine($"- Evaluation budget: {maxEvaluationNumber.Value:N0}");
    if (pBestRate.HasValue && archiveSizeRate.HasValue && memorySize.HasValue)
        Console.WriteLine($"- L-SHADE control: p-best={pBestRate.Value}, archiveRate={archiveSizeRate.Value}, memory={memorySize.Value}");
    Console.WriteLine($"- Parameter vector size: 32 (11 shared + 7×3 specific)");
    Console.WriteLine($"- Processors: {processorsCount}");
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

    OperationResult<GroupOptimizationResult> operationResult;

    using (meter.GetTotalExecutionTimeMeasurer().StartFrame())
    {
        operationResult = await scenario.RunAsync();
    }

    return (operationResult, meter, settings.DifferentialEvolutionSettings);
}

// Reads a list of doubles (invariant culture) from a file: whitespace/comma/semicolon separated,
// with optional '#' line comments so the vector file can be self-documenting.
double[] ReadVectorFile(string path)
{
    if (!File.Exists(path))
        throw new FileNotFoundException($"Vector file not found: {path}");

    var values = new List<double>();
    string? declaredChecksum = null;
    int? declaredCount = null;

    foreach (var rawLine in File.ReadLines(path))
    {
        var commentStart = rawLine.IndexOf('#');
        if (commentStart >= 0)
        {
            // Self-describing guards emitted by the report / WriteVectorFile: "# count = N", "# checksum = <hex>".
            var directive = rawLine[(commentStart + 1)..].Trim();
            var eq = directive.IndexOf('=');
            if (eq > 0)
            {
                var key = directive[..eq].Trim();
                var val = directive[(eq + 1)..].Trim();
                if (key.Equals("checksum", StringComparison.OrdinalIgnoreCase))
                    declaredChecksum = val;
                else if (key.Equals("count", StringComparison.OrdinalIgnoreCase)
                         && int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
                    declaredCount = n;
            }
        }

        var line = commentStart >= 0 ? rawLine[..commentStart] : rawLine;

        foreach (var token in line.Split([' ', '\t', ',', ';'], StringSplitOptions.RemoveEmptyEntries))
            values.Add(double.Parse(token, CultureInfo.InvariantCulture));
    }

    var genes = values.ToArray();

    if (declaredCount is { } expectedCount && expectedCount != genes.Length)
        throw new InvalidDataException(
            $"Vector file '{path}' declares # count = {expectedCount} but contains {genes.Length} values (copy/paste truncated?).");

    if (declaredChecksum is { } expectedChecksum)
    {
        var actualChecksum = VectorFormat.Checksum(genes);
        if (!string.Equals(actualChecksum, expectedChecksum, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException(
                $"Vector file '{path}' checksum mismatch (declared {expectedChecksum}, computed {actualChecksum}) — a value was likely mangled in transit.");
    }

    return genes;
}

// Persists a vector at round-trip ("R") precision, one value per line, so an optimization run can
// be replayed exactly via --forward-eval (a PDF report is too lossy to recover the vector from).
void WriteVectorFile(string path, double[] genes)
{
    // Self-documenting guards (ignored by ReadVectorFile's parser, verified by it) so a truncated or
    // mangled copy is caught on read; gene lines use the shared VectorFormat so they match the PDF block.
    var lines = new List<string>
    {
        $"# count = {genes.Length}",
        $"# checksum = {VectorFormat.Checksum(genes)}"
    };
    lines.AddRange(VectorFormat.FormatLines(genes));
    File.WriteAllLines(path, lines);
}

// Forward-eval: solve every (fuel, pressure) context for a SINGLE supplied group vector — no DE
// search — and emit the same report/plots as an optimization run. Seconds instead of ~4 hours.
async Task RunForwardEvalAsync(string inputFileName, string vectorFilePath)
{
    Console.WriteLine(new string('=', 90));
    Console.WriteLine("FORWARD EVALUATION (single parameter vector — no optimization)");
    Console.WriteLine(new string('=', 90) + "\n");

    var genes = ReadVectorFile(vectorFilePath);
    var groupLowerBound = GetGroupLowerBound();
    if (genes.Length != groupLowerBound.Length)
        throw new ArgumentException(
            $"Vector file '{vectorFilePath}' has {genes.Length} values; expected {groupLowerBound.Length} (group vector).");

    var meter = new PerformanceMeter();
    var settings = DifferentialEvolutionScenarioSettings.CreateBuilder()
        .WithMeter(meter)
        .WithPropellantsFromFile(inputFileName)
        .WithPopulationSize(groupLowerBound.Length) // unused (no DE run), but the builder requires a positive value
        .WithLowerBound(groupLowerBound)
        .WithUpperBound(GetGroupUpperBound())
        .WithStrategy(DifferentialEvolutionStrategy.Jde)
        .WithMutationForce(0.5)
        .WithCrossoverProbability(0.9)
        .WithTerminationStrategy(new TimeoutTerminationStrategy(TimeSpan.FromMinutes(1)))
        .AddPenaltyEvaluators(BuildGroupPenaltyEvaluators())
        .WithProcessorsCount(1)
        .Build();

    var scenario = new GroupDifferentialEvolutionScenario(settings);

    Console.WriteLine($"- Input file: {inputFileName}");
    Console.WriteLine($"- Vector file: {vectorFilePath} ({genes.Length} parameters)\n");

    GroupOptimizationResult result;
    using (meter.GetTotalExecutionTimeMeasurer().StartFrame())
        result = scenario.EvaluateVector(genes);

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
    var plotSettings = new PlotSettings
    {
        Title = "Burning Rates - Forward Evaluation (All Propellants)",
        TitleFontSize = 22,
        XAxisMinimum = 0.9,
        XAxisMaximum = 6.6,
        XAxisTitle = "Pressure, MPa",
        YAxisTitle = "mm/s",
        Width = 800,
        Height = 800,
        Dpi = 96
    };
    new GroupBurningRatePlotRenderer().Render(result, plotSettings);
    new GroupBurnRateLogLogPlotRenderer().Render(result, new PlotSettings
    {
        Title = "Burning Rates (log-log) - Forward Evaluation (All Propellants)",
        TitleFontSize = 22,
        XAxisMinimum = 0.9,
        XAxisMaximum = 6.6,
        XAxisTitle = "Pressure, MPa (log)",
        YAxisTitle = "mm/s (log)",
        Width = 800,
        Height = 800,
        Dpi = 96
    });

    Console.WriteLine("Rendering Python plots...");
    var propellantPlotsRenderingHelper = new PropellantPlotsRenderingHelper(
        PythonRuntime.ScriptPath("PropellantsPlotRendering/src/main.py"));
    await propellantPlotsRenderingHelper.RenderPlotsAsync(inputFileName);

    Console.WriteLine("Rendering skeleton-layer plots...");
    var skeletonPlotsResult = await SkeletonLayerPlotsHelper.RenderPlotsAsync(
        result.ToOptimizationResult(),
        PythonRuntime.ScriptPath("PropellantsPlotRendering/src/skeleton_layer_plots.py"));
    if (!skeletonPlotsResult.IsSuccess)
        Console.WriteLine($"⚠ Skeleton-layer plots failed: {skeletonPlotsResult.Exception?.Message}");

    Console.WriteLine("Generating PDF report...");
    GenerateGroupReport(result, inputFileName, "en-US", "en",
        settings.DifferentialEvolutionSettings, meter);

    Console.WriteLine("\n✓ Forward-evaluation report generated\n");
}

EventBus<InfoLogEvent>.Subscribe(logEvent => {
    Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}] ({logEvent.Sender ?? "none"}) | {logEvent.Message}");
});

try
{
    // Forward-eval mode: `--forward-eval <vector-file>` solves and reports a single supplied
    // group vector without running the optimizer (point evaluation; ~seconds).
    var forwardEvalIndex = Array.IndexOf(args, "--forward-eval");
    if (forwardEvalIndex >= 0)
    {
        if (forwardEvalIndex + 1 >= args.Length)
            throw new ArgumentException(
                "--forward-eval requires a path to a vector file, e.g. --forward-eval best_vector.txt");

        await RunForwardEvalAsync("propellants.01234.json", args[forwardEvalIndex + 1]);
        return;
    }

    Console.WriteLine(new string('=', 90));
    Console.WriteLine("GROUP OPTIMIZATION: BAS_0, BAS_1, BAS_2+BAS_3+BAS_4 (SIMULTANEOUS)");
    Console.WriteLine(new string('=', 90) + "\n");

    var (operationResult, meter, deSettings) = await RunGroupOptimizationAsync("propellants.01234.json");

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
    WriteVectorFile("best_vector.txt", groupResult.BestParams);
    Console.WriteLine("\n✓ Best vector saved to best_vector.txt (replay: --forward-eval best_vector.txt)\n");

    // Render burning rate plots for each group
    Console.WriteLine("\nRendering burning rate plots...");
    var plotSettings = new PlotSettings
    {
        Title = "Burning Rates - Group Optimization (All Propellants)",
        TitleFontSize = 22,
        XAxisMinimum = 0.9,
        XAxisMaximum = 6.6,
        XAxisTitle = "Pressure, MPa",
        YAxisTitle = "mm/s",
        Width = 800,
        Height = 800,
        Dpi = 96
    };

    var groupPlotRenderer = new GroupBurningRatePlotRenderer();
    groupPlotRenderer.Render(groupResult, plotSettings);
    Console.WriteLine("✓ Group burning rate plot rendered\n");

    new GroupBurnRateLogLogPlotRenderer().Render(groupResult, new PlotSettings
    {
        Title = "Burning Rates (log-log) - Group Optimization (All Propellants)",
        TitleFontSize = 22,
        XAxisMinimum = 0.9,
        XAxisMaximum = 6.6,
        XAxisTitle = "Pressure, MPa (log)",
        YAxisTitle = "mm/s (log)",
        Width = 800,
        Height = 800,
        Dpi = 96
    });
    Console.WriteLine("✓ Group burning rate log-log plot rendered\n");

    // Render Python plots
    Console.WriteLine("Rendering Python plots...");
    var propellantPlotsRenderingHelper = new PropellantPlotsRenderingHelper(
        PythonRuntime.ScriptPath("PropellantsPlotRendering/src/main.py"));
    await propellantPlotsRenderingHelper.RenderPlotsAsync("propellants.01234.json");
    Console.WriteLine("✓ Python plots rendered\n");

    // Render skeleton-layer plots from the fitted solver outputs (JSON sidecar)
    Console.WriteLine("Rendering skeleton-layer plots...");
    var skeletonPlotsResult = await SkeletonLayerPlotsHelper.RenderPlotsAsync(
        groupResult!.ToOptimizationResult(),
        PythonRuntime.ScriptPath("PropellantsPlotRendering/src/skeleton_layer_plots.py"));
    if (skeletonPlotsResult.IsSuccess)
        Console.WriteLine("✓ Skeleton-layer plots rendered\n");
    else
        Console.WriteLine($"⚠ Skeleton-layer plots failed: {skeletonPlotsResult.Exception?.Message}\n");

    // Generate PDF report
    Console.WriteLine("Generating PDF report...");
    GenerateGroupReport(groupResult!, "propellants.01234.json", "en-US", "en", deSettings, meter);
    Console.WriteLine("✓ PDF report generated\n");

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
catch (Exception ex)
{
    Console.WriteLine($"\n❌ Error: {ex}");
}
