# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

PastyPropellant is a multi-language toolkit for analysing, optimising, and reporting on **liquid (pasty) propellant performance**. The primary runtime is a single .NET console host that runs one parametric combustion-model optimisation **in-process**, thread-parallel across the machine's cores, then renders plots and a PDF report. Python scripts (invoked as child processes) supply plot generation and auxiliary numerical kernels.

⚠ [QWEN.md](QWEN.md) and [docs/architecture.md](docs/architecture.md) still describe a superseded multi-process / named-pipe worker architecture (`Initializer`, `OptimizationController`, `worker_path` tickets, C++ workers). None of that exists in the current tree — treat those two documents as historical background only, and prefer this file. [docs/math-model.md](docs/math-model.md) is current and is the reference for the physics.

## Build, test, run

Most projects target **.NET 10** (`global.json` pins SDK 9.0+ with `rollForward: latestMajor`). Build artefacts are centralised in [artifacts/bin/](artifacts/) and [artifacts/obj/](artifacts/) via [Directory.Build.props](Directory.Build.props) — do not look under each project's own `bin/obj` folder.

```bash
dotnet restore PastyPropellant.sln
dotnet build   PastyPropellant.sln
dotnet test    PastyPropellant.sln
```

Run a single test project / single test:
```bash
dotnet test src/dotnet/ParametricCombustionModel/tests/ParametricCombustionModel.Core.Tests
dotnet test --filter "FullyQualifiedName~SomeTestClass.SomeMethod"
```

⚠ **A plain `dotnet test PastyPropellant.sln` takes tens of minutes.** Several Tools tests shell out
to the real Python helpers, and `PythonThermodynamicsCalculatorTest` (marked
`[Trait("Category", "LongRunning")]`) runs a full thermodynamics solve. For a fast signal use:
```bash
dotnet test PastyPropellant.sln --filter "Category!=LongRunning"
```
Historically these tests appeared to finish in ~110 ms — but only because their hardcoded
`../../../../../` paths, broken by the centralised `artifacts/` output layout, made them fail before
launching anything. They now resolve the repository root via `PythonRuntime` and genuinely execute.

Run the console host. A run configuration file is **optional**: with none present the host runs the built-in defaults, which are the values that used to be hardcoded (see *Runtime data* below).

```bash
# Full optimisation run (hours; 9 h safety timeout)
dotnet run --project src/dotnet/Apps/src/PastyPropellant.ConsoleApp

# Forward-eval: score + report ONE supplied 32-parameter vector, no DE search (seconds)
dotnet run --project src/dotnet/Apps/src/PastyPropellant.ConsoleApp -- --forward-eval best_vector.txt
```

`Program.cs` parses two arguments: `--forward-eval <vector-file>` and `--config <path>`. Anything else is ignored and the full optimisation runs.

Every run writes `run_configuration.resolved.json` recording the configuration it **actually used** — effective values after defaults are applied and after the processor-count reduction runs, not what the file requested. It is written *before* the search starts, so a killed or timed-out run still leaves the record. The same content appears as a block at the top of the PDF. That record is the reason a config file is acceptable here at all: hardcoding made every run traceable through git, and the resolved record is what replaces that guarantee.

The host resolves `propellants.01234.json` **relative to the process working directory** and throws `FileNotFoundException` if it is missing. The checked-in copy lives at `data/propellants.01234.json`, which is not where `dotnet run` starts, so make the file reachable from the working directory before running (the sibling Python script paths are hardcoded relative to the ConsoleApp project directory, so that is the working directory the relative paths assume).

There is currently no benchmark project; the former `benchmarks/` tree was unbuildable and has been removed.

Python helpers (e.g. plot rendering):
```bash
python src/python/PropellantsPlotRendering/src/main.py
```

The repository uses a git submodule at [externals/src/python/AerospacePropellantThermodynamics](externals/). Run `git submodule update --init --recursive` after a fresh clone.

## Architecture worth knowing up front

**Solution layout** — the .NET tree is grouped by *role*, not by deployable, so a single feature usually touches several projects:

- [src/dotnet/Apps/src/PastyPropellant.ConsoleApp](src/dotnet/Apps/src/PastyPropellant.ConsoleApp/) — the only entry-point executable (`OutputType>Exe`). `Program.cs` is a ~600-line top-level-statements file: local functions build the bounds and penalty set, then `GroupDifferentialEvolutionScenario` runs the campaign. Also holds the termination strategies (`CustomStagnationStreakTerminationStrategy`, `TimeoutTerminationStrategy`, `OrTerminationStrategy`) and the `Helpers/` that shell out to Python.
- [src/dotnet/ParametricCombustionModel/src/](src/dotnet/ParametricCombustionModel/src/) — all **class libraries**, split into `Core` (DTOs/contracts), `Computation` (numerical kernels, problem contexts, `MixedPropellantSolver` — library only, it has no `Program.cs` and is not an executable), `Optimization` (DE/NM optimisers, fitness + penalty evaluators, result adapters), `PlotRenderer`, `ReportMaking`, `Telemetry`.
- [src/dotnet/Common/src/PastyPropellant.Core](src/dotnet/Common/src/PastyPropellant.Core/) — shared primitives, notably `EventBus<TEvent>` and `OperationResult<T>`.
- [src/dotnet/Common/src/PastyPropellant.ProcessHandling](src/dotnet/Common/src/PastyPropellant.ProcessHandling/) — a thin `ProcessHandler` plus two log-event records. Its only consumers are the `Tools/` libraries and the ConsoleApp helpers that launch **Python** scripts; there is no IPC layer and no optimisation worker.
- [src/dotnet/Tools/](src/dotnet/Tools/) — independent utility libraries (`RegionMapper`, `Thermodynamics`, `PorosityCalculation`, `PropellantsPlotRendering`) with their own tests; they don't depend on the optimiser.

**Execution model — everything is in-process.** There is no worker process, no IPC, and no job queue. `Program.cs` builds a `DifferentialEvolutionScenarioSettings` (population, bounds, strategy, termination, penalty evaluators, processor count, Nelder–Mead settings), hands it to `GroupDifferentialEvolutionScenario`, and that constructs a `GroupDifferentialEvolutionOptimizer`. The optimiser implements `IFitnessFunctionEvaluator` and passes *itself* to `DifferentialEvolutionBuilder.ForFunction(this)` from the **`DotNetDifferentialEvolution` 5.1.0** NuGet package (with `DotNetNelderMead` / `DotNetNelderMead.DifferentialEvolution` for refinement). `UseProcessors(n)` makes the DE library run `n` in-process workers.

Two consequences of the 5.x upgrade are worth knowing before touching the optimiser:

- **A run can now be made bit-for-bit reproducible** via `.WithSeed(n)` — the engine derives one RNG stream per worker, so reproducibility no longer costs single-threading (4.x only managed it at `UseProcessors(1)`). The seed is off by default. It reproduces a run *only at the same worker count*, because individual *i* draws from worker *i mod W*'s stream, so `run_configuration.resolved.json` and the PDF header record the seed and the effective worker count together — either alone does not identify a run.
- **The in-loop memetic Nelder–Mead refiner is unavailable.** `DotNetNelderMead.DifferentialEvolution` 1.0.0 is compiled against DE 4.0.0 and calls `ProblemContext.Population` / `.PopulationFfValues`, both removed in 5.1.0. It links and loads fine and fails only when the refiner first fires, so `NelderMeadRefinementSettings.Validate` rejects `MemeticInLoop` up front. The **final polish is unaffected** — it goes through `DotNetNelderMead` directly and never touches a DE type. Re-enabling is one `throw` to delete once an adapter built against 5.x ships.

**Parallelism is by pre-allocated per-worker context.** The concurrency contract lives in the shape of one array. `GroupDifferentialEvolutionScenario` builds `OptimizationProblemByDoubles[processorCount, 3]`, giving **every worker its own freshly built copy** of each group's problem-context matrix — the contexts are mutated during a solve, so sharing them across threads would corrupt results. `GroupDifferentialEvolutionOptimizer.Evaluate(int workerIndex, ReadOnlySpan<double> genes)` indexes `_compositionProblems[workerIndex, groupIdx]`, which is what keeps the threads from colliding. **Any new mutable per-solve state must be added to that matrix, not to a field on the optimiser.**

**Fitness aggregation.** One evaluation splits the 32-vector into three 18-parameter composition vectors via `GroupCombustionSolverParamsByDoubles.ToCompositionVector(groupIdx)`, solves each group, and returns `mean(fitness over the 3 groups) + total penalty`. If any group returns `double.MaxValue` the whole evaluation short-circuits to `double.MaxValue`. Five penalty evaluators are applied (pocket heat-flux ratio competition, inter-pocket faster burn, kinetic-flame heat flux, pore diameter, large oxidiser particle size); `BuildGroupPenaltyEvaluators()` is single-sourced so the optimisation and forward-eval paths report identical penalties.

**Final evaluation and forward-eval share one code path.** After DE converges (and the optional final Nelder–Mead polish, which is guarded to keep the DE solution unless the refined fitness is at least as good), the optimiser calls `EvaluateVector(bestGenes)` — the same public method `--forward-eval` uses. It replays the vector through the UnitsNet contexts (`OptimizationProblemByUnits[3]`) and returns the fully populated `GroupOptimizationResult`. Keep it that way: a reported optimisation result and a forward-eval of the same vector must be bit-identical.

**Event bus.** `EventBus<TEvent>` is a *static, process-wide* pub/sub keyed by event type, constrained to `struct` events. Components publish typed events (`InfoLogEvent`, `ProcessInfoLogEvent`, …); `Program.cs` subscribes a console writer for `InfoLogEvent` at startup. Because subscriptions are static and the handler list is unsynchronised, tests that exercise the bus must clean up handlers. Adding a new event type is the canonical way to surface new lifecycle information — don't add ad-hoc logger interfaces.

**Group optimisation parameter layout.** When several propellant compositions share an optimisation run, the parameter vector is laid out as a single 32-element array:

| Slot | Meaning |
|------|---------|
| `[0..10]` | 11 shared parameters (optimised once across all compositions) |
| `[11..17]` | 7 parameters specific to the `Bas_2 + Bas_3 + Bas_4` group (`compositionIndex = 0`) |
| `[18..24]` | 7 parameters specific to `Bas_1` (`compositionIndex = 1`) |
| `[25..31]` | 7 parameters specific to `Bas_0` (`compositionIndex = 2`) |

The same ordering is used for `compositionIndex` everywhere downstream — `GroupCombustionSolverParamsByDoubles.ToCompositionVector(idx)`, the per-group context arrays in `GroupOptimizationResult.CompositionContexts`, and the merged matrix produced by `GroupOptimizationResultAdapter`. Anything that splices, bounds-checks, or reports on this vector must follow the same slicing.

`Bas_2`, `Bas_3`, and `Bas_4` are always optimised together as one group sharing the same composition-specific parameters — that is the load-bearing reason for the grouped 32-parameter formulation.

## Runtime data and run configuration

**There is still no job/ticket file** — in particular `data/optimization_tickets.json` does not exist and never did. What does exist is an optional run configuration read from `run-config.json` in the process working directory, or from `--config <path>`.

The built-in defaults *are* the historical hardcoded configuration: `new RunConfiguration()` has every former literal as a property initialiser, and a file only overrides members of that object. "No config file" is therefore not a fallback branch — it is the same object with nothing overridden, which is why absence cannot drift from the documented behaviour. An annotated template is checked in at `ConsoleApp/run-config.example.json`; it is deliberately not copied to the build output so it cannot become an active config by accident.

Configs may be partial — override one threshold and inherit the rest. Validation is loud and up-front: an unknown JSON member, an unknown bound name, an inverted or out-of-range bound, or a missing `--config` path throws at startup before anything is constructed. Nothing silently falls back to a default.

| Setting | Where the default lives | Config key |
|---------|------------------------|------------|
| Input propellants file (`"propellants.01234.json"`) | `RunConfiguration` | input file |
| 18-element lower/upper bounds | `Configuration/BoundsProvider.cs`, per-slot comments naming each physical parameter and unit | `bounds`, keyed **by parameter name** (not index), validated against `BoundsProvider.BaseParameterNames` |
| 32-element group bounds | derived from the 18-element ones by index maps — shared slots from `[2,3,4,5,8,9,13,14,15,16,17]`, per-composition slots from `[0,1,6,7,10,11,12]` repeated for all three groups | derived, not configurable directly |
| DE strategy, population, processors, termination | `Runners/GroupScenarioRunner.cs` (jDE, population `32×12 = 384`, processors `ProcessorCount−1` reduced until it divides the population evenly, stagnation-streak OR 9 h timeout) | `strategy`, and `fixedPopulation` / `lShade` as separate sub-records — only the branch matching `strategy` is used, but **both are validated**, so a broken setting cannot lie dormant until someone switches strategy |
| RNG seed | `RunConfiguration` (null — unseeded, the historical behaviour) | `differentialEvolution.seed`; only reproduces a run at the same worker count, so both are written to the resolved record |
| Model constants — metal melting temperature, skeleton contact factor | `Computation/Models/KnownParams/ModelConstants.cs` (1300 K, factor 1.0 = no correction) | `model.metalMeltingTemperatureKelvins`, `model.skeletonContactFactor`. Not fitted and not per-propellant, but they change every computed number, which is why they are configuration: a 2300 K campaign used to need a source patch and left no trace in any report |
| Surface-temperature search bracket | `Computation/Models/ProblemContexts/SurfaceTemperatureSearchBounds.cs` (600–900 K) | `model.minSurfaceTemperatureKelvins`, `model.maxSurfaceTemperatureKelvins`. A **soft constraint**, not a handbook constant: the condensed-phase solve reports failure when no root lies inside, so a composition sitting on a bound is a solution the bracket imposed, and moving a bound can move the solve onto a different root. The published line of work used 600–750 K |
| Skeleton surface fraction `f_s` closure | `Computation/Models/KnownParams/SkeletonSurfaceFractionSettings.cs` (`Polynomial` — the historical per-propellant fit) | `model.skeletonSurfaceFraction.mode`, one of `Polynomial` / `EquilibriumCarbon` / `KineticCoverage`; the equilibrium closure reads `model.skeletonSurfaceFraction.equilibriumTableFile`, the kinetic one `model.skeletonSurfaceFraction.kinetic`. See *Skeleton coverage* below |
| Penalty thresholds | `Configuration/GroupPenaltyEvaluatorFactory.cs` | `penalties` |
| Nelder–Mead refinement | `RunConfiguration` (final-polish on, in-loop memetic off) | Nelder–Mead section |

Not configurable, and deliberately so: the λ thermal-conductivity coefficients in `Helpers/ConstructPropellantJsonHelper.cs` are propellant *material data*, not run configuration — they belong with the propellants input under `data/`.

⚠ **`model.skeletonContactFactor` is a calibration constant, not a tuning knob.** δ (the skeleton-layer thickness) enters the model only through the metal heat flux this factor divides, so a value chosen to improve the fit is algebraically the same as switching the δ ≤ d_AP constraint off — the constraint would still print as satisfied while constraining nothing. Pick it once by a stated rule, freeze it, and present any scan explicitly as a sensitivity analysis.

**Skeleton coverage.** `f_s` is the share of the pocket surface carrying a skeleton layer. It weighs `q_metal + q_k^(S)` against `q_k^(OS)` in the pocket energy balance and appears nowhere else, which is why it is configuration and never a search parameter — a coverage the optimiser could move would be algebraically the same as redistributing between the two kinetic-flame pre-exponentials.

Three closures exist. `Polynomial` (default) is the historical per-propellant fit shipped in `propellants*.json`: 22 coefficients across five compositions, byte-identical to `agglomeration_coefficients`, none transferable to an unmeasured composition, four going negative or turning upward outside the fitted 1–6.5 MPa window.

The measurements those polynomials approximate now ship too, as `agglomeration_confidence_intervals` next to `agglomeration_coefficients` — four `Z_a^m` points per composition with error bars, digitised from Babuk's published figure. They give the **measurement uncertainty on the quantity `f_s` is built from directly**: 2.9–16 %, median 6.6 %, which is the right yardstick for judging a closure (the burn-rate `confidence_intervals` had been used as a proxy). ⚠ **They also caught a bad polynomial: `Bas_0`'s reproduced `Bas_3`'s curve, not its own measurements.** Corrected 2026-08-11 by refitting the source figure's *dashed* `Bas_0` curve, so **every `Bas_0` number computed before that date came from the wrong curve** — `f_s(Bas_0)` is unchanged at 1 MPa but up to +9.9 % higher at 5 MPa. The old coefficients and the refit are in [docs/data-manifest.md](docs/data-manifest.md). Note the polynomial lives in **two** places per propellant — `pocket_surface_fraction_coefficients` at the top level and a byte-identical `agglomeration_coefficients` inside `components.Aluminum` — and both must move together.

`EquilibriumCarbon` replaces it with a computation that contains **no fitted numbers, no scale factor and no agglomeration data**: the skeleton is the pocket's aluminium held together by the carbonaceous residue of binder pyrolysis, and `f_s` is an *area* fraction, which Delesse's theorem equates to the volume fraction still solid at the surface — `f_s(p, T_s) = φ_Al + φ_C(s)(T_pore, p)` with `T_pore = (T_s + T_m)/2` (the range the skeleton layer spans — it ends where the metal melts, so `T_m` is `model.metalMeltingTemperatureKelvins`, and the table records which `T_m` it was built for). `φ_Al` is recipe; `φ_C(s)` is what equilibrium leaves condensed. Pressure lowers it because `C(s) + 2H₂ → CH₄` is the only carbon sink here with Δn < 0; Boudouard and steam gasification both go with Δn = +1 and are suppressed by pressure. Every transport-based candidate for the same trend gives the opposite sign, since any length of the form (transport coefficient)/(velocity) shrinks with pressure.

⚠ **Two normalisations of the same idea exist, and a table does not say which it used unless it was written after 2026-08-08.** Delesse gives `f_s = φ_Al + φ_C(s)`, the share of the pocket still solid; the char-yield form gives `f_s = n_C(s)/n_C,total`, the share of the pocket's carbon equilibrium leaves condensed. They differ by roughly `φ_Al`, so mistaking one for the other shifts every coverage. The C# reads only the `coverages` array and cannot tell them apart. Newer tables record `coverageNormalisation` and `poreTemperatureRule`; the shipped `data/skeleton_carbon_equilibrium.json` is `delesse` at `T_pore = (T_s + T_m)/2`, and **no run has used it** — the runs on this line of work predate that pore rule.

`KineticCoverage` is the third closure and the only one with fitted constants: `f_s = 1/(1 + a₀ + a₁·w_fine·(p/p_ref)^m)`, coverage as the steady state of accumulation against burnout of the binder residue rather than as an inventory. Writing both rates per unit skeleton area makes δ cancel identically, so unlike everything else touching the skeleton it is independent of δ and of `skeletonContactFactor`. Its three constants are **calibrated on `Bas_2`/`Bas_3`/`Bas_4` only** — 4.7–8.6 % in-sample RMS, 12.7–20.9 % leaving one composition out — and they **do not describe `Bas_0` or `Bas_1`**, which carry a different binder and are overshot by 13–137 %. Don't quote a `Bas_0`/`Bas_1` number from a run under this closure. The honest claim is three shared transferable constants in place of twelve per-composition ones that transfer to nothing, not that it is fit-free the way `EquilibriumCarbon` is.

Two consequences for the code. First, `f_s` now depends on the surface temperature, so it is evaluated **inside** the surface-temperature bisection (`PocketPropellantSolver`, both tiers) rather than once at matrix-build time; `PropellantParams.SkeletonCoverage` is a `SkeletonCoverageCurve`, and the polynomial closure is the special case of a constant curve, which is what keeps unconfigured runs bit-identical. Second, the equilibrium itself is a Gibbs minimisation — far too slow for the inner loop, but a pure function of recipe and thermodynamics with no dependence on the search vector, so it is tabulated ahead of the run by [generate_skeleton_carbon_equilibrium.py](generate_skeleton_carbon_equilibrium.py) into `data/skeleton_carbon_equilibrium.json`. **Regenerate that file whenever `data/propellants*.json` changes**; a table that does not cover the loaded set throws at startup rather than falling back.

The only file `data/` genuinely supplies to the live entry point is the propellants set — plus `skeleton_carbon_equilibrium.json` when the equilibrium closure is selected. `data/propellants*.json` holds propellant definitions; the suffix (`0`, `1`, `234`, `01234`) selects which set is loaded, and the entry point always asks for `01234`. It is deserialised as a `List<Propellant>` (case-insensitive) and the scenario then partitions it by name into `Bas_0`, `Bas_1`, and `Bas_2`/`Bas_3`/`Bas_4` — **a propellants file missing any of those names will throw**, since the scenario uses `First(...)`.

The other files in `data/` (`telegram_settings.json`, `thermodynamic_params.json`, `thermodynamic_substances.json`, `chemical_elements.json`, `substances_file.json`, `propellant_components.json`, `clients.json`, `TAB.dat`) are **not referenced by any C# or Python source in this repository** — they are leftovers from earlier tooling. Don't assume editing them changes a run. In particular there is no Telegram integration in the current tree: the `TelegramBot.Api` project lives under `externals/`, is not in `PastyPropellant.sln`, and nothing in `src/dotnet` references it or reads `telegram_settings.json`.

Per-file detail for everything in `data/` — format, consumer (including the one live test that reads `data/propellants.json`), and whether a run needs it — is in [docs/data-manifest.md](docs/data-manifest.md).

**Outputs** are written to the process working directory: `best_vector.txt` (the converged 32-vector at round-trip precision, with self-verifying `# count` / `# checksum` header lines so `--forward-eval` can replay the exact run), the rendered burn-rate plots (linear and log-log), the Python and skeleton-layer plots, and `propellants.group_optimization.report.en.pdf`.

## Conventions

- `<Nullable>enable</Nullable>` and `<ImplicitUsings>enable</ImplicitUsings>` are on solution-wide.
- Dimensional quantities (`Pressure`, `HeatFlux`, …) come from **UnitsNet**; do not introduce raw `double` for quantities the rest of the codebase models with UnitsNet.
- Test layout mirrors source layout: `src/dotnet/<Area>/tests/<Project>.Tests/` next to `src/dotnet/<Area>/src/<Project>/`. Seven test projects are in `PastyPropellant.sln`, so they are what `dotnet test PastyPropellant.sln` actually runs. `ParametricCombustionModel.PlotRenderer.Tests` exists on disk but is **not** in the solution, so it never runs.
- The legacy `src/` subtrees outside `src/dotnet/` are **not members of the solution** and are not built or run by the standard commands; they retain only stale `bin/obj` artefacts. Treat them as legacy unless you have verified otherwise.
- Don't hardcode the working branch here — it moves. Run `git rev-parse --abbrev-ref HEAD` to see it; the main branch is `master`. Work happens on topic branches (typically `experiment/…`) that are cut from and merged back toward `master`.

## Utility scripts (root-level)

- [checker.py](checker.py) — Ollama-driven analyser that flags `ByDoubles` / `ByUnits` method-pair parity issues in the C# code.
- [projecthandler.py](projecthandler.py) — Per-file summariser that calls a local Ollama model.
- [Generate-All-Resources.ps1](Generate-All-Resources.ps1) — PowerShell resource-generation script.

Reports they produce (`unitsnet_analysis_report_*.json`, `combined_analysis_report_*.json`) are checked in as artefacts of previous runs; treat them as outputs, not inputs.
