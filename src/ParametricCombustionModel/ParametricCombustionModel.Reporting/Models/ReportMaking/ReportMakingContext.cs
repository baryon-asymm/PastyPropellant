using System.Collections.ObjectModel;
using ParametricCombustionModel.Core.Models;

namespace ParametricCombustionModel.Reporting.Models.ReportMaking;

public record ReportMakingContext(
    ReadOnlyCollection<double> Pressures,
    ReadOnlyCollection<double> Point,
    ReadOnlyCollection<Propellant> Propellants
);
