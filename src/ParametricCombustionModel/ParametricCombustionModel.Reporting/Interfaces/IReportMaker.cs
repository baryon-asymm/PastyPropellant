using ParametricCombustionModel.Reporting.Models.ReportMaking;
using PastyPropellant.Core.Utils;

namespace ParametricCombustionModel.Reporting.Interfaces;

public interface IReportMaker
{
    public OperationResult<Report> GetReport(ReportMakingContext context);
}
