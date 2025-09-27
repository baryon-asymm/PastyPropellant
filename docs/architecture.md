# Architecture

## Top-level components
- **Console host (`src/dotnet/PastyPropellant.ConsoleApp`)** – orchestrates optimisation
  campaigns by publishing lifecycle events, configuring infrastructure, and executing
  optimisation controllers.
- **Optimisation libraries (`src/dotnet/ParametricCombustionModel`)** – provide DTOs,
  optimisers, and worker executables that evaluate parametric combustion models.
- **Common utilities (`src/dotnet/Common`)** – house shared primitives such as the
  `EventBus<T>` pub/sub hub and process-handling helpers.
- **Data assets (`data/`)** – JSON files describing propellant properties, thermodynamic
  tables, and job definitions consumed at runtime.

## Execution flow
1. **Startup** – `Program.cs` publishes start-up log events, instantiates
   `Initializer`, waits for asynchronous initialisation, and finally calls `RunAsync` to
   execute the optimisation suite.
2. **Initialisation** – `Initializer` loads optimisation tickets and Telegram settings,
   constructs an `OptimizationController` through the builder, and registers event bus
   subscriptions so console output and optional webhooks receive updates.
3. **Task preparation** – `OptimizationControllerBuilder` parses ticket definitions,
   resolves propellant datasets, and constructs `OptimizationTask` objects containing the
   worker executable path and a serialisable optimisation context with bounds, surface
   temperature limits, and iteration settings.
4. **Worker execution** – `OptimizationController` allocates tasks across a configurable
   process pool. Each worker process is launched with a named-pipe identifier, receives the
   optimisation context as JSON, streams intermediate log messages back through the pipe,
   and replies with an `OptimizationResult`. Depending on the stop condition, the controller
   either iterates a fixed number of times or repeats until the target function value meets
   a specified threshold.
5. **Result handling** – After each iteration the controller records transitions between
   initial and final parameter vectors and persists them to `results.txt`. Aggregated
   exceptions are surfaced through `OperationResult` instances and propagated back to
   `Program.cs` for final reporting.

## Event bus
The static `EventBus<TEvent>` class maintains a process-wide subscriber list. Components
invoke `Publish` to deliver typed events (such as `LogEvent` or `InfoLogEvent`) to all
registered handlers. Subscriptions are added during initialisation so both console output
and optional Telegram notifications receive the same stream of status updates.

## External integrations
- **Named pipes** – Provide bi-directional IPC between the console host and optimisation
  worker processes. This allows CPU-intensive computations to run in isolated executables
  written in .NET, C++, or Python.
- **Telegram notifications** – When compiled with the `RELEASE` symbol, the initializer
  posts log messages to the configured Telegram relay endpoint.
- **Ollama summariser (`projecthandler.py`)** – Optional tool for producing file summaries
  by querying a locally hosted large language model.

## Data contracts
`ParametricModelOptimizationTicket` defines the JSON contract for optimisation jobs. Key
fields include the propellant data source, worker executable path, pressure array, initial
point, search bounds, surface temperature limits, and stopping conditions. Optional fields
fine-tune iteration counts, acceptance thresholds, and heat-flow offsets.
