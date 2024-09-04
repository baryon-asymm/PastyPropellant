using OxyPlot;
using OxyPlot.Series;
using ParametricCombustionModel.PlotRenderer.Extensions;
using ParametricCombustionModel.PlotRenderer.Models;
using ParametricCombustionModel.ReportMaking.Models;

namespace ParametricCombustionModel.PlotRenderer.Renderers;

public class BurningRatePlotRenderer : BasePlotRenderer
{
    private static readonly OxyColor[] Colors =
    [
        OxyColors.Blue,
        OxyColors.Green,
        OxyColors.Red,
        OxyColors.Violet
    ];

    public override void Render(Report report, PlotSettings settings)
    {
        var plotModel = CreatePlotModel(settings);

        // Prepare data and colors for each propellant
        var seriesData = PrepareBurningRateData(report);

        // Add series to the plot
        foreach (var (propellantName, data) in seriesData)
        {
            var colorIndex = seriesData.Keys.ToList().IndexOf(propellantName);
            var color = Colors[colorIndex % Colors.Length];

            // Add calculated burning rate series (lighter color)
            AddLineSeries(
                plotModel,
                data.CalculatedBurningRates,
                $"{propellantName} (Calculated)",
                color,
                LineStyle.Dash);

            // Add experimental burning rate series (darker color)
            AddLineSeries(
                plotModel,
                data.ExperimentalBurningRates,
                $"{propellantName} (Experimental)",
                color);
        }

        // Optionally save or return the plot as needed
        SavePlotToFile(plotModel, "burning_rate_plot.jpg", settings);
    }

    private Dictionary<string, (IEnumerable<DataPoint> CalculatedBurningRates, IEnumerable<DataPoint>
        ExperimentalBurningRates)> PrepareBurningRateData(Report report)
    {
        var seriesData =
            new Dictionary<string, (IEnumerable<DataPoint> CalculatedBurningRates, IEnumerable<DataPoint>
                ExperimentalBurningRates)>();

        foreach (var propellantReport in report.PropellantReports)
        {
            var calculatedDataPoints = propellantReport.PressureFrameReports
                                                       .Select(pfr => new DataPoint(pfr.Pressure, pfr.BurningRate * 1e3))
                                                       .ToList();

            var experimentalDataPoints = propellantReport.PressureFrameReports
                                                         .Select(pfr =>
                                                             new DataPoint(pfr.Pressure, pfr.ExperimentalBurningRate * 1e3))
                                                         .ToList();

            seriesData[propellantReport.Propellant.Name] = (calculatedDataPoints, experimentalDataPoints);
        }

        return seriesData;
    }

    protected void AddLineSeries(PlotModel plotModel,
                                 IEnumerable<DataPoint> data,
                                 string seriesTitle,
                                 OxyColor color,
                                 LineStyle lineStyle = LineStyle.Solid)
    {
        var series = new LineSeries
        {
            Title = seriesTitle,
            ItemsSource = data,
            Color = color,
            LineStyle = lineStyle
        };

        plotModel.Series.Add(series);
    }
}
