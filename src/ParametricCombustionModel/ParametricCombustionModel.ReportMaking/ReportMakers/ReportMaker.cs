using System.Collections.ObjectModel;
using ParametricCombustionModel.Computation.Utils;
using ParametricCombustionModel.Core.Models;
using ParametricCombustionModel.Optimization.TargetFunctions;
using ParametricCombustionModel.Optimization.TargetFunctions.Interfaces;
using ParametricCombustionModel.ParamsRecording.Builders;
using ParametricCombustionModel.ParamsRecording.Models;
using ParametricCombustionModel.ParamsRecording.ParamsRecorders;
using ParametricCombustionModel.ReportMaking.Models;
using PastyPropellant.Core.Utils;

namespace ParametricCombustionModel.ReportMaking.ReportMakers;

public static class ReportMaker
{
    public static OperationResult<Report> GetReport(ReportMakingContext context)
    {
        try
        {
            return new OperationResult<Report>(TryGetReport(context));
        }
        catch (Exception ex)
        {
            return new OperationResult<Report>(ex);
        }
    }

    private static Report TryGetReport(ReportMakingContext context)
    {
        var propellantReports = GetPropellantReports(context);

        var point = context.Point;
        Span<double> values = stackalloc double[3];
        var targetFunctionSolver = GetTargetFunctionSolver(context);
        targetFunctionSolver.RunTargetFunction(point.ToArray(), values);

        var finalPointReport = new PointReport(point);

        return GetReport(
            finalPointReport,
            values[0],
            propellantReports
        );
    }

    private static ReadOnlyCollection<PropellantReport> GetPropellantReports(ReportMakingContext context)
    {
        var propellants = context.Propellants;
        var pressures = context.ReportablePressures;
        
        Span<double> surfaceTemperatures = stackalloc double[2];
        var burningParams = BurningParams.FromVector(context.Point.ToArray());
        var solverMatrix = GetMixedSolverMatrix(propellants, pressures);
        var experimentalBurningRatesMatrix = propellants.GetExperimentalBurningRates(pressures);

        var propellantReports = new List<PropellantReport>();

        for (var i = 0; i < propellants.Count; i++)
        {
            double pocketVolumeFraction = 0;
            double interPocketVolumeFraction = 0;

            var pressureFrameReports = new List<PressureFrameReport>();
            for (var j = 0; j < pressures.Count; j++)
            {
                solverMatrix[i][j].GetBurningRate(ref burningParams, surfaceTemperatures);
                var mixedParams = solverMatrix[i][j].GetRecord(surfaceTemperatures, ref burningParams);
                pressureFrameReports.Add(
                    GetPressureFrameReport(pressures[j],
                                           experimentalBurningRatesMatrix[i][j],
                                           ref mixedParams)
                );
                pocketVolumeFraction = mixedParams.PocketVolumeFraction;
                interPocketVolumeFraction = mixedParams.InterPocketVolumeFraction;
            }

            propellantReports.Add(
                GetPropellantReport(propellants[i],
                                    pocketVolumeFraction,
                                    interPocketVolumeFraction,
                                    pressureFrameReports)
            );
        }

        return propellantReports.AsReadOnly();
    }

    private static ITargetFunctionSolver GetTargetFunctionSolver(ReportMakingContext context)
    {
        var propellants = context.Propellants;
        var pressures = context.PressuresForTargetFunction;
        var solverMatrix = GetMixedSolverMatrix(propellants, pressures);
        
        var experimentalBurningRates = propellants.GetExperimentalBurningRates(pressures);
        return new TargetFunctionSolver(experimentalBurningRates, solverMatrix);
    }

    private static ReadOnlyCollection<ReadOnlyCollection<MixedSolverParamsRecorder>> GetMixedSolverMatrix(
        IEnumerable<Propellant> propellants,
        IEnumerable<double> pressures)
    {
        return MixedSolverParamsRecordersBuilder.FromPropellants(propellants)
                                                .ForPressures(pressures)
                                                .Build();
    }

    private static Report GetReport(PointReport pointReport,
                                    double targetFunctionValue,
                                    IEnumerable<PropellantReport> propellantReports)
    {
        return new Report(
            pointReport,
            targetFunctionValue,
            propellantReports
        );
    }

    private static PropellantReport GetPropellantReport(Propellant propellant,
                                                        double pocketVolumeFraction,
                                                        double interPocketVolumeFraction,
                                                        IEnumerable<PressureFrameReport> reports)
    {
        return new PropellantReport(
            propellant,
            pocketVolumeFraction,
            interPocketVolumeFraction,
            reports
        );
    }

    private static PressureFrameReport GetPressureFrameReport(double pressure,
                                                              double experimentalBurningRate,
                                                              ref MixedComputationParams parameters)
    {
        return new PressureFrameReport(
            pressure,
            parameters.BurningRate,
            experimentalBurningRate,
            GetPocketPropellantReport(ref parameters),
            GetInterPocketPropellantReport(ref parameters)
        );
    }

    private static PocketPropellantReport GetPocketPropellantReport(ref MixedComputationParams parameters)
    {
        var pocketParameters = parameters.PocketComputationParams;

        var maxHeatFlow = pocketParameters.DiffusionFlameHeatFlow;
        var minHeatFlow = pocketParameters.DiffusionFlameHeatFlow;

        HandleHeatFlow(pocketParameters.SkeletonKineticFlameHeatFlow, ref maxHeatFlow, ref minHeatFlow);
        HandleHeatFlow(pocketParameters.OutSkeletonKineticFlameHeatFlow, ref maxHeatFlow, ref minHeatFlow);
        HandleHeatFlow(pocketParameters.MetalBurningHeatFlow, ref maxHeatFlow, ref minHeatFlow);

        return new PocketPropellantReport(
            pocketParameters.SurfaceTemperature,
            pocketParameters.BurningRate,
            pocketParameters.DecomposingRate,
            pocketParameters.SkeletonKineticFlameHeatFlow,
            pocketParameters.SkeletonKineticFlameHeight,
            pocketParameters.OutSkeletonKineticFlameHeatFlow,
            pocketParameters.OutSkeletonKineticFlameHeight,
            pocketParameters.DiffusionFlameHeatFlow,
            pocketParameters.MetalBurningHeatFlow,
            maxHeatFlow / minHeatFlow,
            parameters.PocketBurningRateFraction,
            pocketParameters.PocketSurfaceFraction
        );
    }

    private static void HandleHeatFlow(double heatFlow, ref double maxHeatFlow, ref double minHeatFlow)
    {
        if (heatFlow > maxHeatFlow)
            maxHeatFlow = heatFlow;
        else if (heatFlow < minHeatFlow)
            minHeatFlow = heatFlow;
    }

    private static InterPocketPropellantReport GetInterPocketPropellantReport(ref MixedComputationParams parameters)
    {
        var interPocketParameters = parameters.InterPocketComputationParams;

        return new InterPocketPropellantReport(
            interPocketParameters.SurfaceTemperature,
            interPocketParameters.BurningRate,
            interPocketParameters.DecomposingRate,
            interPocketParameters.KineticFlameHeatFlow,
            interPocketParameters.KineticFlameHeight,
            parameters.InterPocketBurningRateFraction
        );
    }
}
