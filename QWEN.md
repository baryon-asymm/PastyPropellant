# PastyPropellant

## Project Overview

PastyPropellant is a multi-language toolkit for analysing, optimising, and reporting on **liquid propellant performance**. The project combines .NET, Python, and C++ components to provide combustion modelling, parametric optimization, and visualization capabilities.

### Main Technologies

- **.NET 9/10** - Primary development platform (C#)
- **Python 3.10+** - Auxiliary calculators, thermodynamic property computation, and plot generation
- **C++** - Native process workers for CPU-intensive computations

### Architecture

The solution is organized into several functional areas:

| Component | Description |
|-----------|-------------|
| **ConsoleApp** | Primary .NET host that orchestrates optimization campaigns, manages worker processes, and handles notifications |
| **ParametricCombustionModel** | Core optimization and combustion modelling libraries with worker executables |
| **Common (PastyPropellant.Core)** | Shared primitives including `EventBus<T>` pub/sub infrastructure and process handling |
| **Tools** | Utility libraries: RegionMapper, Thermodynamics, PorosityCalculation, PropellantsPlotRendering |
| **Externals** | Third-party integrations: PDFsharp (PDF generation), TelegramBot (notifications) |

### Key Features

- **Differential Evolution Optimization** - Parametric optimization with configurable termination strategies
- **Multi-Process Worker Pool** - IPC via named pipes for isolated computation workers
- **Group Optimization** - Simultaneous optimization of multiple propellant compositions with shared parameters
- **Report Generation** - PDF reports with burning rate plots and optimization results
- **Event-Driven Logging** - Pub/sub event bus for lifecycle events and telemetry

## Repository Structure

```
PastyPropellant/
├── src/
│   ├── dotnet/
│   │   ├── Apps/                    # Console application and ancillary tooling
│   │   ├── Common/                  # Shared core libraries
│   │   ├── ParametricCombustionModel/  # Optimization and combustion models
│   │   └── Tools/                   # Utility libraries
│   └── python/                      # Python helpers for thermodynamics and plotting
├── tests/                           # Unit and integration tests
├── benchmarks/                      # BenchmarkDotNet benchmarks
├── externals/                       # Third-party integrations (PDFsharp, TelegramBot)
├── data/                            # JSON datasets (propellants, thermodynamic tables)
├── docs/                            # Documentation
└── artifacts/                       # Build output (binaries, objects)
```

## Building and Running

### Prerequisites

- **.NET SDK 9.0+** (see `global.json` for version requirements)
- **Python 3.10+** (for auxiliary calculators and plotters)
- **C++ toolchain** (if rebuilding native process workers)

### Build Commands

```bash
# Restore dependencies
dotnet restore PastyPropellant.sln

# Build the solution
dotnet build PastyPropellant.sln

# Run all tests
dotnet test PastyPropellant.sln

# Run the console application
dotnet run --project src/dotnet/Apps/src/PastyPropellant.ConsoleApp
```

### Running Benchmarks

```bash
dotnet run --project benchmarks/ParametricCombustionModel/ParametricCombustionModel.Computation.Benchmark
```

### Python Components

```bash
# Run propellant plot rendering
python src/python/PropellantsPlotRendering/src/main.py
```

## Configuration

### Runtime Configuration Files

| File | Purpose |
|------|---------|
| `data/propellants*.json` | Propellant property definitions |
| `data/optimization_tickets.json` | Optimization job definitions (worker paths, pressure grids, bounds) |
| `data/telegram_settings.json` | Telegram notification credentials |
| `data/thermodynamic_params.json` | Thermodynamic calculation parameters |

### Optimization Ticket Schema

Tickets use the `ParametricModelOptimizationTicket` contract with:
- `name`, `propellants_file`, `worker_path`
- `pressures`, `lower_bound`, `upper_bound`
- Optional: iteration counts, target function thresholds, surface temperature limits

## Development Conventions

### Project Structure

- **Source files**: `src/dotnet/{Component}/src/{ProjectName}/`
- **Test files**: `src/dotnet/{Component}/tests/{ProjectName}.Tests/` or `tests/`
- **Benchmarks**: `benchmarks/{Component}/`
- **Build artifacts**: Centralized in `artifacts/bin/` and `artifacts/obj/`

### Coding Standards

- **Nullable reference types**: Enabled (`<Nullable>enable</Nullable>`)
- **Implicit usings**: Enabled (`<ImplicitUsings>enable</ImplicitUsings>`)
- **Target framework**: .NET 10.0 for most projects
- **UnitsNet library**: Used for dimensional quantities (Pressure, HeatFlux, etc.)

### Testing Practices

- Test projects mirror source project structure
- Integration tests require native workers to be built
- Test sharing via `ParametricCombustionModel.Test.Share` project

### Event Bus Pattern

The static `EventBus<TEvent>` class provides process-wide pub/sub:
```csharp
// Publish events
EventBus<LogEvent>.Publish(new LogEvent { Message = "..." });

// Subscribe to events
EventBus<InfoLogEvent>.Subscribe(e => Console.WriteLine(e.Message));
```

## Key Workflows

### Optimization Execution Flow

1. **Startup** - `Program.cs` publishes lifecycle events, instantiates `Initializer`
2. **Initialization** - Load tickets, configure Telegram hooks, wire event subscriptions
3. **Task Preparation** - Parse tickets, resolve propellants, create `OptimizationTask` objects
4. **Worker Execution** - Fan tasks across configurable process pool via named pipes
5. **Result Handling** - Aggregate results, persist to `results.txt`, propagate through event bus

### Group Optimization

The unified 32-parameter vector structure:
- **[0-10]**: 11 shared parameters (optimized once for all compositions)
- **[11-17]**: 7 specific parameters for Bas_0
- **[18-24]**: 7 specific parameters for Bas_1
- **[25-31]**: 7 specific parameters for Bas_2+Bas_3+Bas_4

## Utility Scripts

| Script | Purpose |
|--------|---------|
| `checker.py` | Ollama-based C# code analyzer for ByDoubles/ByUnits method parity |
| `projecthandler.py` | File summarizer using Ollama LLM |
| `Generate-All-Resources.ps1` | PowerShell script for resource generation |

## Related Reports

- `unitsnet_analysis_report_*.json` - UnitsNet usage analysis
- `combined_analysis_report_*.json` - Combined code analysis reports
