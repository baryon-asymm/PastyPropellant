using ParametricCombustionModel.Computation.Models.ComputationsParams;
using ParametricCombustionModel.Computation.Utils;
using ParametricCombustionModel.Core.Models;
using ParametricCombustionModel.Reporting.Builders;
using ParametricCombustionModel.Reporting.Interfaces;
using ParametricCombustionModel.Reporting.Models.RecordParameters;
using ParametricCombustionModel.Reporting.Models.ReportMaking;
using PastyPropellant.Core.Utils;

namespace ParametricCombustionModel.Reporting.ReportMakers;

public class ReportMaker : IReportMaker
{
    public OperationResult<Report> GetReport(ReportMakingContext context)
    {
		try
		{
			return new(TryGetReport(context));
		}
		catch (Exception ex)
		{
			return new(ex);
		}
    }

	private Report TryGetReport(ReportMakingContext context)
	{
        Span<double> surfaceTemperatures = stackalloc double[2];
        var burningParams = BurningParams.FromVector(context.Point.ToArray());
        var mixedPropellantSolvers = MixedSolverParameterRecordersBuilder.FromPropellants(context.Propellants)
                                                                                                           .ForPressures(context.Pressures)
                                                                                                           .Build();
        var experimentalBurningRates = context.Propellants.GetExperimentalBurningRates(context.Pressures);

        var propellantReports = new List<PropellantReport>();

        for (int i = 0; i < context.Propellants.Count; i++)
        {
            double pocketVolumeFraction = 0;
            double interPocketVolumeFraction = 0;

            var pressureFrameReports = new List<PressureFrameReport>();
            for (int j = 0; j < context.Pressures.Count; j++)
            {
                mixedPropellantSolvers[i][j].GetBurningRate(ref burningParams, surfaceTemperatures);
                var mixedParameters = mixedPropellantSolvers[i][j].GetRecord(surfaceTemperatures, ref burningParams);
                pressureFrameReports.Add(GetPressureFrameReport(context.Pressures[j],
                                                                     experimentalBurningRates[i][j],
                                                                     ref mixedParameters));
                pocketVolumeFraction = mixedParameters.PocketVolumeFraction;
                interPocketVolumeFraction = mixedParameters.InterPocketVolumeFraction;
            }
            propellantReports.Add(
                GetPropellantReport(context.Propellants[i],
                                         pocketVolumeFraction,
                                         interPocketVolumeFraction,
                                         pressureFrameReports)
            );
        }

        return GetReport(
            propellantReports
        );
    }

    private Report GetReport(IEnumerable<PropellantReport> propellantReports)
    {
        return new(
            propellantReports
        );
    }

    private PropellantReport GetPropellantReport(Propellant propellant,
                                                 double pocketVolumeFraction,
                                                 double interPocketVolumeFraction,
                                                 IEnumerable<PressureFrameReport> reports)
    {
        return new(
            propellant,
            pocketVolumeFraction,
            interPocketVolumeFraction,
            reports
        );
    }

    private PressureFrameReport GetPressureFrameReport(double pressure,
                                                       double experimentalBurningRate,
                                                       ref MixedComputationParams parameters)
    {
        return new(
            pressure,
            parameters.BurningRate,
            experimentalBurningRate,
            GetPocketPropellantReport(ref parameters),
            GetInterPocketPropellantReport(ref parameters)
        );
    }

    private PocketPropellantReport GetPocketPropellantReport(ref MixedComputationParams parameters)
    {
        var pocketParameters = parameters.PocketComputationParams;

        return new(
            pocketParameters.SurfaceTemperature,
            pocketParameters.BurningRate,
            pocketParameters.DecomposingRate,
            pocketParameters.SkeletonKineticFlameHeatFlow,
            pocketParameters.OutSkeletonKineticFlameHeatFlow,
            pocketParameters.DiffusionFlameHeatFlow,
            pocketParameters.MetalBurningHeatFlow,
            parameters.PocketBurningRateFraction
        );
    }

    private InterPocketPropellantReport GetInterPocketPropellantReport(ref MixedComputationParams parameters)
    {
        var interPocketParameters = parameters.InterPocketComputationParams;

        return new(
            interPocketParameters.SurfaceTemperature,
            interPocketParameters.BurningRate,
            interPocketParameters.DecomposingRate,
            interPocketParameters.KineticFlameHeatFlow,
            parameters.InterPocketBurningRateFraction
        );
    }
}
