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

**Standing verification for anything touching the model or the report:** `--forward-eval best_vector.txt` must keep printing `5.9923E+307` / `5.5810E+002` and the three per-group pairs `9.7426E-002`/`4.8878E+002`, `1.7977E+308`/`0`, `2.5025E-002`/`6.9316E+001`. Every commit in this campaign that could have moved a number was checked against it. (That vector is from the `b5007df` reconstruction model, which is *why* Bas_1 does not converge — it is a useful property, not a defect: it exercises the non-convergence paths.)

**A pattern worth carrying forward:** of the items closed so far, **nine** had a materially wrong premise — the "verbatim" duplicate that was not verbatim ([ARCH-8](#arch-8)), the "pure reuse" inheritance that was a template method ([ARCH-3](#arch-3)), the duplication whose fix was deletion ([DUP-4](#dup-4)), the ignore rules that would have been inert ([BUILD-2](#build-2)), the already-resolved item ([DUP-6](#dup-6)), the wrong-comment-not-wrong-code item ([NEW-6](#new-6)), the "dead local" that is read by a sign-check ([NEW-4](#new-4)), the config item prescribing a file that does not exist ([ARCH-7](#arch-7)), and the mis-targeted test whose "obviously worthless" block turned out to hold the solution's only PDF-render coverage ([TEST-4](#test-4)).

**The error is asymmetric, and that is the useful part.** Every one of those nine overstated how safe a deletion was, or pointed the fix at the wrong place. Not one understated a problem. An audit that cannot run the code sees structure but not consequence, so it reads "unreferenced" as "removable" — and [NEW-9](#new-9), where a `ProjectReference` that looked decorative was carrying a type, is the same mistake in miniature. Trace the data flow, then delete. Items closed in this campaign that were *deferred* rather than actioned ([NEW-14](#new-14), [NEW-15](#new-15), [NEW-16](#new-16), [NEW-19](#new-19), [NEW-21](#new-21)) were all deferred for this reason: each needs a trace or a policy decision, not a drive-by edit.

The errors are not random. The audit was read-only — no builds, no runs, no `git` — so it was reliable on *static* structure and systematically wrong wherever the answer required a dynamic fact: who constructs what, what is tracked, where a virtual call dispatches, whether a variable is read two lines later. Read the code, and run it, before acting on any remaining entry here.

| ID | Commit | Note |
|----|--------|------|
| [BUG-1](#bug-1) | `9062392` | Confirmed exactly as described. Pressure tables now emit 17 rows, not 16. |
| [BUG-2](#bug-2) | `9ca6a18` | Confirmed: zero subscribers. stderr is now buffered (50 lines / 8192 chars, tail kept) into the failure exception; the event is still published. |
| [BUILD-2](#build-2), BUILD-4, [DEAD-7](#dead-7) | `2454091` | **Audit was wrong on the key point:** all six files were *tracked*, so ignoring alone would have been inert — git never ignores a tracked file. Files untracked and left on disk. `.env` held only a placeholder, never a live key. |
| [DOC-1](#doc-1), [DOC-2](#doc-2) | `91593f7` | Every fictional element confirmed absent. `data/optimization_tickets.json` does not merely go unused — **it does not exist**. |
| [COR-1](#cor-1) | `aa49396` | Confirmed, and worse than described: **three** concurrent publishers, not one. The cited `Parallel.ForEach` is the least dangerous — it holds a local lock. Fixed by copy-on-write. |
| [COR-5](#cor-5) | `e13be7b` | Confirmed. Optional token + timeout, defaults preserve wait-forever, all call sites unchanged. **This fix introduced a disposal defect — see [NEW-10](#new-10).** |
| [DEAD-3](#dead-3) | `f85dbcd` | All three renderers confirmed unreferenced repo-wide. |
| [DEAD-5](#dead-5) | `2cd2456` | Confirmed *except* the Python line — see NEW-2 below. |
| [DEAD-6](#dead-6) | `de45bd8`, `32dd10b` | Closed in two steps. `GroupReportContextDto.Propellants` was a required ctor parameter supplied by `Program.cs`; removing it made its whole feeding chain dead, which was unwound at the same time rather than left threading a parameter three levels deep to nowhere. |
| [ARCH-5](#arch-5), [ARCH-6](#arch-6), [DUP-5](#dup-5) | `9862751` | One compile-linked `PythonRuntime` replaces the 5-level relative paths, the hardcoded `python3`, and the ×4 invocation boilerplate. Linked rather than referenced so the four Tools libraries stay mutually independent. |
| [COR-2](#cor-2) | `0edfaf6` | Confirmed. All four `Visit` variants aligned. Note [NEW-5](#new-5): `checker.py` structurally cannot catch this class of defect. |
| [COR-3](#cor-3), [COR-4](#cor-4), [DUP-8](#dup-8) | `2980a82` | Hardcoded constants given provenance blocks; the two 29-line fraction copies collapsed with operation order preserved, so results stay IEEE-754 identical. |
| [DOC-3](#doc-3), [DOC-6](#doc-6) | `9629ad6` | `data/` manifest added; per-file consumer recorded. |
| [DEAD-2](#dead-2), [DUP-1](#dup-1) | `368fb04` | **1,526 LOC, not the estimated ~1200.** The duplicate `PerformanceMeterReport` went with the dead stack, so no shared OS-probe helper was needed. |
| [DEAD-1](#dead-1), [TEST-1](#test-1) | `d2b7a2d`, `07c3c65` | **Ten** dead test projects, not nine, and one of them compiled. The wide `ByDoubles`/`ByUnits` parity sweep was ported into a new live `Computation.Tests` project *before* deletion — see [NEW-7](#new-7). |
| [COR-6](#cor-6), [ARCH-9](#arch-9), [ARCH-10](#arch-10), [DUP-9](#dup-9), BUILD-3, [DOC-4](#doc-4), [DOC-5](#doc-5) | `d032e0c` | Python guards, env overrides, `--output-dir`, `requirements.txt`, READMEs. The agglomeration polynomial evaluates pressure in **MPa**, not Pa. |
| [ARCH-3](#arch-3) | `25d1cc0` | **Register was wrong:** the base `GetKineticFlameHeatFlux` is a *template method* dispatching back into the derived `ExtractKineticBurnParams`, so the inheritance carried real behaviour. Simply dropping the stubs would have broken the calculation. Math extracted into `KineticFlameCalculator` instead. |
| [ARCH-4](#arch-4), [DUP-7](#dup-7) | `67a9f4c` | Abstraction split into single-result vs group variants. Verified by byte-comparing both rendered plots — MD5 identical. Also removed a false `PlotRenderer → ReportMaking` project reference. |
| [NEW-8](#new-8) | `3e45139`, `4f8c3e0` | Root cause was [ARCH-5](#arch-5) left unfixed in the *test* projects: `../../../../../` assumed `bin/<config>/<tfm>` and misses by one level from `artifacts/bin/<Project>/<tfm>`. They failed in ~110 ms, before any process started, and the sibling "should fail" test passed for the wrong reason. |
| [DUP-4](#dup-4) | `f621731` | **Register proposed the wrong fix.** `DifferentialEvolutionScenario` is never constructed — no direct use, no reflection, no DI. The duplication is removed by deleting the dead half, not by sharing code with a caller that does not exist. |
| [ARCH-8](#arch-8) | `32dd10b` | **The six copies are not verbatim:** four report sites render `"Bas_2 (includes Bas_3, Bas_4)"`, two console sites `"Bas_2+Bas_3+Bas_4"`. A single constant would have silently rewritten published output, so `CompositionGroups` keeps both display forms over one shared ordering. |
| [ARCH-2](#arch-2) | `2bae0f6` (partial) | Half dissolved rather than fixed: deleting the dead scenario orphaned `DifferentialEvolutionOptimizer` and `IParametricCombustionModelOptimizer`, so the "non-polymorphic interface" complaint no longer has an interface. What remains of ARCH-2 is a question about the surviving group optimiser alone. |
| [DUP-10](#dup-10), [BUILD-1](#build-1) | `50a1c60` | All 23 project files declared the three properties identically, so no project needed an exemption. Changing `Directory.Build.props` invalidates everything: the next build is a full rebuild (~19 min here). |
| [NEW-3](#new-3), [DUP-6](#dup-6), [NEW-6](#new-6) | `c0a0df1` | NEW-3 is worse than recorded — the stale value is a *previous candidate's* burn rate, not a sentinel, because per-worker contexts are reused. On the campaign vector the old report published `Per-fuel RMS = 179.19%` computed entirely from non-existent points. **DUP-6 was already resolved** before this pass; only three identical `CompositionNames` overrides remained. **NEW-6: the comment was wrong, the code was right** — and the same mismatch appeared three more times in the file. |
| [TEST-2](#test-2) | `71b2a61` | 30 tests. Isolation against the process-wide static bus is structural (one event type per test) rather than disciplinary. Found [NEW-10](#new-10). |
| [NEW-10](#new-10) | `1f6cae3` | Disposal regression introduced by the COR-5 fix, found by the TEST-2 tests. |
| [ARCH-1](#arch-1), [NEW-12](#new-12) | `763f8c7` | `Program.cs` 609 → 41 lines over ten types. **Deviated from the suggested split on purpose:** one `ScenarioRunner` would have mixed console narration with numerical configuration, so the runner holds no console output at all and the workflows narrate — that is what makes the extraction buy testability. The two artefact pipelines were deliberately *not* merged, since the optimisation path interleaves progress lines forward-eval does not print; that remains duplication rather than something claimed as clean. |
| [TEST-3](#test-3), [TEST-6](#test-6) | `303eddf` | 48 report tests; `PlotRenderer.Tests` wired into the solution for the first time. Suite: 11 projects / 113 tests. **Verified by mutation** — reintroducing [BUG-1](#bug-1) fails four tests, restoring it returns to green. |
| [NEW-4](#new-4) | — | Closed with no change: the finding was wrong, `rightValue` is read by the bracket sign-check. See below. |
| [TEST-4](#test-4) | (this pass) | **The register's implied fix was half wrong.** The MigraDoc block was indeed mis-targeted, but not worthless and *not* redundant with the 48 `ReportMaking.Tests` — those stop at the `Queue<IPdfOperation>` a report emits and deliberately never render, so **nothing in the solution executed the queue→PDF step at all**. Deleting the block outright would have removed the only coverage of `PTAstraSerifFontResolver` handing PDFsharp bytes from an embedded `.ttf`, where a dropped resource surfaces only at the end of an hours-long run. Kernel salvaged into `PdfGenerationSmokeTest.cs`, retargeted from raw MigraDoc onto our own `PdfSharpAdapter`, asserting the `%PDF` magic number rather than "file is non-empty". `PreparePropellantDataHelper` itself gained fast coverage of its constructor, its one piece of pure logic (a three-way join by name, reached by reflection since the class has no seam — inputs shuffled so an index-zip implementation fails), and its failure path (~76 ms instead of hours). Found [NEW-21](#new-21). ConsoleApp.Tests 40 → 49; suite 12 projects / 190 tests. Verified by mutation: pre-emptively *fixing* the NEW-21 leak fails the characterisation test, and weakening the join's `First(x => x.Name == …)` to `First()` fails two tests. |
| [TEST-7](#test-7) | (this pass) | New `ParametricCombustionModel.Optimization.Tests` — 32 tests, ~800 ms, and the **first test project to reference `Optimization` at all**. Pins the worker-index contract for `UseProcessors(n)`, n ∈ {2,3,4}: indices in `[0,n)`, zero concurrent sharing, and more than one distinct index actually used so the assertion cannot go vacuous if the library ever falls back to sequential. **Found [NEW-17](#new-17)** — the agent's first draft asserted the index-free overload was never called and failed on the first run, which is how the latent initial-population assumption surfaced. Seed control does not exist ([NEW-18](#new-18)); rather than fake a determinism test, the absence is pinned as an inverted tripwire. UnitsNet pins cover the five quantity/unit pairs the reports actually print, confirmed by call-site tally, and surfaced [NEW-19](#new-19). Lock-file assessment surfaced [NEW-20](#new-20). Verified by mutation three ways: forcing `workerIndex = 0` and `workerIndex + 99` both fail as intended (935/1130/1288 violations for n=2/3/4 — a count that scales with real thread interleaving, not a hardcoded flag), and an independent fourth probe collapsing every busy flag onto index 0 fails 3 tests, which is the check that proves the library genuinely runs workers in parallel rather than the assertion passing vacuously. Restored byte-identical (`sha256sum -c` OK) and green. |
| [TEST-5](#test-5) | (this pass) | 125 tests in 0.19 s, `python3 -m pytest src/python`. **The packages are not importable** — each is a flat directory importing siblings by bare name, and RegionMapper and PorosityCalculation *both* define `models`, `molar_masses`, `json_reader`, `json_writer` and `calculators` with different contents, so putting both on `sys.path` lets one silently shadow the other. `conftest.py` imports each in isolation and hands out module objects as fixtures; this works only because every import in these packages is top-level, and a lazy in-function import added later would break it silently. Highest-value target was the agglomeration polynomial, which is duplicated in `region_mappers.py:385` and `PropellantsPlotRendering/src/main.py:88` and evaluates in **MPa** from a Pa argument: a cross-package test now enforces bit-identical output across all five shipped propellants at all ten shipped pressures, which the plot script's docstring claimed but nothing checked. Rendering is deliberately uncovered — asserting on pixels is a change-detector, not a correctness test. Verified by mutation: dropping the `/1e6` fails 11 tests, `CARBON_RETENTION` 0.1→1.0 fails 5, inverting the AP split fails 5; an independent fourth mutation (`CARBON_DENSITY` 2267→2500) fails 5. Test-only deps recorded in a new `requirements-dev.txt`. Found [NEW-15](#new-15) and [NEW-16](#new-16). |
| [NEW-11](#new-11), [ARCH-2](#arch-2) | (this pass) | **NEW-11 confirmed and removed.** Independently verified that the PDF's "Input file" line does *not* come from this property — it flows `RunConfiguration.InputFileName` → workflows → `GroupReportGenerator` → `GroupReportContextDto`, so the three surviving `PropellantsFilePath` hits are on that unrelated type. `ResolvedRunRecordService` serialises `ResolvedRunRecord`, never the scenario settings, so `run_configuration.resolved.json` is untouched. One invisible behaviour change: the builder had two validation checks that were always set together, so a caller who skips `WithPropellantsFromFile` now gets "Propellants must be loaded from file." instead of "Propellants file path must be set." — a programmer-misuse exception unreachable from either entry point. **ARCH-2 closed with no code change.** The item asked for a generic optimiser interface or an extracted shared DE/NM step; both were duplication fixes, and after `2bae0f6` there is no second optimiser to duplicate against, one consumer, and the polymorphism that actually dispatches (`IFitnessFunctionEvaluator`) is already implemented. Adding a type parameter for a single implementation would be speculative generality, and it would put indirection in front of `EvaluateVector` — the one path CLAUDE.md requires optimisation and forward-eval to share. Resolved by deletion. Surfaced [NEW-14](#new-14). |
| [ARCH-7](#arch-7) | (this pass) | **The register prescribed a non-existent mechanism** — it said to source config from `data/optimization_tickets.json` "like the rest of the app", but that file is part of the fiction [DOC-1](#doc-1) removed. A mechanism was designed instead: an optional `run-config.json` (or `--config`), where the built-in defaults *are* the historical literals, so absence is not a fallback branch. The provenance that hardcoding used to give for free (recompile ⇒ git history) is replaced by `run_configuration.resolved.json` plus a PDF block, both recording **effective** values after defaults and after the processor-reduction rule, written before the search starts so a killed run still leaves a record. Bounds are keyed by parameter name so a typo is detectable. Verified: with no config file the eight campaign values reproduce exactly; a partial config setting `penaltyRate` 0.01→0.02 doubles every penalty with fitnesses untouched; unknown members, unknown bound names and missing paths all fail at startup. Also fixed a defect the new tests caught: `BoundsConfiguration`'s record equality compared dictionaries by reference, so no two configurations ever compared equal. |

## 🔴 New findings (not in the original audit)

<a id="new-1"></a>**NEW-1 · High · —** — **Live Telegram bot token committed since `Initial commit`.**
`data/telegram_settings.json` is tracked and its `token` field matches the Telegram bot-token shape exactly (`\d{8,12}:[A-Za-z0-9_-]{35}`), alongside a numeric `chat_id`. It has been in git since `5802f0d`, i.e. in every clone and every history snapshot, on a repository that is public. [BUILD-2](#build-2) pointed at `.env` as *the* secrets hazard and aimed at the wrong file — `.env` holds an inert placeholder. → **Revoke and reissue the bot token via BotFather.** History rewriting is pointless while the token is live. Note nothing in `src/dotnet` actually reads this file (see [DOC-1](#doc-1)), so untracking it costs nothing functionally.

<a id="new-2"></a>**NEW-2 · — · —** — **[DEAD-5](#dead-5) was wrong about the Python line.**
`PorosityCalculation/src/calculators.py:8` is not dead code: `# ALUMINUM_TEMPERATURE_FACTOR = 0.6 # Unused: Previously considered 40% density reduction at 2300 K`. The trailing prose records a *rejected physical assumption* and names the exact metal temperature under active investigation elsewhere in the project. That is modelling provenance. Kept deliberately; do not re-flag.

<a id="new-3"></a>**NEW-3 · Med · S** — Reports read `BurnRate` with no convergence guard.
Every report (`PressureTablesReport`, `BurnRateErrorReport`, `BurnRateVieilleFitReport`, `ProblemContextReport`, `SkeletonLayerPlotsHelper`) reads `MixedCombustionParams.BurnRate` **unconditionally** — none checks `BurnRateIsFound`. A non-converged context reaching a report prints a stale or sentinel burn rate silently. Distinct from [COR-2](#cor-2).

<a id="new-4"></a>**NEW-4 · — · — — NOT A DEFECT, closed with no change.**
The claim was that `rightValue` is a write-once dead local in all four bisection loops. It is not dead: in every one of the four (`BasePropellantSolver.cs:204,357`, `PocketPropellantSolver.cs:586,945`) it is assigned and then **read** in the bracket sign-check on the very next lines (`:208`, `:361`, `:593`, `:952`), which is what rejects an interval that does not bracket a root. Removing it would delete that check.

What the finding actually observed is that `rightValue` is not *updated* inside the loop. That is correct for this formulation: the loop maintains its invariant through `leftValue` alone, so refreshing `rightValue` would be dead work, not a fix. No action available.

<a id="new-5"></a>**NEW-5 · Med · —** — `checker.py` structurally cannot catch solver parity defects.
It pairs files by name (`(.*)(ByDoubles|ByUnits)(.*)\.cs$`), but the solvers carry **both** overloads inside a single file, so every solver is invisible to it. [COR-2](#cor-2) is exactly the class of defect it was built to find, and it cannot see it. Do not treat a clean `checker.py` run as parity evidence for the solvers.

<a id="new-6"></a>**NEW-6 · Low · S** — Comment/resource mismatch in `PressureTablesReport`.
The comment at `PressureTablesReport.cs:146` reads "Add pocket kinetic flame heat flux" while the block below it uses the `SkeletonKineticFlameHeatFlux` resources. Cosmetic, but it is what made [BUG-1](#bug-1) hard to spot by eye.

<a id="new-9"></a>**NEW-9 · Low · S — CLOSED in `67a9f4c`** — `PlotRenderer` carried a `ProjectReference` to `ReportMaking` it did not need.
It compiled only because `OptimizationResult` reached it transitively through that reference. Recorded because it is the general hazard: a reference that looks decorative may still be carrying a type. "Nobody calls it" is not the same as "nothing depends on it" — worth remembering before acting on [DUP-2](#dup-2).

<a id="new-10"></a>**NEW-10 · Med · S — CLOSED in `1f6cae3`** — The [COR-5](#cor-5) fix leaked a cancellation registration.
`process.Start()` runs before the block that creates the linked token source and registers the cancellation callback. A fast-exiting child fires `Exited` on a pool thread → `Complete()` disposes both fields while they still hold their default values → the real ones, assigned immediately afterwards, are never released. Leaks a `CancellationTokenSource` holding a live `CancelAfter` timer plus a registration on the *caller's* token, once per call. Correctness was unaffected (the single-completion gate covers the late kill path); disposal was not. Latent until someone uses the COR-5 parameters, since no current call site passes a token or timeout. **Found by the [TEST-2](#test-2) tests — this is the campaign's own regression, not inherited debt.**

<a id="new-11"></a>**NEW-11 · Low · S** — `DifferentialEvolutionScenarioSettings.PropellantsFilePath` is assigned and never read.
Surfaced while closing [DUP-4](#dup-4), but predates it — the deleted scenario used `.Propellants`, not the path. The `PropellantsFilePath` hits in `ReportMaking/` are on the unrelated `GroupReportContextDto`.

<a id="new-12"></a>**NEW-12 · Med · S** — `SkeletonLayerPlotsHelper` reads `BurnRate` unguarded.
The reporting half of [NEW-3](#new-3) was fixed in `c0a0df1`, but `Helpers/SkeletonLayerPlotsHelper.cs` (ConsoleApp) has the same unguarded read and serialises `skeleton_layer.json` for the Python plots. A non-converged point is therefore still *plotted* as a stale burn rate with no indication.

<a id="new-13"></a>**NEW-13 · — · —** — The suite is only fast under a filter.
Fixing [NEW-8](#new-8) made the Python-backed tests genuinely execute, which exposed that `PythonThermodynamicsCalculatorTest` is a real thermodynamics solve exceeding ten minutes. `dotnet test PastyPropellant.sln` is no longer a quick check; use `--filter "Category!=LongRunning"`. Recorded in CLAUDE.md. This is not a regression — the test was previously "passing" in ~110 ms only because it was broken.

<a id="new-21"></a>**NEW-21 · Med · S** — `PreparePropellantDataHelper.TryPrepareAsync` leaks an `EventBus` subscription on every failure.
[PreparePropellantDataHelper.cs:81-87](../src/dotnet/Apps/src/PastyPropellant.ConsoleApp/Helpers/PreparePropellantDataHelper.cs#L81): it subscribes `RegionMapperProcessInfoLogEventHandler`, early-returns on failure at :85, and unsubscribes at :87 — one line *after* the return, with no `try`/`finally`, so on the failure path the unsubscribe never runs. Identical shape on the porosity branch at :90-97. Verified by reading the code, not inferred.

`EventBus<T>` is static and process-wide, so after N failed `PrepareAsync` calls every `ProcessInfoLogEvent` from *any* component is re-published N extra times as an `InfoLogEvent` attributed to `PythonRegionMapper`. Blast radius today is small — in the console host a failure ends the run, so it is duplicated console lines, not wrong numbers — which is exactly why it could sit here unnoticed.

Found by the [TEST-4](#test-4) tests and **characterised rather than fixed**, since the fix is production code and this pass was scoped to tests. Pinned by `PrepareAsync_WhenRegionMappingFails_LeavesItsProcessLogHandlerSubscribed`, carrying a `⚠ CHARACTERISATION OF A BUG` comment naming what to change. Distinct from [COR-1](#cor-1), which fixed the unsynchronised handler list in this same area but not this leak. → One `try`/`finally` per branch; **the characterisation test must be inverted in the same commit**, or it will fail on the fix.

<a id="new-17"></a>**NEW-17 · Med · —** — The DE library evaluates the initial population through the **index-free** `Evaluate(genes)` overload, and we depend on it being sequential.
Found by the [TEST-7](#test-7) contract tests, and entirely undocumented until now. `DotNetDifferentialEvolution` calls `Evaluate(genes)` — not `Evaluate(workerIndex, genes)` — exactly `PopulationSize` times before any worker thread starts. [GroupDifferentialEvolutionOptimizer.cs:225](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Optimization/Optimizers/GroupDifferentialEvolutionOptimizer.cs#L225) forwards that overload to `workerIndex: 0` unconditionally.

That is safe **today, and only because those calls happen sequentially on one thread.** If a future version parallelises initial-population evaluation, `PopulationSize` threads all pile into worker 0's mutable problem contexts — silent result corruption, no exception, no compile error. This is exactly the failure mode [TEST-7](#test-7) was written to guard, and it turned out to already exist as a latent assumption rather than a hypothetical one. Now pinned by `InitialPopulationEvaluation_UsesTheIndexFreeOverloadSequentiallyOnOneThread` (asserts zero overlaps, exactly one distinct thread). No action needed while the assertion holds; the test is the guard.

<a id="new-18"></a>**NEW-18 · Med · —** — The DE library exposes **no seed control**, so runs are irreproducible.
Confirmed while writing [TEST-7](#test-7): zero occurrences of "Seed" anywhere in `DotNetDifferentialEvolution` or `DotNetOptimization.Abstractions`; `RandomProvider` has only a parameterless constructor; no builder stage accepts a `BaseRandomProvider`. The only deterministic hook is `WithPopulationSampling(IPopulationSamplingMaker)`, which fixes the initial population but not mutation or crossover.

**Consequence for the campaign's own claims:** single-run strategy comparisons (jDE vs SHADE/JADE/L-SHADE on the group run, recorded in the campaign notes) cannot be separated from seed luck. This is the concrete reason the never-implemented fixed-seed item 7.1 matters — it was not merely nice-to-have rigour, it is currently unimplementable without library support. Pinned by `Library_ExposesNoSeedControl`, written as an inverted tripwire: it asserts the *absence*, and its failure message says that failing is good news, because it means an upgrade added seeding and the real fixed-seed harness can finally be built.

<a id="new-19"></a>**NEW-19 · Low · S** — UnitsNet drops to scientific notation above 1e6, mid-table.
`999,000 W/m²` prints in full; `1,000,000` becomes `"1e+06"` and `9,999,000` becomes `"1e+07"` — three significant figures gone. Combustion heat fluxes sit right across that boundary, so report tables mix full and exponent forms cell to cell. Separately, **production never pins culture**: reports inherit the ambient culture, so on a `de-DE` host every cell reads `"6,5 MPa"`. Both are now pinned by [TEST-7](#test-7) under `InvariantCulture` — the pins record the invariant baseline, not what an arbitrary host prints, and that gap is documented in the test. Recorded rather than fixed: changing report number formatting is an output decision, not a cleanup.

<a id="new-20"></a>**NEW-20 · Med · S** — `DotNetOptimization.Abstractions` is transitive-only and version-uncontrolled.
`IFitnessFunctionEvaluator` and `BaseRandomProvider` — the interface `GroupDifferentialEvolutionOptimizer` implements and the type any future seeding work would need — live in a package declared in **no `.csproj` in this repository**. It arrives solely via `DotNetDifferentialEvolution`'s dependency edge, so nothing here controls its version. Verified: `grep DotNetOptimization.Abstractions --include=*.csproj` returns nothing, yet the assembly resolves from the NuGet cache. `ConsoleApp` resolves 25 packages from 9 direct declarations; 16 are transitive and unpinned. → This is the strongest argument for the `packages.lock.json` limb of [TEST-7](#test-7), best paired with `Directory.Packages.props` (which would also settle the `Microsoft.NET.Test.Sdk` split — 17.11.1 in some test projects, 17.12.0 in others).

<a id="new-15"></a>**NEW-15 · Low · S** — `read_components` silently discards components.
`RegionMapper/src/json_reader.py:16` does `next(iter(item.items()))`, so if two components are ever written into one JSON object the second vanishes — no warning, no error. The shipped `data/propellant_components.json` uses one component per object, so it does not bite today; it is a latent trap in the *input format*, which is why it is recorded rather than fixed (changing the reader is a decision about what that format is allowed to express). Pinned as current behaviour by `test_read_components_silently_drops_all_but_the_first_key`.

<a id="new-16"></a>**NEW-16 · Low · S** — Three inconsistencies in the Python helpers, all pinned as-is by [TEST-5](#test-5) rather than corrected.
Recorded together because each is a *policy* question, not a bug with an obvious right answer:
1. `calculate_region_density` returns `0.0` for a zero-volume mixture (`PorosityCalculation/src/calculators.py:23`) — a physically meaningless density that flows into the output JSON — while `calculate_porosity` raises `ValueError` on the identical condition. Same input, two different policies in one module.
2. The two JSON readers default differently on the same file: RegionMapper defaults a missing `mass_fraction` to `0.0` and missing `agglomeration_coefficients` to `[]`; PorosityCalculation raises `KeyError` for the first and yields `None` for the second. Both eventually fail loudly, but at different points — related to [DUP-2](#dup-2), which is where the drifted-schema question belongs.
3. `InterPocketRegionMapper`'s docstring claims a normalisation it does not perform. Harmless for the shipped set (the fractions already sum to 1), misleading for anything else.

<a id="new-14"></a>**NEW-14 · Low · S** — Three `ByDoubles` context matrices are built at startup and never read.
[GroupDifferentialEvolutionScenario.cs:78-80](../src/dotnet/Apps/src/PastyPropellant.ConsoleApp/Scenarios/GroupDifferentialEvolutionScenario.cs#L78) builds `contextMatrixBas0/1/2ByDoubles` into locals that nothing consumes — the per-worker loop at :94-96 builds its own fresh matrices, and those are what feed the optimiser. The parallel `ByUnits` locals at :82-84 **are** read (at :116-118), which is almost certainly why the dead trio went unnoticed: the two blocks read as a symmetric pair.

Surfaced while closing [NEW-11](#new-11), and deliberately **not** acted on in that pass. `ProblemContextByDoublesMatrixBuilder` only reads from the propellant (getters and `PressureFrames` lookups, no assignment back), so the calls do appear to be pure wasted work — but "appears pure" is exactly the confidence level that produced the seven wrong entries noted at the top of this file, and [feedback on the agglomeration coefficient](#new-9) is the standing reminder that a build-time data path can be load-bearing in ways a call-graph read does not show. Cost is three redundant matrix builds once per process, so there is no urgency. → Trace matrix-build-time data flow properly, then delete under its own forward-eval check.

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

<a id="test-7"></a>**TEST-7 · Med · M** — No dependency-contract tests: nothing pins the behaviour we rely on from any third-party library. **Not from the original audit — raised by the user during the cleanup campaign.**

All live tests exercise our own code. No test asserts what an external package does, so an upgrade is currently a blind change.

Scope the urgency correctly first: every `PackageReference` is an exact version and there are no floating (`*`) ranges, so nothing drifts on its own. The risk materialises **only when someone deliberately bumps a version**. These tests are not a monitor — they are what makes that bump safe. Priority is therefore "before the next upgrade", not "now".

Ranked by what an upgrade could break silently:

1. **`DotNetDifferentialEvolution` 4.0.0 / `DotNetNelderMead` 1.0.0 — zero coverage.** No test project references `ParametricCombustionModel.Optimization` at all, and that is the single place our code meets a third-party API (`ForFunction(this)`, `UseProcessors(n)`, `WithStrategy`, `ITerminationStrategy`). The load-bearing contract is **behavioural, not signature-level**: `UseProcessors(n)` must invoke `Evaluate(workerIndex, …)` such that concurrent callers never share a `workerIndex`. The whole `OptimizationProblemByDoubles[processorCount, 3]` scheme rests on it — the contexts are mutated in place, so if a future version reuses or collides indices, **the code still compiles, nothing throws, and results silently corrupt**. A signature change is caught free by the compiler; this one is not caught by anything.
2. **`UnitsNet 6.0.0-pre011` is a prerelease** — the one dependency with no semver obligation, and it is pervasive (~15 factory methods; 28 call sites of `Temperature.FromKelvins` alone). Beyond the API, reports depend on `ToUnit(...).ToString()`, whose output is culture- and rounding-sensitive. Coverage here exists but is incidental: [PressureTablesReportTests.cs:149](../src/dotnet/ParametricCombustionModel/tests/ParametricCombustionModel.ReportMaking.Tests/PressureTablesReportTests.cs#L149) asserts the substring `"7.77"`, which is de facto a UnitsNet formatting test written for another purpose.
3. **No `packages.lock.json`.** Direct versions are exact but transitive resolution is unpinned, so `restore` is not strictly reproducible.

Partial existing cover: the standing `--forward-eval` check would catch a gross numerical change, but it is manual and exercises only the forward path — never the DE search itself.

→ Add (a) a worker-index contract test: a stub `IFitnessFunctionEvaluator` under `UseProcessors(n)`, asserting indices are within `[0,n)` and never concurrently shared; (b) a determinism test on a trivial objective (sphere) with a fixed seed reaching a known optimum — this also finally supplies the fixed-seed harness item 7.1 never got, which currently weakens the jDE/JADE/SHADE attribution; (c) invariant-culture formatting pins for the UnitsNet quantities the report prints. Plus `dotnet restore --use-lock-file` for (3).

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
