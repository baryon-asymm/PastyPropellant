using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using ParametricCombustionModel.Core.DTOs;
using PastyPropellant.ConsoleApp.Controllers.Interfaces;
using PastyPropellant.ConsoleApp.Models;
using PastyPropellant.Core.IPCHelpers;
using PastyPropellant.Core.Models.Events;
using PastyPropellant.Core.Utils;

namespace PastyPropellant.ConsoleApp.Controllers.Optimization;

public class OptimizationController : ITaskController
{
    private readonly int _processorNumber;
    private readonly OptimizationTask[] _tasks;

    public OptimizationController(int processorNumber, IEnumerable<OptimizationTask> tasks)
    {
        (_processorNumber, _tasks) = (processorNumber, tasks.ToArray());
    }

    public async Task<OperationResult> RunAsync()
    {
        EventBus<LogEvent>.Publish(new LogEvent
        {
            Message = $"OptimizationController start. Loaded {_tasks.Length} tasks.".AsMemory()
        });

        var workers = new byte[_processorNumber];
        var results = new OperationResult[_processorNumber];

        await Parallel.ForAsync(0,
                                workers.Length,
                                async (i, _) =>
                                {
                                    try
                                    {
                                        results[i] = await RunProcessWorkerAsync(i, workers.Length);
                                    }
                                    catch (Exception ex)
                                    {
                                        results[i] = new OperationResult(ex);
                                        EventBus<LogEvent>.Publish(new LogEvent { Message = ex.Message.AsMemory() });
                                    }
                                });

        EventBus<LogEvent>.Publish(new LogEvent
                                       { Message = "OptimizationController all process workers finished.".AsMemory() });

        var exceptions = results.Where(r => r?.IsSuccess == false).Select(r => r!.Exception).ToArray();
        return exceptions.Length == 0
            ? new OperationResult()
            : new OperationResult(new AggregateException(exceptions));
    }

    private Process GetRunningProcessWorker(int workerId, string pipeName, string workerPath)
    {
        var processWorker = new Process();
        processWorker.StartInfo.FileName = workerPath;
        processWorker.StartInfo.Arguments = pipeName;
        processWorker.StartInfo.UseShellExecute = false;
        processWorker.StartInfo.CreateNoWindow = true;

        processWorker.Start();

        EventBus<LogEvent>.Publish(new LogEvent
        {
            Message = $"OptimizationController process worker {workerId} started.".AsMemory()
        });

        return processWorker;
    }

    private async Task<OperationResult> RunProcessWorkerAsync(int workerId, int workerNumber)
    {
        var results = new List<OperationResult<OptimizationResult>>();
        for (var i = workerId; i < _tasks.Length; i += workerNumber)
        {
            var task = _tasks[i];

            var pipeName = Guid.NewGuid().ToString();
            using var pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.InOut);
            var worker = GetRunningProcessWorker(i, pipeName, task.OptimizationTicket.WorkerPath);
            await pipeServer.WaitForConnectionAsync();
            var ipcHelper = new StreamString(pipeServer);

            EventBus<LogEvent>.Publish(new LogEvent
            {
                Message =
                    $"OptimizationController task: \"{task.OptimizationTicket.Name}\" started by process worker {workerId}."
                        .AsMemory()
            });

            await ipcHelper.WriteStringAsync(task.OptimizationTicket.Name);

            switch (task.OptimizationTicket.StopCondition)
            {
                case StopCondition.IterationCountReached:
                    results.Add(await RunIterationStopConditionAsync(task, worker, ipcHelper));
                    break;
                case StopCondition.TargetFunctionValueCrossedBoundary:
                    results.Add(await RunCrossBoundaryStopConditionAsync(task, worker, ipcHelper));
                    break;
            }

            EventBus<LogEvent>.Publish(new LogEvent
            {
                Message =
                    $"OptimizationController task: \"{task.OptimizationTicket.Name}\" executed by process worker {workerId}."
                        .AsMemory()
            });

            await ipcHelper.WriteStringAsync(StreamString.InitWorkerExit);

            EventBus<LogEvent>.Publish(new LogEvent
            {
                Message = $"OptimizationController process worker {workerId} shut down.".AsMemory()
            });
        }

        EventBus<LogEvent>.Publish(new LogEvent
        {
            Message = $"OptimizationController thread pool worker {workerId} released.".AsMemory()
        });

        var exceptions = results.Where(r => r?.IsSuccess == false).Select(r => r!.Exception).ToArray();
        return exceptions.Length == 0
            ? new OperationResult()
            : new OperationResult(new AggregateException(exceptions));
    }

    private async Task<OperationResult<OptimizationResult>> RunIterationStopConditionAsync(
        OptimizationTask task,
        Process worker,
        StreamString ipcHelper)
    {
        if (task.OptimizationTicket.IterationNumber == null)
            new OperationResult(new InvalidOperationException("MaxIterations is not set."));
        if (task.OptimizationTicket.InitialPointAction == null)
            new OperationResult(new InvalidOperationException("InitialPointAction is not set."));

        var iterationNumber = task.OptimizationTicket.IterationNumber.Value;
        var action = task.OptimizationTicket.InitialPointAction.Value;

        var context = task.OptimizationContext;

        OptimizationResult result = null;
        for (var i = 0; i < iterationNumber; i++)
        {
            EventBus<LogEvent>.Publish(new LogEvent
            {
                Message = $"Task: \"{task.OptimizationTicket.Name}\" start at {i + 1} iteration.".AsMemory()
            });

            var contextStr = JsonSerializer.Serialize(context);
            await ipcHelper.WriteStringAsync(contextStr);

            var msg = string.Empty;
            while (msg != StreamString.InitReceiveResult)
            {
                msg = await ipcHelper.ReadStringAsync();
                if (msg != StreamString.InitReceiveResult)
                    EventBus<LogEvent>.Publish(new LogEvent
                    {
                        Message = $"Name: {task.OptimizationTicket.Name}\n{msg}".AsMemory()
                    });
            }

            var resultStr = await ipcHelper.ReadStringAsync();
            result = JsonSerializer.Deserialize<OptimizationResult>(resultStr);

            switch (action)
            {
                case InitialPointIterTransAction.TakeBest:
                    if (result.TargetFunctionValue == double.MaxValue)
                        goto case InitialPointIterTransAction.GenerateRandom;
                    context = context with { InitialPoint = result.FinalPoint };
                    EventBus<LogEvent>.Publish(new LogEvent
                    {
                        Message = $"Task: \"{task.OptimizationTicket.Name}\" took best point.".AsMemory()
                    });
                    break;
                case InitialPointIterTransAction.GenerateRandom:
                    context = context with { InitialPoint = GetRandomInitialPoint(task.OptimizationTicket) };
                    EventBus<LogEvent>.Publish(new LogEvent
                    {
                        Message = $"Task: \"{task.OptimizationTicket.Name}\" generated new point.".AsMemory()
                    });
                    break;
            }
        }

        if (result is null)
            return new OperationResult<OptimizationResult>(new InvalidOperationException("Wtf!?"));

        var strBuild = new StringBuilder();
        var initialPoint = task.OptimizationTicket.InitialPoint;
        for (var i = 0; i < initialPoint.Length; i++)
            strBuild.Append($"{initialPoint[i]} ");
        strBuild.Append("-> ");
        var finalPoint = result.FinalPoint.ToArray();
        for (var i = 0; i < finalPoint.Length; i++)
            strBuild.Append($"{finalPoint[i]} ");
        await File.AppendAllTextAsync("results.txt", $"{strBuild.ToString().TrimEnd()}\n");

        return new OperationResult<OptimizationResult>(result);
    }

    private async Task<OperationResult<OptimizationResult>> RunCrossBoundaryStopConditionAsync(
        OptimizationTask task,
        Process worker,
        StreamString ipcHelper)
    {
        if (task.OptimizationTicket.MaxTargetFunctionValue == null)
            new OperationResult(new InvalidOperationException("MaxTargetFunctionValue is not set."));
        if (task.OptimizationTicket.InitialPointAction == null)
            new OperationResult(new InvalidOperationException("InitialPointAction is not set."));

        var maxTargetFunctionValue = task.OptimizationTicket.MaxTargetFunctionValue.Value;
        var action = task.OptimizationTicket.InitialPointAction.Value;

        var context = task.OptimizationContext;

        OptimizationResult result = null;
        do
        {
            EventBus<LogEvent>.Publish(new LogEvent
            {
                Message = $"Task: \"{task.OptimizationTicket.Name}\" start for max fval {maxTargetFunctionValue}."
                    .AsMemory()
            });

            var contextStr = JsonSerializer.Serialize(context);
            await ipcHelper.WriteStringAsync(contextStr);

            var msg = string.Empty;
            while (msg != StreamString.InitReceiveResult)
            {
                msg = await ipcHelper.ReadStringAsync();
                if (msg != StreamString.InitReceiveResult)
                    EventBus<LogEvent>.Publish(new LogEvent
                    {
                        Message = $"Name: {task.OptimizationTicket.Name}\n{msg}".AsMemory()
                    });
            }

            var resultStr = await ipcHelper.ReadStringAsync();
            result = JsonSerializer.Deserialize<OptimizationResult>(resultStr);

            switch (action)
            {
                case InitialPointIterTransAction.TakeBest:
                    if (result.TargetFunctionValue == double.MaxValue)
                        goto case InitialPointIterTransAction.GenerateRandom;
                    context = context with { InitialPoint = result.FinalPoint };
                    EventBus<LogEvent>.Publish(new LogEvent
                    {
                        Message = $"Task: \"{task.OptimizationTicket.Name}\" took best point.".AsMemory()
                    });
                    break;
                case InitialPointIterTransAction.GenerateRandom:
                    context = context with { InitialPoint = GetRandomInitialPoint(task.OptimizationTicket) };
                    EventBus<LogEvent>.Publish(new LogEvent
                    {
                        Message = $"Task: \"{task.OptimizationTicket.Name}\" generated new point.".AsMemory()
                    });
                    break;
            }

            EventBus<LogEvent>.Publish(new LogEvent
            {
                Message = $"Task: \"{task.OptimizationTicket.Name}\" is over, TFV: {result.TargetFunctionValue}."
                    .AsMemory()
            });
        } while (result.TargetFunctionValue > maxTargetFunctionValue);

        if (result is null)
            return new OperationResult<OptimizationResult>(new InvalidOperationException("Wtf!?"));

        return new OperationResult<OptimizationResult>(result);
    }

    private double[] GetRandomInitialPoint(ParametricModelOptimizationTicket ticket)
    {
        var random = new Random();
        var initialPoint = new double[ticket.LowerBound.Length];
        for (var i = 0; i < initialPoint.Length; i++)
            initialPoint[i] = random.NextDouble() * (ticket.UpperBound[i] - ticket.LowerBound[i]) +
                              ticket.LowerBound[i];

        return initialPoint;
    }
}
