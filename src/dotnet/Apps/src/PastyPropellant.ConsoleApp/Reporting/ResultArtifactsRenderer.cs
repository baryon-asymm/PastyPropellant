using ParametricCombustionModel.Optimization.Results;
using ParametricCombustionModel.PlotRenderer.Renderers;
using PastyPropellant.ConsoleApp.Helpers;
using PastyPropellant.Core.Utils;
using PastyPropellant.Interop;

namespace PastyPropellant.ConsoleApp.Reporting;

/// <summary>
/// The output artefacts a finished group evaluation produces, one method per artefact.
///
/// <para>Deliberately granular rather than a single "render everything" call: the optimisation and
/// forward-eval paths emit the same artefacts but narrate them differently on the console, and that
/// console text is part of the host's observable output. Keeping the steps separate lets each path
/// interleave its own progress lines while still sharing the rendering itself — the Python script
/// paths in particular are resolved in exactly one place here.</para>
///
/// <para>The output file names are fixed by the renderers and the Python plotters and are treated as
/// a published contract; nothing here parameterises them.</para>
/// </summary>
public static class ResultArtifactsRenderer
{
    private const string PropellantPlotsScript = "PropellantsPlotRendering/src/main.py";
    private const string SkeletonLayerPlotsScript = "PropellantsPlotRendering/src/skeleton_layer_plots.py";

    /// <summary>Renders the linear burn-rate plot for every composition.</summary>
    public static void RenderBurnRatePlot(GroupOptimizationResult result, string runLabel) =>
        new GroupBurningRatePlotRenderer().Render(result, BurnRatePlotSettingsFactory.CreateLinear(runLabel));

    /// <summary>Renders the log-log burn-rate plot for every composition.</summary>
    public static void RenderBurnRateLogLogPlot(GroupOptimizationResult result, string runLabel) =>
        new GroupBurnRateLogLogPlotRenderer().Render(result, BurnRatePlotSettingsFactory.CreateLogLog(runLabel));

    /// <summary>
    /// Renders the thermodynamic plots from the INPUT propellants file (these depend on the
    /// characterisation, not on the fitted vector).
    /// </summary>
    public static Task RenderPythonPlotsAsync(string inputFileName) =>
        new PropellantPlotsRenderingHelper(PythonRuntime.ScriptPath(PropellantPlotsScript))
            .RenderPlotsAsync(inputFileName);

    /// <summary>
    /// Writes the <c>skeleton_layer.json</c> sidecar of FITTED solver outputs and renders the
    /// skeleton-layer plots from it. Failures are returned rather than thrown: these plots are
    /// diagnostic, and losing them must not cost the caller its PDF report.
    /// </summary>
    public static Task<OperationResult> RenderSkeletonLayerPlotsAsync(GroupOptimizationResult result) =>
        SkeletonLayerPlotsHelper.RenderPlotsAsync(
            result.ToOptimizationResult(),
            PythonRuntime.ScriptPath(SkeletonLayerPlotsScript));
}
