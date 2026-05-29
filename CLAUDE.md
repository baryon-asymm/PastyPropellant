# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

PastyPropellant is a multi-language toolkit for analysing, optimising, and reporting on **liquid (pasty) propellant performance**. The primary runtime is a .NET console host that drives parametric combustion-model optimisation campaigns and farms CPU-intensive work to external worker executables over named-pipe IPC. Python and C++ components supply auxiliary numerical kernels and plot generation.

A more verbose human-oriented overview lives in [QWEN.md](QWEN.md) and [docs/architecture.md](docs/architecture.md); prefer this file for day-to-day operating instructions and treat QWEN.md as background reading.

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

Run the console host (it loads tickets from `data/optimization_tickets.json`):
```bash
dotnet run --project src/dotnet/Apps/src/PastyPropellant.ConsoleApp
```

Run the computation benchmark:
```bash
dotnet run -c Release --project benchmarks/ParametricCombustionModel/ParametricCombustionModel.Computation.Benchmark
```

Python helpers (e.g. plot rendering):
```bash
python src/python/PropellantsPlotRendering/src/main.py
```

The repository uses a git submodule at [externals/src/python/AerospacePropellantThermodynamics](externals/). Run `git submodule update --init --recursive` after a fresh clone.

## Architecture worth knowing up front

**Solution layout** — the .NET tree is grouped by *role*, not by deployable, so a single feature usually touches several projects:

- [src/dotnet/Apps/src/PastyPropellant.ConsoleApp](src/dotnet/Apps/src/PastyPropellant.ConsoleApp/) — the only entry-point executable. `Program.cs` → `Initializer` → `OptimizationControllerBuilder` → `OptimizationController`.
- [src/dotnet/ParametricCombustionModel/src/](src/dotnet/ParametricCombustionModel/src/) — split into `Core` (DTOs/contracts), `Computation` (numerical kernels + worker `Program.cs`), `Optimization` (DE optimiser, controller, task fan-out), `PlotRenderer`, `ReportMaking`, `Telemetry`.
- [src/dotnet/Common/src/PastyPropellant.Core](src/dotnet/Common/src/PastyPropellant.Core/) — shared primitives, notably `EventBus<TEvent>`.
- [src/dotnet/Common/src/PastyPropellant.ProcessHandling](src/dotnet/Common/src/PastyPropellant.ProcessHandling/) — child-process lifecycle / pipe wiring used by the controller.
- [src/dotnet/Tools/](src/dotnet/Tools/) — independent utility libraries (`RegionMapper`, `Thermodynamics`, `PorosityCalculation`, `PropellantsPlotRendering`) with their own tests; they don't depend on the optimiser.

**Process model.** Optimisation work is *not* run in-process. `OptimizationController` launches one worker process per task from a configurable pool, hands it a named-pipe identifier, serialises the `OptimizationContext` as JSON over the pipe, and consumes streamed log messages plus a final `OptimizationResult`. Workers are normally the `ParametricCombustionModel.Computation` executable but can be any binary honouring the pipe contract (C++ workers are supported). The `worker_path` field in each ticket is what gets executed — when changing the worker contract, both the controller (`Optimization`) and the worker entry point (`Computation/Program.cs`) must move together.

**Event bus.** `EventBus<TEvent>` is a *static, process-wide* pub/sub keyed by event type. Components publish typed events (`LogEvent`, `InfoLogEvent`, …) and the console host plus the optional Telegram relay both subscribe during `Initializer` startup. Because subscriptions are static, tests that exercise the bus must clean up handlers, and adding a new event type is the canonical way to surface new lifecycle information — don't add ad-hoc logger interfaces.

**Group optimisation parameter layout.** When several propellant compositions share an optimisation run, the parameter vector is laid out as a single 32-element array:

| Slot | Meaning |
|------|---------|
| `[0..10]` | 11 shared parameters (optimised once across all compositions) |
| `[11..17]` | 7 parameters specific to the `Bas_2 + Bas_3 + Bas_4` group (`compositionIndex = 0`) |
| `[18..24]` | 7 parameters specific to `Bas_1` (`compositionIndex = 1`) |
| `[25..31]` | 7 parameters specific to `Bas_0` (`compositionIndex = 2`) |

The same ordering is used for `compositionIndex` everywhere downstream — `GroupCombustionSolverParamsByDoubles.ToCompositionVector(idx)`, the per-group context arrays in `GroupOptimizationResult.CompositionContexts`, and the merged matrix produced by `GroupOptimizationResultAdapter`. Anything that splices, bounds-checks, or reports on this vector must follow the same slicing.

`Bas_2`, `Bas_3`, and `Bas_4` are always optimised together as one group sharing the same composition-specific parameters — that is the load-bearing reason for the grouped 32-parameter formulation.

## Runtime data and tickets

`data/` is consumed at runtime, not just at build time. Key files:

| File | Purpose |
|------|---------|
| `data/propellants*.json` | Propellant component definitions; the suffix (`0`, `1`, `234`, `01234`) selects which set is loaded. |
| `data/optimization_tickets.json` | Job list — each ticket is a `ParametricModelOptimizationTicket` (name, propellants_file, worker_path, pressures, lower_bound, upper_bound, optional iteration / target-function / surface-temperature settings). |
| `data/telegram_settings.json` | Credentials for the optional Telegram notification relay. |
| `data/thermodynamic_params.json`, `data/thermodynamic_substances.json`, `data/chemical_elements.json` | Inputs to the thermodynamics tooling. |

Telegram notifications are gated on the `RELEASE` compile symbol — Debug builds will not post even with credentials present.

## Conventions

- `<Nullable>enable</Nullable>` and `<ImplicitUsings>enable</ImplicitUsings>` are on solution-wide.
- Dimensional quantities (`Pressure`, `HeatFlux`, …) come from **UnitsNet**; do not introduce raw `double` for quantities the rest of the codebase models with UnitsNet.
- Test layout mirrors source layout: `src/dotnet/<Area>/tests/<Project>.Tests/` next to `src/dotnet/<Area>/src/<Project>/`. The legacy `tests/` directory at the repo root holds integration tests and the shared `ParametricCombustionModel.Test.Share` project — integration tests there require worker executables to be built first.
- The current working branch is `global_opt_by_groups`; the main branch is `master`.

## Utility scripts (root-level)

- [checker.py](checker.py) — Ollama-driven analyser that flags `ByDoubles` / `ByUnits` method-pair parity issues in the C# code.
- [projecthandler.py](projecthandler.py) — Per-file summariser that calls a local Ollama model.
- [Generate-All-Resources.ps1](Generate-All-Resources.ps1) — PowerShell resource-generation script.

Reports they produce (`unitsnet_analysis_report_*.json`, `combined_analysis_report_*.json`) are checked in as artefacts of previous runs; treat them as outputs, not inputs.
