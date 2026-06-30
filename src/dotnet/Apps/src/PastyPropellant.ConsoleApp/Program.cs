using System.Globalization;
using DotNetDifferentialEvolution.TerminationStrategies;
using ParametricCombustionModel.Core.Models;
using ParametricCombustionModel.Optimization.ConstraintPenaltyEvaluators;
using ParametricCombustionModel.Optimization.ConstraintPenaltyEvaluators.Interfaces;
using ParametricCombustionModel.Optimization.Results;
using ParametricCombustionModel.Optimization.Settings;
using ParametricCombustionModel.PlotRenderer.Models;
using ParametricCombustionModel.PlotRenderer.Renderers;
using ParametricCombustionModel.ReportMaking.Models;
using ParametricCombustionModel.ReportMaking.ReportMakers;
using ParametricCombustionModel.Telemetry;
using PastyPropellant.ConsoleApp;
using PastyPropellant.ConsoleApp.Helpers;
using PastyPropellant.ConsoleApp.Scenarios;
using PastyPropellant.ConsoleApp.Scenarios.Settings;
using PastyPropellant.Core.Models.Events.Logs;
using PastyPropellant.Core.Utils;
using PDFsharp.Api.Adapters;
using UnitsNet;

void GenerateGroupReport(
    GroupOptimizationResult groupOptimizationResult,
    string inputFileName,
    string cultureName,
    string reportSuffix,
    System.Collections.ObjectModel.ReadOnlyCollection<Propellant> propellants,
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
        propellants,
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
        0.0,    // [17] KCoefficientRadiationTemperature opened to [0,1] so DE can explore the radiative-temperature closure
        0.0,    // [18] KDiffusionPressureFactor (C_rxn): BDP/Lengellé diffusion-standoff pressure factor; 0 ⇒ original laminar model
        0.0,    // [19] KDiffusionSizeExponent (m): petite-ensemble standoff size-exponent (d/d_ref)^m; 0 ⇒ size-independent
        0.0,    // [20] KBimodalPackingFactor (K_pack): bimodal heat-feedback enhancement 1+K·f_c(1−f_c); 0 ⇒ no enhancement
        2.0,    // [21] KDiffusionPressureExponent (n_p): standoff pressure-exponent (p_ref/p)^n_p; 2 ⇒ original fixed quadratic law
        0.0,    // [22] KCondensedReactionFactor (K_wsb): WSB condensed-phase reaction completeness θ=Da/(1+Da), Da=K_wsb(p/p_ref)²; 0 ⇒ original surface energy balance
        0.0,    // [23] KApPremixedFactor (K_AP): AP self-deflagration premixed-flame conductance q_AP=K·(1−f_c)·(T_AP−T_s)·max(0,(p/p_dl)^n_AP−1) [W·m⁻²·K⁻¹]; 0 ⇒ Step-6 model
        0.77,   // [24] KApPressureExponent (n_AP): AP monopropellant pressure exponent; 0.77 ⇒ Guirao–Williams floor
        0.0     // [25] KBimodalPackingPressureExponent (a_pack): bimodal-packing pressure-decay (p_ref/p)^a_pack; 0 ⇒ pressure-independent K_pack = Step-8 model (floor, cannot regress)
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
        1.0,    // [17] KCoefficientRadiationTemperature (was [0,0], now [0,1])
        50.0,   // [18] KDiffusionPressureFactor (C_rxn): diffusion-standoff pressure-factor coeff, range [0,50] (BDP/Lengellé)
        2.0,    // [19] KDiffusionSizeExponent (m): petite-ensemble standoff size-exponent, range [0,2] (BDP/Lengellé)
        10.0,   // [20] KBimodalPackingFactor (K_pack): bimodal heat-feedback enhancement coeff, range [0,10] (Miller 1982/Kubota)
        4.0,    // [21] KDiffusionPressureExponent (n_p): standoff pressure-exponent, range [2,4] (Lengellé–Duterque–Trubert 2000/BDP 1970)
        5.0,    // [22] KCondensedReactionFactor (K_wsb): WSB condensed-phase reaction coeff, range [0,5] (Ward–Son–Brewster 1998/Zenin 1995)
        5000.0, // [23] KApPremixedFactor (K_AP): AP premixed-flame conductance [W·m⁻²·K⁻¹], range [0,5000] (Boggs 1970/Guirao–Williams 1971/Price 1984)
        2.5,    // [24] KApPressureExponent (n_AP): AP monopropellant burn-rate pressure exponent, range [0.77,2.5] (Boggs 1970/Price 1984 — AP slope is regime-dependent)
        2.0     // [25] KBimodalPackingPressureExponent (a_pack): bimodal-packing pressure-decay exponent, range [0,2] (LEF importance ~ diffusion-standoff p⁻¹–p⁻²; BDP 1970 / AP plateau lit.)
    ];
}

// ========== GROUP OPTIMIZATION BOUNDS (40-ELEMENT VECTOR) ==========
// Structure: [0-10] shared, [11-17] Bas_2, [18-24] Bas_1, [25-31] Bas_0,
//            [32] C_rxn, [33] m, [34] K_pack, [35] n_p, [36] K_wsb, [37] K_AP, [38] n_AP, [39] a_pack (appended-tail shared)
double[] GetGroupLowerBound()
{
    var originalLower = GetLowerBound();
    var groupBounds = new double[40];
    
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

    // Shared diffusion-flame pressure-factor coefficient (18-vector index 18) appended at group index 32
    groupBounds[32] = originalLower[18];

    // Shared diffusion-flame standoff size-exponent (19-vector index 19) appended at group index 33
    groupBounds[33] = originalLower[19];

    // Shared bimodal-packing heat-feedback enhancement (20-vector index 20) appended at group index 34
    groupBounds[34] = originalLower[20];

    // Shared diffusion-flame standoff pressure-exponent (21-vector index 21) appended at group index 35
    groupBounds[35] = originalLower[21];

    // Shared WSB condensed-phase reaction coefficient (22-vector index 22) appended at group index 36
    groupBounds[36] = originalLower[22];

    // Shared AP monopropellant premixed-flame conductance (23-vector index 23) appended at group index 37
    groupBounds[37] = originalLower[23];

    // Shared AP self-deflagration pressure exponent (24-vector index 24) appended at group index 38
    groupBounds[38] = originalLower[24];

    // Shared bimodal-packing pressure-decay exponent a_pack (25-vector index 25) appended at group index 39
    groupBounds[39] = originalLower[25];

    return groupBounds;
}

double[] GetGroupUpperBound()
{
    var originalUpper = GetUpperBound();
    var groupBounds = new double[40];
    
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

    // Shared diffusion-flame pressure-factor coefficient (18-vector index 18) appended at group index 32
    groupBounds[32] = originalUpper[18];

    // Shared diffusion-flame standoff size-exponent (19-vector index 19) appended at group index 33
    groupBounds[33] = originalUpper[19];

    // Shared bimodal-packing heat-feedback enhancement (20-vector index 20) appended at group index 34
    groupBounds[34] = originalUpper[20];

    // Shared diffusion-flame standoff pressure-exponent (21-vector index 21) appended at group index 35
    groupBounds[35] = originalUpper[21];

    // Shared WSB condensed-phase reaction coefficient (22-vector index 22) appended at group index 36
    groupBounds[36] = originalUpper[22];

    // Shared AP monopropellant premixed-flame conductance (23-vector index 23) appended at group index 37
    groupBounds[37] = originalUpper[23];

    // Shared AP self-deflagration pressure exponent (24-vector index 24) appended at group index 38
    groupBounds[38] = originalUpper[24];

    // Shared bimodal-packing pressure-decay exponent a_pack (25-vector index 25) appended at group index 39
    groupBounds[39] = originalUpper[25];

    return groupBounds;
}

async Task<(OperationResult<GroupOptimizationResult>? result, PerformanceMeter meter, DifferentialEvolutionSettings? deSettings, System.Collections.ObjectModel.ReadOnlyCollection<Propellant> propellants)> RunGroupOptimizationAsync(string inputFileName)
{
    const double penaltyRate = 0.01;
    const double heatFluxRatioThreshold = 1000.0;
    const double poreDiameterThreshold = 3.0;
    const double largeOxidizerParticleSizeThreshold = 1.0;

    var meter = new PerformanceMeter();

    var maxInterPocketKineticFlameHeatFlux = HeatFlux.FromWattsPerSquareMeter(1e9);
    var maxSkeletonKineticFlameHeatFlux = HeatFlux.FromWattsPerSquareMeter(1e8);
    var maxOutSkeletonKineticFlameHeatFlux = HeatFlux.FromWattsPerSquareMeter(1e8);

    IPenaltyEvaluator[] penaltyEvaluators = [
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

    var groupLowerBound = GetGroupLowerBound();
    var groupUpperBound = GetGroupUpperBound();
    var dimensions = groupLowerBound.Length; // 40 (11 shared + 7×3 specific + 8 appended-tail shared)

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
        populationSize = dimensions * 18; // 40 * 18 = 720
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
        populationSize = dimensions * 12; // 40 * 12 = 480 (jDE production population)
        processorsCount = maxAvailableProcessors;
        for (; processorsCount >= 14; processorsCount--)
            if (populationSize % processorsCount == 0)
                break;
        terminationStrategy = new OrTerminationStrategy(
            new CustomStagnationStreakTerminationStrategy(
                maxStagnationStreak: 100_000,
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
    Console.WriteLine($"- Parameter vector size: {dimensions} (11 shared + 7×3 specific + 8 appended-tail shared)");
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

    return (operationResult, meter, settings.DifferentialEvolutionSettings, settings.Propellants);
}

EventBus<InfoLogEvent>.Subscribe(logEvent => {
    Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}] ({logEvent.Sender ?? "none"}) | {logEvent.Message}");
});

try
{
    Console.WriteLine(new string('=', 90));
    Console.WriteLine("GROUP OPTIMIZATION: BAS_0, BAS_1, BAS_2+BAS_3+BAS_4 (SIMULTANEOUS)");
    Console.WriteLine(new string('=', 90) + "\n");

    var (operationResult, meter, deSettings, propellants) = await RunGroupOptimizationAsync("propellants.01234.json");

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
    var compositionNames = new[] { "Bas_2+Bas_3+Bas_4", "Bas_1", "Bas_0" };
    Console.WriteLine("Individual Results:");
    for (int i = 0; i < 3; i++)
    {
        Console.WriteLine($"  {compositionNames[i]}:");
        Console.WriteLine($"    - Fitness: {groupResult.IndividualFitnesses[i]:E4}");
        Console.WriteLine($"    - Penalty: {groupResult.IndividualPenalties[i]:E4}");
    }

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

    // Render Python plots
    Console.WriteLine("Rendering Python plots...");
    var propellantPlotsRenderingHelper = new PropellantPlotsRenderingHelper(
        "../../../../../src/python/PropellantsPlotRendering/src/main.py");
    await propellantPlotsRenderingHelper.RenderPlotsAsync("propellants.01234.json");
    Console.WriteLine("✓ Python plots rendered\n");

    // Generate PDF report
    Console.WriteLine("Generating PDF report...");
    GenerateGroupReport(groupResult!, "propellants.01234.json", "en-US", "en", propellants, deSettings, meter);
    Console.WriteLine("✓ PDF report generated\n");

    Console.WriteLine(new string('=', 90));
    Console.WriteLine("GROUP OPTIMIZATION COMPLETED SUCCESSFULLY");
    Console.WriteLine(new string('=', 90));
    Console.WriteLine("Summary:");
    Console.WriteLine("  ✓ Simultaneous optimization of 3 composition groups");
    Console.WriteLine("  ✓ 40-parameter unified vector");
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
