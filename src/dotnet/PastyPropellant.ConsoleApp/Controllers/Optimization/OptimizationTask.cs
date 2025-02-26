using ParametricCombustionModel.Core.DTOs;
using PastyPropellant.ConsoleApp.Models;

namespace PastyPropellant.ConsoleApp.Controllers.Optimization;

public record OptimizationTask(
    ParametricModelOptimizationTicket OptimizationTicket,
    OptimizationContext OptimizationContext
);
