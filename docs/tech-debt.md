# Technical Debt Register — PastyPropellant

**Scope:** whole repository (.NET + Python + scripts). **Date:** 2026-07-12.
**Branch audited:** `experiment/surface-900-metal-1300` (working tree, includes the uncommitted two-scenario `Program.cs` edits — some line numbers there will shift once that work lands).
**Method:** four read-only audit passes over disjoint areas (core optimization/computation · reporting/plotting/telemetry · host app/infra/build · Python & scripts) plus a repo-hygiene meta-scan. No code was changed.

**Priority weighting** (per request): **Architecture/coupling** and **Dead/duplicated code** are listed first and weighted highest; correctness, tests, build and docs follow. Two genuine *shipping bugs* surfaced during the sweep and are called out up top despite being outside the top-weighted categories.

### How to read this
- **Severity** — High / Med / Low (impact × blast radius).
- **Effort** — S (< 1 h) · M (hours) · L (day+).
- IDs are stable so they can be referenced in commits/PRs.
- A few hygiene items need a `git` check to confirm whether a file is *tracked* vs merely present on disk (the audit was run without git); those are marked **[verify tracked]**.

---

## Resolution log

Items closed on `dev`, newest last. Each was independently re-verified against the code before the fix landed — where the audit turned out to be wrong, that is recorded too.

| ID | Commit | Note |
|----|--------|------|
| [BUG-1](#bug-1) | `9062392` | Confirmed exactly as described. Pressure tables now emit 17 rows, not 16. |
| [BUG-2](#bug-2) | `9ca6a18` | Confirmed: zero subscribers. stderr is now buffered (50 lines / 8192 chars, tail kept) into the failure exception; the event is still published. |
| [BUILD-2](#build-2), BUILD-4, [DEAD-7](#dead-7) | `2454091` | **Audit was wrong on the key point:** all six files were *tracked*, so ignoring alone would have been inert — git never ignores a tracked file. Files untracked and left on disk. `.env` held only a placeholder, never a live key. |
| [DOC-1](#doc-1), [DOC-2](#doc-2) | `91593f7` | Every fictional element confirmed absent. `data/optimization_tickets.json` does not merely go unused — **it does not exist**. |
| [COR-1](#cor-1) | `aa49396` | Confirmed, and worse than described: **three** concurrent publishers, not one. The cited `Parallel.ForEach` is the least dangerous — it holds a local lock. Fixed by copy-on-write. |
| [COR-5](#cor-5) | `e13be7b` | Confirmed. Optional token + timeout, defaults preserve wait-forever, all call sites unchanged. |
| [DEAD-3](#dead-3) | `f85dbcd` | All three renderers confirmed unreferenced repo-wide. |
| [DEAD-5](#dead-5) | `2cd2456` | Confirmed *except* the Python line — see NEW-2 below. |
| [DEAD-6](#dead-6) | `de45bd8` | Partially closed: `GroupReportContextDto.Propellants` is a required ctor parameter supplied by `Program.cs`, so removing it is an API change — deferred to the [ARCH-8](#arch-8) pass. |

## 🔴 New findings (not in the original audit)

<a id="new-1"></a>**NEW-1 · High · —** — **Live Telegram bot token committed since `Initial commit`.**
`data/telegram_settings.json` is tracked and its `token` field matches the Telegram bot-token shape exactly (`\d{8,12}:[A-Za-z0-9_-]{35}`), alongside a numeric `chat_id`. It has been in git since `5802f0d`, i.e. in every clone and every history snapshot, on a repository that is public. [BUILD-2](#build-2) pointed at `.env` as *the* secrets hazard and aimed at the wrong file — `.env` holds an inert placeholder. → **Revoke and reissue the bot token via BotFather.** History rewriting is pointless while the token is live. Note nothing in `src/dotnet` actually reads this file (see [DOC-1](#doc-1)), so untracking it costs nothing functionally.

<a id="new-2"></a>**NEW-2 · — · —** — **[DEAD-5](#dead-5) was wrong about the Python line.**
`PorosityCalculation/src/calculators.py:8` is not dead code: `# ALUMINUM_TEMPERATURE_FACTOR = 0.6 # Unused: Previously considered 40% density reduction at 2300 K`. The trailing prose records a *rejected physical assumption* and names the exact metal temperature under active investigation elsewhere in the project. That is modelling provenance. Kept deliberately; do not re-flag.

<a id="new-3"></a>**NEW-3 · Med · S** — Reports read `BurnRate` with no convergence guard.
Every report (`PressureTablesReport`, `BurnRateErrorReport`, `BurnRateVieilleFitReport`, `ProblemContextReport`, `SkeletonLayerPlotsHelper`) reads `MixedCombustionParams.BurnRate` **unconditionally** — none checks `BurnRateIsFound`. A non-converged context reaching a report prints a stale or sentinel burn rate silently. Distinct from [COR-2](#cor-2).

<a id="new-4"></a>**NEW-4 · Low · S** — `rightValue` is a write-once dead local in all four bisection loops.
Computed for the initial bracket sign-check, then never read again — the loops test only `leftValue`. The algorithm is correct; the variable is vestigial. Surfaced while removing the commented-out `// rightValue = middleValue;` mirrors in `2cd2456`.

<a id="new-5"></a>**NEW-5 · Med · —** — `checker.py` structurally cannot catch solver parity defects.
It pairs files by name (`(.*)(ByDoubles|ByUnits)(.*)\.cs$`), but the solvers carry **both** overloads inside a single file, so every solver is invisible to it. [COR-2](#cor-2) is exactly the class of defect it was built to find, and it cannot see it. Do not treat a clean `checker.py` run as parity evidence for the solvers.

<a id="new-6"></a>**NEW-6 · Low · S** — Comment/resource mismatch in `PressureTablesReport`.
The comment at `PressureTablesReport.cs:146` reads "Add pocket kinetic flame heat flux" while the block below it uses the `SkeletonKineticFlameHeatFlux` resources. Cosmetic, but it is what made [BUG-1](#bug-1) hard to spot by eye.

<a id="new-7"></a>**NEW-7 · Med · —** — The parity test harness is in the dead tree.
`MixedPropellantSolverTester.cs` already asserts `unitsParams.BurnRateIsFound == doublesParams.BurnRateIsFound` — exactly the [COR-2](#cor-2) invariant — but it lives in `tests/ParametricCombustionModel/…`, which is not in the solution and does not compile ([DEAD-1](#dead-1)). It has never run since the restructure. Its `ComparePocketCombustionParams` is additionally defined but never called. Any test written to cover a solver fix must go to a **live** project, which for `Computation` does not yet exist ([TEST-1](#test-1)).

---

## ⭐ Most consequential (act on these first)

| ID | What | Sev |
|----|------|-----|
| [DOC-1](#doc-1) | `CLAUDE.md`'s architecture is **fictional** — the worker/named-pipe/`Initializer`/`OptimizationController` model it documents does not exist in the code. Misdirects every future contributor (and this assistant). | High |
| [DEAD-1](#dead-1) | The **entire root `tests/` tree, the nested `tests/ParametricCombustionModel/` tree, and `benchmarks/`** are dead: dangling `ProjectReference`s to pre-restructure paths, references to removed APIs, none wired into the solution. `dotnet build/test PastyPropellant.sln` never touches them, so they rot silently. | High |
| [TEST-1](#test-1) | The **numerically load-bearing core** (`Optimization` + `Computation`) has **zero live automated tests** in the buildable solution. | High |
| [BUG-1](#bug-1) | `PressureTablesReport` **silently drops the "Inter-Pocket Kinetic Flame Height" row** from every pressure table in every generated PDF. | High |
| [BUG-2](#bug-2) | `ProcessErrorLogEvent` has **no subscribers** — all stderr from Python worker subprocesses is silently discarded; only a generic non-zero-exit exception ever surfaces. | High |
| [DEAD-2](#dead-2)/[DUP-1](#dup-1) | ~1200 LOC of unreachable non-group report stack **plus** a ~500-line byte-identical `PerformanceMeterReport` duplicate. | High |
| [DUP-2](#dup-2) | The RegionMapper→Porosity→Thermo Python pipeline appears **orphaned from `Program.cs`**, yet its two packages reparse the same JSON with **already-drifted** duplicate schemas + a byte-identical molar-mass table. | High |

---

## A. Architecture & coupling  *(primary focus)*

<a id="arch-1"></a>**ARCH-1 · High · L** — God-file `Program.cs`.
[Program.cs](../src/dotnet/Apps/src/PastyPropellant.ConsoleApp/Program.cs) is a ~630-line top-level-statement `Main` that mixes CLI parsing, DE bounds/strategy selection, penalty wiring, vector-file IO, plot rendering and PDF orchestration with no class boundaries; its local functions (`RunGroupOptimizationAsync`, `RunForwardEvalAsync`, `ReadVectorFile`, …) are unreachable from the `.Tests` project. → Extract `BoundsProvider`, `ScenarioRunner`, `VectorFileService`.

<a id="arch-2"></a>**ARCH-2 · Med · M** — Optimizer duplication + non-polymorphic interface.
[DifferentialEvolutionOptimizer.cs](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Optimization/Optimizers/DifferentialEvolutionOptimizer.cs) and [GroupDifferentialEvolutionOptimizer.cs](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Optimization/Optimizers/GroupDifferentialEvolutionOptimizer.cs) duplicate the whole `RunAsync`/`Evaluate` control flow; only the group one has the NM refinement. `IParametricCombustionModelOptimizer` is implemented only by the single-composition optimizer (group returns a different result type), so they can't be treated polymorphically and every change must be hand-duplicated. → Generalize to `IParametricCombustionModelOptimizer<TResult>` or extract the shared DE-run/NM-polish steps.

<a id="arch-3"></a>**ARCH-3 · Med · M** — `NotImplementedException` stubs from mis-inheritance.
[PocketPropellantSolver.cs:17-180,187-350](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Solvers/PocketPropellantSolver.cs#L17) — `KineticSkeletonHelper`/`KineticOutSkeletonHelper` inherit a solver base purely to reuse `GetKineticFlameHeatFlux`, then must stub 4 `Visit`/`GetSurfaceHeatFluxesError` overrides with `throw new NotImplementedException()`. They're never dispatched through `ISolverVisitor`. ISP/LSP leak. → Extract the kinetic-flame math into a plain composed helper.

<a id="arch-4"></a>**ARCH-4 · Med · M** — Leaky plot-renderer base.
[GroupBurningRatePlotRenderer.cs:40-43](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.PlotRenderer/Renderers/GroupBurningRatePlotRenderer.cs#L40) and [GroupBurnRateLogLogPlotRenderer.cs:46-49](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.PlotRenderer/Renderers/GroupBurnRateLogLogPlotRenderer.cs#L46) override the base single-result `Render(...)` only to `throw NotSupportedException`. Every future group renderer repeats the stub. → Split the abstraction into single-result vs group-result variants.

<a id="arch-5"></a>**ARCH-5 · Med · S/M** — Hardcoded 5-level relative Python paths.
[Program.cs:479,600,608](../src/dotnet/Apps/src/PastyPropellant.ConsoleApp/Program.cs#L479) invoke `"../../../../../src/python/..."` verbatim (duplicated for both entry points); correct only when run from the bin output dir, breaks under any other cwd with no compile signal. → Single repo-root resolver.

<a id="arch-6"></a>**ARCH-6 · Med · S** — `python3` hardcoded, unquoted args.
[PythonRegionMapper.cs:13,58-60](../src/dotnet/Tools/PastyPropellant.RegionMapper/src/PastyPropellant.RegionMapper/Mappers/PythonRegionMapper.cs#L13) (and the Porosity/Thermo/Plots wrappers) hardcode the interpreter and interpolate args into one raw command string; spaces in paths or a host without `python3` on PATH break silently.

<a id="arch-7"></a>**ARCH-7 · Med · S** — No experiment-config surface; values baked into `Program.cs`.
The DE strategy choice ([Program.cs:196](../src/dotnet/Apps/src/PastyPropellant.ConsoleApp/Program.cs#L196)), NM tuning, the 6 penalty thresholds ([:151-181](../src/dotnet/Apps/src/PastyPropellant.ConsoleApp/Program.cs#L151)), and even the input file `"propellants.01234.json"` ([:511,532](../src/dotnet/Apps/src/PastyPropellant.ConsoleApp/Program.cs#L511)) are hardcoded literals rather than sourced from `data/optimization_tickets.json` like the rest of the app; every experiment variation needs a recompile. Related: 5 propellants' thermal-conductivity λ coefficients are a hardcoded dict in [ConstructPropellantJsonHelper.cs:25-31](../src/dotnet/Apps/src/PastyPropellant.ConsoleApp/Helpers/ConstructPropellantJsonHelper.cs#L25).

<a id="arch-8"></a>**ARCH-8 · Med · S** — Composition-name array copy-pasted 6×.
The `["Bas_2 (…3,4)", "Bas_1", "Bas_0"]` ordering (the `compositionIndex` layout) is duplicated verbatim in [BurnRateErrorReport.cs:25](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.ReportMaking/Reports/Pdf/BurnRateErrorReport.cs#L25), [BurnRateVieilleFitReport.cs:28](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.ReportMaking/Reports/Pdf/BurnRateVieilleFitReport.cs#L28), [FlameStructureReport.cs:30](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.ReportMaking/Reports/Pdf/FlameStructureReport.cs#L30), [GroupCombustionSolverParamsReport.cs:81](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.ReportMaking/Reports/Pdf/GroupCombustionSolverParamsReport.cs#L81), and Program.cs:440,549. Any regrouping must edit all 6 in sync with no compiler help. → One shared constant in Core.

<a id="arch-9"></a>**ARCH-9 · Low · S** — Ollama endpoint/model hardcoded.
[checker.py:10](../checker.py#L10) and [projecthandler.py:8-9](../projecthandler.py#L8) bake in base URL + model name with no env override.

<a id="arch-10"></a>**ARCH-10 · Low · S** — `PropellantsPlotRendering` has no `--output-dir`.
[main.py:171-196](../src/python/PropellantsPlotRendering/src/main.py#L171) writes PNGs with bare relative names (unlike sibling RegionMapper), making output location an implicit contract with the host cwd.

---

## B. Dead & duplicated code  *(primary focus)*

<a id="dead-1"></a>**DEAD-1 · High · L** — The entire legacy test/benchmark tree is dead.
None of these are in [PastyPropellant.sln](../PastyPropellant.sln) (26 projects; 33 `.csproj` on disk), and all reference pre-restructure paths and/or removed types, so they cannot compile against current source:
- `tests/ParametricCombustionModel.{Computation,Optimization}.Test/` and the near-duplicate nested `tests/ParametricCombustionModel/{Computation.Test, Optimization.Test, Test.Share, TestPocketSurface}/` — reference removed APIs (`OptimizationUpdatedEvent`, `MetricsGlobalSearchOptimizer`, `OptimizationProblemContextByUnits`, `ProblemContextByUnitsMatrixBuilder.ForPressures`, `TargetFunctionSolver`, `BurningPropellantSolvers.InterPocketPropellantSolver`, `Computation.Utils`).
- [tests/DifferentialEvolution.Optimizer.Test](../tests/DifferentialEvolution.Optimizer.Test/) and [tests/PDFsharp.Api.Test](../tests/PDFsharp.Api.Test/) — point at `..\..\src\Optimizers\...` / `..\..\src\PDFsharp\...` (DE is now the DotNetDifferentialEvolution NuGet; PDFsharp.Api lives under `externals/`).
- [tests/MATLAB.Api.Test](../tests/MATLAB.Api.Test/) — references a nonexistent `MatlabGlobalSearch.Api` + Windows-only MATLAB R2023b HintPaths + a missing `Optimize.dll`; also pins the oldest test-SDK/xunit versions in the repo.
- [benchmarks/…/Computation.Benchmark.csproj:15](../benchmarks/ParametricCombustionModel/ParametricCombustionModel.Computation.Benchmark/ParametricCombustionModel.Computation.Benchmark.csproj#L15) — stale `ProjectReference`; the `dotnet run … benchmarks/…` command in CLAUDE.md fails immediately.
→ Delete both dead trees (porting any still-relevant assertions into new in-solution projects first) and either fix or drop the benchmark.

<a id="dead-2"></a>**DEAD-2 · High · M** — Unreachable non-group report stack (~1200 LOC), partially stale.
[PdfReportMaker.cs](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.ReportMaking/ReportMakers/PdfReportMaker.cs), [TextReportMaker.cs](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.ReportMaking/ReportMakers/TextReportMaker.cs), [ReportContextDto.cs](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.ReportMaking/Models/ReportContextDto.cs), all of `Reports/Text/*`, and `Reports/Pdf/CombustionSolverParamsReport.cs` (a ~230-line 18-param copy-paste that never got the `AddParam` refactor its group twin already has) are reachable only via the never-constructed `PdfReportMaker`/`TextReportMaker`. The `Reports/Text/*` versions have already drifted from their Pdf twins (e.g. [Text/CombustionSolverParamsReport.cs:43](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.ReportMaking/Reports/Text/CombustionSolverParamsReport.cs#L43) — commented-out `Nu`, missing 6 of 18 params). → Delete after confirming no ticket/worker path builds `ReportContextDto`.

<a id="dup-1"></a>**DUP-1 · High · M** — ~500-line duplicate `PerformanceMeterReport`.
[PerformanceMeterReport.cs:50-541](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.ReportMaking/Reports/Pdf/PerformanceMeterReport.cs#L50) and [GroupPerformanceMeterReport.cs:49-541](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.ReportMaking/Reports/Pdf/GroupPerformanceMeterReport.cs#L49) are byte-identical except for the constructor (all the OS-probing: `GetProcessorName`, `Get{Windows,Linux}PhysicalMemory/ProcessorFrequency/MemoryFrequency`, `AddSystemInfo`, …). A `wmic`/`dmidecode` fix must be applied twice; the non-group one is also dead ([DEAD-2](#dead-2)). → Extract one shared OS-probe helper.

<a id="dup-2"></a>**DUP-2 · High · M** — Python duplication + drifted schemas + possibly-orphaned pipeline.
- [PorosityCalculation/molar_masses.py](../src/python/PorosityCalculation/src/molar_masses.py) == [RegionMapper/molar_masses.py](../src/python/RegionMapper/src/molar_masses.py) — byte-identical 147-line file.
- `PropellantComponent` is duplicated with a **drifted contract**: [PorosityCalculation/models.py:26-29](../src/python/PorosityCalculation/src/models.py#L26) requires `density` (bracket access) while [RegionMapper/models.py:48-50](../src/python/RegionMapper/src/models.py#L48) omits/drops it — both read the *same* `propellants.json`. `RegionCalculationResult` is likewise hand-rolled twice ([PorosityCalculation/models.py:66-79](../src/python/PorosityCalculation/src/models.py#L66) vs [RegionMapper/calculators.py:12-25](../src/python/RegionMapper/src/calculators.py#L12)).
- **Load-bearing question first:** the whole RegionMapper→Porosity→Thermo subprocess pipeline is defined in [PreparePropellantDataHelper.cs:43](../src/dotnet/Apps/src/PastyPropellant.ConsoleApp/Helpers/PreparePropellantDataHelper.cs#L43) and kept alive by 3 `ProjectReference`s, but is instantiated only from its own tests — `Program.cs`'s live entry point never calls it. → Decide whether this pipeline is still used *before* investing in a shared-schema fix; if dead, this is a large removable block.

<a id="dup-4"></a>**DUP-4 · Med · S** — Scenario matrix methods duplicated.
`GetProblemContextMatrixByDoubles/ByUnits` are byte-identical private statics in [DifferentialEvolutionScenario.cs:102-128](../src/dotnet/Apps/src/PastyPropellant.ConsoleApp/Scenarios/DifferentialEvolutionScenario.cs#L102) and [GroupDifferentialEvolutionScenario.cs:137-157](../src/dotnet/Apps/src/PastyPropellant.ConsoleApp/Scenarios/GroupDifferentialEvolutionScenario.cs#L137).

<a id="dup-5"></a>**DUP-5 · Med · S** — Python-invocation boilerplate ×4.
The four `Tools/*` Python wrappers (RegionMapper, Thermodynamics, Porosity, Plots) each reimplement `const PythonPath="python3"` + try/catch + `ExecutePythonScriptAsync`→`ProcessHandler.RunProcessAsync` with no shared base; adding an interpreter override or timeout means editing all four. (Overlaps [ARCH-6](#arch-6).)

<a id="dup-6"></a>**DUP-6 · Med · M** — Per-composition/fuel report skeleton triplicated.
[BurnRateErrorReport.cs](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.ReportMaking/Reports/Pdf/BurnRateErrorReport.cs), [BurnRateVieilleFitReport.cs](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.ReportMaking/Reports/Pdf/BurnRateVieilleFitReport.cs), [FlameStructureReport.cs](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.ReportMaking/Reports/Pdf/FlameStructureReport.cs) share the same header→per-composition→per-fuel skeleton (~40 dup lines each). → Extract a `PerCompositionPerFuelPdfReport` base.

<a id="dup-7"></a>**DUP-7 · Med · M** — Group plot renderers near-identical.
[GroupBurningRatePlotRenderer.cs:60-131](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.PlotRenderer/Renderers/GroupBurningRatePlotRenderer.cs#L60) and [GroupBurnRateLogLogPlotRenderer.cs:64-112](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.PlotRenderer/Renderers/GroupBurnRateLogLogPlotRenderer.cs#L64) duplicate the `CompositionContexts[0..2]×fuels` loop (skipping `Bas_21`/`Bas_22`) and each redefines its own private `AddLineSeries` overload. → Shared data-collection helper.

<a id="dup-8"></a>**DUP-8 · Med · S** — Area/volume-fraction copy-paste.
[PropellantExtensions.cs:26-54](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Extensions/PropellantExtensions.cs#L26) (`GetInterPocketAreaVolumeFraction`) and `:68-96` (`GetPocketAreaVolumeFraction`) are 24-line copies differing only by a `(1-PocketMassFraction)` vs `PocketMassFraction` factor.

<a id="dup-9"></a>**DUP-9 · Med · M** — Agglomeration polynomial duplicated in Python.
[RegionMapper/region_mappers.py:299-313](../src/python/RegionMapper/src/region_mappers.py#L299) vs [PropellantsPlotRendering/main.py:44-53](../src/python/PropellantsPlotRendering/src/main.py#L44) implement the same fit twice with a code-commented behavioral split (unclamped vs clamped to [0,100]) nobody reconciled.

<a id="dup-10"></a>**DUP-10 · Low · M** — csproj boilerplate duplicated ~20×.
`<TargetFramework>net10.0</>`, `<ImplicitUsings>enable</>`, `<Nullable>enable</>` are repeated in ~20 project files instead of living once in [Directory.Build.props](../Directory.Build.props) (which currently only centralizes output paths). (See [BUILD-1](#build-1).)

<a id="dead-3"></a>**DEAD-3 · Med · S** — Empty / unwired plot renderers.
[ThermalConductivityPlotRenderer.cs](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.PlotRenderer/Renderers/ThermalConductivityPlotRenderer.cs) is a 0-byte file; `PocketSurfaceFractionPlotRenderer` is never instantiated and its body is commented-out returning an empty dict; the non-group `BurningRatePlotRenderer` is never instantiated. → Delete or finish/wire.

<a id="dead-4"></a>**DEAD-4 · Med · S** — Dead metal-burning-temperature path.
[PocketPropellantSolver.cs:530-540,893-902](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Solvers/PocketPropellantSolver.cs#L530) — `GetAverageMetalBurningTemperature` is implemented twice but never called (both call sites commented at :462/:834), and `AverageMetalBurningTemperature` is hardcoded to `Temperature.Zero`. The field in [PocketCombustionParams.cs:52,170](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Models/ComputedParams/PocketCombustionParams.cs#L52) is now inert. → Wire back in or delete both.

<a id="dead-7"></a>**DEAD-7 · Med · S** — Generated artefacts committed at repo root. **[verify tracked]**
`out.txt` (472 KB — a `projecthandler.py` dump), [combined_analysis_report_1751538106.json](../combined_analysis_report_1751538106.json) (45 KB) and [unitsnet_analysis_report_1751533475.json](../unitsnet_analysis_report_1751533475.json) (42 KB) are generated outputs living in VCS root; CLAUDE.md itself calls them "outputs, not inputs". → Move to `artifacts/` and add an ignore pattern. Also [data/hh.txt](../data/hh.txt) is a 0-byte stray file → remove.

<a id="dead-5"></a>**DEAD-5 · Low · S** — Commented-out dead lines.
[DifferentialEvolutionOptimizer.cs:108](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Optimization/Optimizers/DifferentialEvolutionOptimizer.cs#L108) (`// FitnessFunctionSolver.Visit…`), the `// rightValue = middleValue;` leftover in all 4 bisection copies (`BasePropellantSolver.cs:220,373`, `PocketPropellantSolver.cs:627,984`), and [PorosityCalculation/calculators.py:8](../src/python/PorosityCalculation/src/calculators.py#L8) (`ALUMINUM_TEMPERATURE_FACTOR`).

<a id="dead-6"></a>**DEAD-6 · Low · S** — Small unused members.
`OxyColorExtensions.Darken` ([:13](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.PlotRenderer/Extensions/OxyColorExtensions.cs#L13)); the `ReportContextDto` ctor field in [DifferentialEvolutionSettingsReport.cs:14,19](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.ReportMaking/Reports/Pdf/DifferentialEvolutionSettingsReport.cs#L14) (never read); `GroupReportContextDto.Propellants` ([:16](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.ReportMaking/Models/GroupReportContextDto.cs#L16), set but never read inside ReportMaking).

---

## C. Correctness  *(surfaced despite lower priority weight)*

<a id="bug-1"></a>**BUG-1 · High · S** — Dropped pressure-table row (shipping).
[PressureTablesReport.cs:136-146](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.ReportMaking/Reports/Pdf/PressureTablesReport.cs#L136) builds the "Inter-Pocket Kinetic Flame Height" row but omits the `rows.Add(row.ToList());` call before `row` is reassigned, so that row is silently missing from every pressure table in every generated PDF (including the live group report). → One-line fix (add the missing `rows.Add` after the loop at ~:143); a row-count/snapshot test would have caught it.

<a id="bug-2"></a>**BUG-2 · High · S** — Worker stderr silently discarded.
[ProcessHandler.cs:39-46](../src/dotnet/Common/src/PastyPropellant.ProcessHandling/ProcessHandlers/ProcessHandler.cs#L39) publishes `ProcessErrorLogEvent` per stderr line, but there are **zero subscribers** anywhere in `src/dotnet`; all diagnostic output from the region-mapper/thermo/porosity/plot subprocesses is lost — only a generic non-zero-exit exception surfaces. → Subscribe (log) or remove the event and surface stderr in the failure exception.

<a id="cor-1"></a>**COR-1 · Med · S** — `EventBus` data race.
[EventBus.cs:6-21](../src/dotnet/Common/src/PastyPropellant.Core/Utils/EventBus.cs#L6) mutates a plain `List<Action<TEvent>>` in `Subscribe`/`Unsubscribe`/`Publish` with no lock, while [PreparePropellantDataHelper.cs:126-165](../src/dotnet/Apps/src/PastyPropellant.ConsoleApp/Helpers/PreparePropellantDataHelper.cs#L126) publishes from inside a `Parallel.ForEach` as the main thread sub/unsubscribes — a real concurrent-mutation race, not style. (The static-bus *pattern* is intentional; the missing synchronization is the debt.)

<a id="cor-2"></a>**COR-2 · Med · S** — ByDoubles/ByUnits parity mismatch.
[InterPocketPropellantSolver.cs:133](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Solvers/InterPocketPropellantSolver.cs#L133) — the ByDoubles `Visit()` sets `BurnRateIsFound = BurnRate > 0.0`; the ByUnits `Visit()` (:24-41) has no equivalent, and `PocketPropellantSolver.Visit` (both variants) also lacks it. → Align the pair (project's `ByDoubles`/`ByUnits` parity convention).

<a id="cor-3"></a>**COR-3 · Low · S** — Misleading extension signature.
[PropellantExtensions.cs:151-155](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Extensions/PropellantExtensions.cs#L151) — `GetMetalMeltingTemperature(this Propellant)` ignores its argument and returns a hardcoded `1300` (no unit/source), yet is called live by both matrix builders. → Drop the unused parameter or make it composition-dependent.

<a id="cor-4"></a>**COR-4 · Low · S** — Hardcoded, duplicated surface-temperature search bounds.
The 600 K / 900 K binary-search bounds are duplicated record-field initializers with no config surface or provenance in [ProblemContextByUnits.cs:98,105](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Models/ProblemContexts/ProblemContextByUnits.cs#L98) and `ProblemContextByDoubles.cs:97,104`.

<a id="cor-5"></a>**COR-5 · Low · S** — No cancellation / no wait timeout in `ProcessHandler`.
[ProcessHandler.cs:10-72](../src/dotnet/Common/src/PastyPropellant.ProcessHandling/ProcessHandlers/ProcessHandler.cs#L10) — `RunProcessAsync` takes no `CancellationToken` and blocks on `WaitForExit()` with no timeout; a hung worker leaves the returned `Task` unresolvable forever.

<a id="cor-6"></a>**COR-6 · Low/Med · S** — Missing guards in Python.
[region_mappers.py:148,205](../src/python/RegionMapper/src/region_mappers.py#L148) uses `large_particles_fraction` in arithmetic with no None-guard (validator only checks `mass_fraction`) → unhandled `TypeError`; [PorosityCalculation/calculators.py:37](../src/python/PorosityCalculation/src/calculators.py#L37) & `json_reader.py:15` use bracket access → bare `KeyError`; [checker.py:107-113](../checker.py#L107) assumes the Ollama reply is a dict and aborts the batch otherwise.

---

## D. Tests

<a id="test-1"></a>**TEST-1 · High · L** — Numerical core has no live tests.
[PastyPropellant.sln](../PastyPropellant.sln) wires only `ParametricCombustionModel.Core.Tests`, `.Telemetry.Tests`, `.PlotRenderer.Tests`. Neither DE optimizer, none of the 7 constraint-penalty evaluators, and none of the 4 solvers have live coverage — the only tests that once did are the dead trees in [DEAD-1](#dead-1). The optimization campaign's load-bearing math ships with no regression net.

**TEST-2 · Med · M** — No tests for `Common` primitives. Neither `PastyPropellant.Core` nor `PastyPropellant.ProcessHandling` has a `tests/` dir, despite `EventBus`/`ProcessHandler` being the shared static primitives (and the site of [BUG-2](#bug-2)/[COR-1](#cor-1)).

**TEST-3 · Med · M** — No tests for `Reports/Pdf/*` (~19 classes) despite real numerics (Vieille OLS+R², RMS aggregation, heat-flux shares, rail-flag boundaries). [BUG-1](#bug-1) would have been caught by a minimal snapshot test.

**TEST-4 · Med · S** — Mis-targeted test file. [PreparePropellantDataHelperTest.cs:71-206](../src/dotnet/Apps/tests/PastyPropellant.ConsoleApp.Tests/PreparePropellantDataHelperTest.cs#L71) exercises raw MigraDoc rendering and never touches the class it's named for; the actual class gets only 2 slow external-process tests.

**TEST-5 · Med · M** — Zero tests for any `src/python/*` package (no `test_*.py`, no pytest config); correctness rests entirely on C#-side black-box integration.

**TEST-6 · Low · —** — `PlotRenderer.Tests` covers only JPEG-export plumbing (not the Bas_21/22-skip data selection); `PropellantsPlotRendering` is the only Tools project with no tests.

---

## E. Build / tooling / config

<a id="build-1"></a>**BUILD-1 · Low · M** — [Directory.Build.props](../Directory.Build.props) centralizes only output paths; TFM/Nullable/ImplicitUsings are duplicated across ~20 csproj (see [DUP-10](#dup-10)).

**BUILD-2 · High · S** — Secrets file not ignored. **[verify tracked]** A 21-byte [.env](../.env) sits at repo root and is **not matched by `.gitignore`**; given the previously-revoked-PAT incident, an un-ignored secrets file is a leak hazard. → Add `.env` to `.gitignore` and confirm it isn't already tracked.

**BUILD-3 · Low · S** — No `requirements.txt`/`pyproject.toml` for the Python packages despite `matplotlib`/`requests` deps — unpinned, undocumented.

**BUILD-4 · Low · S** — [src/python/RegionMapper/.DS_Store](../src/python/RegionMapper/) is tracked with no `.DS_Store` ignore rule. **[verify tracked]**

---

## F. Docs / provenance

<a id="doc-1"></a>**DOC-1 · High · M** — `CLAUDE.md` architecture is fictional.
Both the "Architecture worth knowing up front" and "Process model" sections describe `Program.cs → Initializer → OptimizationControllerBuilder → OptimizationController`, per-task **worker processes over named-pipe IPC**, a `worker_path` ticket field, `Computation/Program.cs` as a worker exe, and a `RELEASE`-gated Telegram relay wired in `Initializer`. Grep across `src/dotnet` finds **none** of these: no `Initializer`/`OptimizationController*`, no `NamedPipe*Stream`, no `OutputType>Exe` in `Computation.csproj`, no Telegram wiring. The real flow is `ConsoleApp/Program.cs` (top-level statements) building `DifferentialEvolutionSettings`/`GroupOptimizationResult` directly and evaluating fitness **in-process** (thread-parallel via `workerIndex`-indexed context arrays). This is foundational doc drift that will misdirect any new work. → Rewrite both sections to match the in-process model.

<a id="doc-2"></a>**DOC-2 · Med · S** — [CLAUDE.md:89](../CLAUDE.md#L89) states the working branch is `global_opt_by_groups`; the real branch is `experiment/surface-900-metal-1300`. → Drop the fixed value or make it a pointer.

**DOC-3 · Low/Med · S** — [data/](../data/) mixes documented runtime inputs with unexplained/legacy files (`TAB.dat`, `clients.json`, `propellant_components.json`, `substances_file.json`, empty `hh.txt`) with no manifest of what each run consumes. → Document or prune.

**DOC-4 · Low · S** — [RegionMapper/README.md](../src/python/RegionMapper/README.md) and [PorosityCalculation/README.md](../src/python/PorosityCalculation/README.md) are effectively empty despite rich in-code docstrings.

**DOC-5 · Low · S** — [checker.py:249-250](../checker.py#L249) argparse help advertises default `codellama:7b-instruct` while the actual `default=` is `gemma3:12b`.

**DOC-6 · Low · S** — [docs/de-tuning-todo.md](de-tuning-todo.md) may be superseded by current DE settings — review/retire.

---

## Cross-cutting themes

1. **A repository restructure was done but the periphery wasn't migrated.** The dead test trees ([DEAD-1](#dead-1)), the stale benchmark, and the fictional architecture docs ([DOC-1](#doc-1)) all point at the same event: `src/` moved under `src/dotnet/...` and several projects were replaced by NuGet packages, but the tests, benchmarks, and CLAUDE.md were never brought along. This is the single largest cleanup lever.
2. **The "Group" variants were added by copy-paste.** Optimizer ([ARCH-2](#arch-2)), performance-meter report ([DUP-1](#dup-1)), plot renderers ([DUP-7](#dup-7)), scenario matrix methods ([DUP-4](#dup-4)) — each new group feature duplicated a whole class rather than sharing a base. This is exactly the pattern CLAUDE.md warns about, and it keeps compounding.
3. **`Program.cs` is absorbing responsibility that belongs in libraries** ([ARCH-1](#arch-1), [ARCH-5](#arch-5), [ARCH-7](#arch-7)), which is also why the core is untestable ([TEST-1](#test-1)).
4. **Cross-language dual maintenance** — RegionMapper/Porosity/Thermodynamics exist in both C# and Python with drifting contracts ([DUP-2](#dup-2)); resolve whether the Python pipeline is still live before deepening the coupling.

## Explicitly *not* flagged (intentional / load-bearing — leave as-is)
- The 32-element group parameter vector layout (`shared[0..10] / Bas_2[11..17] / Bas_1[18..24] / Bas_0[25..31]`) and the mandatory grouping of `Bas_2/3/4`.
- The `ByDoubles`/`ByUnits` method-pair convention (only *parity mismatches* are flagged, e.g. [COR-2](#cor-2)).
- The static `EventBus<TEvent>` as an extension point (only the missing lock, [COR-1](#cor-1), is flagged).

---
*Generated from a read-only audit; no source was modified. Items marked **[verify tracked]** need a `git ls-files` check to confirm the file is committed vs merely present on disk.*
