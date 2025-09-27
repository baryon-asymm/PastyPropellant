# PastyPropellant Documentation

## Overview
PastyPropellant is a multi-language toolkit for analysing, optimising, and reporting on
liquid propellant performance. The .NET console host orchestrates optimisation campaigns,
invokes external process workers, and relays progress through an in-process event bus.
Supporting utilities (Python, C++, and PowerShell) provide numerical kernels and data
preparation pipelines.

## Repository layout
| Path | Description |
| --- | --- |
| `src/dotnet/PastyPropellant.ConsoleApp` | Primary .NET host that initialises optimisation runs, streams tasks to worker processes, and manages notifications. |
| `src/dotnet/ParametricCombustionModel` | Optimisation and combustion modelling libraries plus worker executables that communicate over named pipes. |
| `src/dotnet/Apps` | Ancillary tooling (e.g., propellant data preparation, plot rendering) that reuse the shared event bus infrastructure. |
| `src/PastyPropellant.Core` | Legacy, minimal core models used by earlier prototypes. |
| `python/` & `src/python/` | Python helpers that calculate thermodynamic properties and generate visualisations. |
| `data/` | JSON datasets describing propellants, thermodynamic tables, and optimisation tickets consumed by the console host. |
| `tests/` | Unit and integration tests for optimisation kernels and inter-process orchestration. |
| `projecthandler.py` | Utility script that summarises repository files by calling an Ollama language model. |

## Getting started
1. **Install prerequisites**
   * .NET SDK 8.0 or newer (see `global.json`).
   * Python 3.10+ for auxiliary calculators and plotters.
   * A C++ toolchain for rebuilding native process workers, if modifications are required.
2. **Restore dependencies**
   ```bash
   dotnet restore PastyPropellant.sln
   ```
3. **Build the solution**
   ```bash
   dotnet build PastyPropellant.sln
   ```
4. **Run the console host**
   ```bash
   dotnet run --project src/dotnet/PastyPropellant.ConsoleApp
   ```

## Configuration files
The console application loads two JSON files at startup:

| File | Purpose |
| --- | --- |
| `data/optimization_tickets.json` | Defines the set of optimisation jobs, including worker executable paths, pressure grids, bounds, and stopping criteria. |
| `data/telegram_settings.json` | Carries credentials for optional Telegram notifications.

Tickets use the schema represented by `ParametricModelOptimizationTicket`, which exposes
properties for optimisation bounds, stopping rules, and thermal constraints. Populate at
least `name`, `propellants_file`, `worker_path`, `pressures`, `lower_bound`, and
`upper_bound`; optional fields include iteration counts, target function thresholds, and
surface temperature limits.

## Runtime flow
1. `Program.cs` publishes lifecycle log events, constructs an `Initializer`, and executes
   initialisation followed by the optimisation run.
2. `Initializer` wires up the optimisation controller, configures Telegram notification
   hooks, and subscribes to `EventBus<LogEvent>` to surface messages on the console.
3. `OptimizationControllerBuilder` reads ticket definitions and propellant datasets, then
   creates `OptimizationTask` instances with serialised optimisation contexts.
4. `OptimizationController` fans tasks out across a configurable worker pool. Each worker
   is a separate process that exchanges JSON payloads with the controller via named pipes.
   Results are aggregated, persisted to `results.txt`, and logged through the event bus.

## Logging and telemetry
The shared `EventBus<T>` static class provides a lightweight pub/sub mechanism for log
records. The console host publishes status updates as `LogEvent` instances, while helper
utilities emit `InfoLogEvent` or process-level events. Subscribe to additional handlers to
forward logs to external sinks (files, dashboards, chat bots) without coupling runtime
components.

## Testing
Run the available test projects to validate optimisation logic and IPC bindings:
```bash
dotnet test PastyPropellant.sln
```
Individual test suites cover the optimisation kernels, process worker protocol, and
integration adapters. Ensure native workers are built before executing IPC-dependent tests.
