using System.Text.Json;
using ParametricCombustionModel.Computation.Models.ComputedParams;
using ParametricCombustionModel.Optimization.Models;
using PastyPropellant.Core.Models.Events.Logs;
using PastyPropellant.Core.Utils;
using PastyPropellant.Interop;

namespace PastyPropellant.ConsoleApp.Helpers;

/// <summary>
/// Writes the machine-readable <c>skeleton_layer.json</c> sidecar (per fuel,
/// per pressure SOLVER OUTPUTS — heat-flux decomposition, flame heights,
/// skeleton-layer geometry, exit temperatures) and renders the skeleton-layer
/// plots from it via the Python plotter. Unlike the thermodynamic plots, this
/// data reflects the FITTED solution rather than the input characterization.
/// </summary>
public static class SkeletonLayerPlotsHelper
{
    private const string SidecarFileName = "skeleton_layer.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true
    };

    /// <summary>Serialises the merged optimisation result to the sidecar JSON.</summary>
    public static string WriteSidecar(OptimizationResult result, string? outputDirectory = null)
    {
        var context = result.OptimizedContext;
        var fuelCount = context.PropellantCount;
        var pressureCount = context.PressureCount;

        var fuels = new List<object>(fuelCount);
        for (int i = 0; i < fuelCount; i++)
        {
            var frames = new List<object>(pressureCount);
            for (int j = 0; j < pressureCount; j++)
            {
                var ctx = context.ProblemContextMatrix[i, j];
                var pocket = ctx.PocketCombustionParams;

                frames.Add(new
                {
                    Pressure = F(ctx.Pressure.Pascals),
                    ExperimentalBurnRate = F(context.ExperimentalBurnRates[i, j].MillimetersPerSecond),
                    CalculatedBurnRate = ConvergedBurnRate(ctx.MixedCombustionParams),
                    SurfaceTemperaturePocket = F(pocket.SurfaceTemperature.Kelvins),
                    SurfaceTemperatureInterPocket = F(ctx.InterPocketCombustionParams.SurfaceTemperature.Kelvins),
                    HeatFlux = new
                    {
                        Skeleton = F(pocket.SkeletonHeatFlux.WattsPerSquareMeter),
                        OutSkeleton = F(pocket.OutSkeletonHeatFlux.WattsPerSquareMeter),
                        Diffusion = F(pocket.DiffusionFlameHeatFlux.WattsPerSquareMeter),
                        MetalBurning = F(pocket.MetalBurningHeatFlux.WattsPerSquareMeter),
                        ToSurfaceTotal = F(pocket.ToSurfaceTotalHeatFlux.WattsPerSquareMeter),
                        Sublimation = F(pocket.SublimationHeatFlux.WattsPerSquareMeter)
                    },
                    FlameHeights = new
                    {
                        SkeletonKinetic = F(pocket.SkeletonKineticFlameCombustionParams.KineticFlameHeight.Micrometers),
                        OutSkeletonKinetic = F(pocket.OutSkeletonKineticFlameCombustionParams.KineticFlameHeight.Micrometers),
                        Diffusion = F(pocket.DiffusionFlameHeight.Micrometers)
                    },
                    SkeletonLayer = new
                    {
                        Thickness = F(pocket.SkeletonLayerThickness.Micrometers),
                        PoreDiameter = F(pocket.PoreDiameter.Micrometers)
                    },
                    ThermalConductivity = new
                    {
                        Effective = F(pocket.EffectiveThermalConductivity.WattsPerMeterKelvin),
                        Conductive = F(pocket.ConductiveThermalConductivity.WattsPerMeterKelvin),
                        Radiative = F(pocket.RadiativeThermalConductivity.WattsPerMeterKelvin)
                    }
                });
            }

            fuels.Add(new
            {
                Name = context.ProblemContextMatrix[i, 0].Propellant.Name,
                PressureFrames = frames
            });
        }

        var path = Path.Combine(
            outputDirectory ?? Directory.GetCurrentDirectory(), SidecarFileName);
        File.WriteAllText(path, JsonSerializer.Serialize(fuels, JsonOptions));
        return path;
    }

    /// <summary>Writes the sidecar, then renders the skeleton-layer plots from it.</summary>
    public static async Task<OperationResult> RenderPlotsAsync(
        OptimizationResult result, string scriptPath)
    {
        try
        {
            EventBus<InfoLogEvent>.Publish(new InfoLogEvent(
                "Writing skeleton-layer sidecar and rendering plots...",
                nameof(SkeletonLayerPlotsHelper)));

            var sidecarPath = WriteSidecar(result);
            return await PythonRuntime.RunScriptAsync(scriptPath, sidecarPath);
        }
        catch (Exception ex)
        {
            return new OperationResult(ex);
        }
    }

    // Non-finite values (e.g. an unconverged context) become JSON null, which
    // the Python plotter skips via `any(v is not None)`.
    private static double? F(double value) => double.IsFinite(value) ? value : null;

    /// <summary>
    /// The mixed burn rate, or <see langword="null"/> when the solve did not converge.
    ///
    /// <para><c>MixedPropellantSolver</c> assigns <c>BurnRate</c> only when both the inter-pocket and the
    /// pocket sub-solve succeed, but always assigns <c>BurnRateIsFound</c>. Because the per-worker problem
    /// contexts are mutated in place and reused across evaluations, a failed solve leaves the field holding
    /// the previous candidate's value — a finite, plausible number that <see cref="F"/> cannot detect and
    /// would happily serialise as a result. <c>BurnRateIsFound</c> is the only trustworthy signal.</para>
    ///
    /// <para>The PDF reports render the equivalent case as a "NOT CONVERGED" marker, but a marker string is
    /// not available here: this payload is consumed by matplotlib, which needs a number or nothing. JSON
    /// <c>null</c> is that "nothing", and it is already the sidecar's established absence convention — the
    /// plotter tests <c>any(v is not None)</c> before drawing a series and matplotlib leaves a gap at a
    /// <c>None</c> point, so a non-converged pressure renders as a visible break in the curve instead of a
    /// fabricated point on it. Emitting 0.0 would be the actively wrong choice: it plots as a real
    /// measurement at the bottom of the axis.</para>
    /// </summary>
    private static double? ConvergedBurnRate(in MixedCombustionParams mixedCombustionParams) =>
        mixedCombustionParams.BurnRateIsFound
            ? F(mixedCombustionParams.BurnRate.MillimetersPerSecond)
            : null;
}
