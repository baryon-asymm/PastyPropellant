using ParametricCombustionModel.Computation.Models.ComputedParams;
using UnitsNet.Units;

namespace ParametricCombustionModel.ReportMaking.Reports.Pdf;

/// <summary>
/// Single source of the convention every PDF report uses to render a mixed burn rate whose solve did not
/// converge.
///
/// <c>MixedPropellantSolver</c> only assigns <see cref="MixedCombustionParams.BurnRate"/> when both the
/// inter-pocket and the pocket sub-solve found a burn rate; otherwise it leaves the field at whatever the
/// struct last held — <c>Speed.Zero</c> for a freshly built context, or the value from a previous solve, since
/// the per-worker contexts are mutated in place and reused across evaluations. It always assigns
/// <see cref="MixedCombustionParams.BurnRateIsFound"/>, which is therefore the only trustworthy signal that the
/// number next to it means anything.
///
/// Reports must not print that number unguarded: these PDFs are read to judge fit quality, and a stale value
/// is indistinguishable from a converged one on the page. Printing <see cref="NotConvergedMarker"/> instead
/// makes the failure impossible to mistake for a result — the fitness evaluator already treats the same
/// condition as a hard <c>double.MaxValue</c>, so a report that shows a number where the objective sees a
/// total failure is actively misleading.
///
/// The marker is deliberately a language-neutral ASCII literal rather than a localised resource: it is a
/// diagnostic about the solve, not a caption, and it must read identically in every generated locale so a
/// non-converged point can be grepped for in any report.
/// </summary>
internal static class BurnRateConvergence
{
    /// <summary>
    /// Printed in place of a burn-rate number whenever the mixed solve did not converge. Uppercase and
    /// non-numeric so it cannot be skimmed past as a value.
    /// </summary>
    public const string NotConvergedMarker = "NOT CONVERGED";

    /// <summary>
    /// The mixed burn rate in mm/s, formatted exactly as it was before this guard existed when the solve
    /// converged, or <see cref="NotConvergedMarker"/> when it did not. Converged output is unchanged.
    /// </summary>
    public static string FormatMixedBurnRate(
        in MixedCombustionParams mixedCombustionParams)
    {
        return mixedCombustionParams.BurnRateIsFound
            ? mixedCombustionParams.BurnRate.ToUnit(SpeedUnit.MillimeterPerSecond).ToString()
            : NotConvergedMarker;
    }
}
