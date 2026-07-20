using ParametricCombustionModel.PlotRenderer.Models;

namespace PastyPropellant.ConsoleApp.Reporting;

/// <summary>
/// Builds the burn-rate plot settings shared by the optimisation and forward-eval paths.
///
/// <para>The two paths previously carried four hand-copied <c>PlotSettings</c> initialisers that were
/// identical apart from the run label inside the title — so the axis range, which is the setting a
/// reader actually compares plots on, could drift between an optimisation plot and the replay of that
/// same optimisation. Here it cannot: only the label varies.</para>
/// </summary>
public static class BurnRatePlotSettingsFactory
{
    /// <summary>Run label used by the optimisation path.</summary>
    public const string OptimizationRunLabel = "Group Optimization";

    /// <summary>Run label used by the <c>--forward-eval</c> path.</summary>
    public const string ForwardEvalRunLabel = "Forward Evaluation";

    /// <summary>Linear pressure/burn-rate plot.</summary>
    public static PlotSettings CreateLinear(string runLabel) => new()
    {
        Title = $"Burning Rates - {runLabel} (All Propellants)",
        TitleFontSize = 22,
        XAxisMinimum = 0.9,
        XAxisMaximum = 6.6,
        XAxisTitle = "Pressure, MPa",
        YAxisTitle = "mm/s",
        Width = 800,
        Height = 800,
        Dpi = 96
    };

    /// <summary>Log-log pressure/burn-rate plot, from which the Vieille exponent is read off as a slope.</summary>
    public static PlotSettings CreateLogLog(string runLabel) => new()
    {
        Title = $"Burning Rates (log-log) - {runLabel} (All Propellants)",
        TitleFontSize = 22,
        XAxisMinimum = 0.9,
        XAxisMaximum = 6.6,
        XAxisTitle = "Pressure, MPa (log)",
        YAxisTitle = "mm/s (log)",
        Width = 800,
        Height = 800,
        Dpi = 96
    };
}
