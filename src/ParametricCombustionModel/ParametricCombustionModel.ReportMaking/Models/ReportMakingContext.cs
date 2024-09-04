using System.Collections.ObjectModel;
using ParametricCombustionModel.Core.Models;
using ParametricCombustionModel.Optimization.TargetFunctions.Interfaces;

namespace ParametricCombustionModel.ReportMaking.Models;

public record ReportMakingContext(
    ReadOnlyCollection<Propellant> Propellants,
    ReadOnlyCollection<double> Point,
    ReadOnlyCollection<double> ReportablePressures,
    ReadOnlyCollection<double> PressuresForTargetFunction
);
