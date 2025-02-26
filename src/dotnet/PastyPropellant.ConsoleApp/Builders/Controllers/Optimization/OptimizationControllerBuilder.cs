using ParametricCombustionModel.Core.DTOs;
using ParametricCombustionModel.Core.Models;
using PastyPropellant.ConsoleApp.Controllers.Optimization;
using PastyPropellant.ConsoleApp.Helpers.ParametricModel;
using PastyPropellant.ConsoleApp.Models;

namespace PastyPropellant.ConsoleApp.Builders.Controllers.Optimization;

public interface IProcessorCountRequirable
{
    public IBuiler WithProcessorCount(int processorCount);
}

public interface IBuiler
{
    public Task<OptimizationController> BuildAsync();
}

public class OptimizationControllerBuilder : IProcessorCountRequirable, IBuiler
{
    private readonly string _filePath;

    private int _processorCount;

    private OptimizationControllerBuilder(string filePath)
    {
        _filePath = filePath;
    }

    public async Task<OptimizationController> BuildAsync()
    {
        var tickets = await GetOptimizationTickets();
        var tasks = await GetOptimizationTasksAsync(tickets);
        return new OptimizationController(_processorCount, tasks);
    }

    public IBuiler WithProcessorCount(int processorCount)
    {
        _processorCount = processorCount;
        return this;
    }

    public static IProcessorCountRequirable FromFilePath(string filePath)
    {
        return new OptimizationControllerBuilder(filePath);
    }

    private async Task<ParametricModelOptimizationTicket[]> GetOptimizationTickets()
    {
        var reader = new OptimizationTicketsHelper(_filePath);

        var operationResult = await reader.GetAllAsync();

        if (operationResult.IsSuccess == false)
            throw operationResult.Exception;

        return operationResult.Value.ToArray();
    }

    private async Task<OptimizationTask[]> GetOptimizationTasksAsync(ParametricModelOptimizationTicket[] tickets)
    {
        var tasks = new OptimizationTask[tickets.Length];

        for (var i = 0; i < tasks.Length; i++)
        {
            var ticket = tickets[i];
            var propellants = await GetPropellantsAsync(ticket);
            var optimizationContext = GetOptimizationContext(ticket, propellants);
            tasks[i] = new OptimizationTask(tickets[i], optimizationContext);
        }

        return tasks;
    }

    private OptimizationContext GetOptimizationContext(ParametricModelOptimizationTicket ticket,
                                                       Propellant[] propellants)
    {
        var surfaceTemperatureRange = GetSurfaceTemperatureRange(ticket);
        var optimizationContext = new OptimizationContext(ticket.Pressures,
                                                          GetInitialPoint(ticket),
                                                          ticket.NumTrialPoints ?? 1000,
                                                          ticket.NumStageOnePoints ?? 200,
                                                          ticket.LowerBound,
                                                          ticket.UpperBound,
                                                          surfaceTemperatureRange.Item1,
                                                          surfaceTemperatureRange.Item2,
                                                          propellants,
                                                          ticket.MaxTime,
                                                          ticket.OffsetOverMiddleHeatFlow ?? 10);
        return optimizationContext;
    }

    private double[] GetInitialPoint(ParametricModelOptimizationTicket ticket)
    {
        if (ticket.UpperBound.Length != ticket.LowerBound.Length)
            throw new InvalidOperationException("Upper and lower bounds must have the same length.");

        if (ticket.InitialPoint is not null)
            return ticket.InitialPoint.ToArray();

        var random = new Random();
        var initialPoint = new double[ticket.LowerBound.Length];
        for (var i = 0; i < initialPoint.Length; i++)
            initialPoint[i] = random.NextDouble() * (ticket.UpperBound[i] - ticket.LowerBound[i]) +
                              ticket.LowerBound[i];

        return initialPoint;
    }

    private (double, double) GetSurfaceTemperatureRange(ParametricModelOptimizationTicket ticket)
    {
        if (ticket.MinSurfaceTemperature is null || ticket.MaxSurfaceTemperature is null)
            return (600, 750);

        if (ticket.MinSurfaceTemperature.Value > ticket.MaxSurfaceTemperature.Value)
            throw new InvalidOperationException("Min surface temperature must be less than max surface temperature.");

        return (ticket.MinSurfaceTemperature.Value, ticket.MaxSurfaceTemperature.Value);
    }

    private async Task<Propellant[]> GetPropellantsAsync(ParametricModelOptimizationTicket ticket)
    {
        var reader = new PropellantsHelper(ticket.PropellantsFilePath);
        var operationResult = await reader.GetAllAsync();

        if (operationResult.IsSuccess == false)
            throw operationResult.Exception;

        return operationResult.Value.ToArray();
    }
}
