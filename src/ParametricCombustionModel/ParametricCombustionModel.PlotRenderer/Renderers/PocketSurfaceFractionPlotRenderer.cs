using System.Collections;
using OxyPlot;
using OxyPlot.Series;
using ParametricCombustionModel.Optimization.Models;
using ParametricCombustionModel.PlotRenderer.Models;

namespace ParametricCombustionModel.PlotRenderer.Renderers;

public class PocketSurfaceFractionPlotRenderer : BasePlotRenderer
{
    private static readonly OxyColor[] Colors =
    [
        OxyColors.Blue,
        OxyColors.Green,
        OxyColors.Red,
        OxyColors.Violet
    ];
    
    public override void Render(OptimizationResult result, PlotSettings settings)
    {
        var plotModel = CreatePlotModel(settings);

        // Extract and prepare data for each propellant
        var seriesData = PreparePocketSurfaceFractionData(result);

        // Add each series to the plot
        foreach (var (propellantName, dataPoints) in seriesData)
        {
            var colorIndex = seriesData.Keys.ToList().IndexOf(propellantName);
            var color = Colors[colorIndex % Colors.Length];
            
            AddLineSeries(plotModel, dataPoints, propellantName, color);
        }

        // Optionally save or return the plot as needed
        SavePlotToFile(plotModel, "pocket_surface_fraction_plot.jpg", settings);
    }

    private Dictionary<string, IEnumerable<DataPoint>> PreparePocketSurfaceFractionData(OptimizationResult result)
    {
        var seriesData = new Dictionary<string, IEnumerable<DataPoint>>();

        // foreach (var propellantReport in report.PropellantReports)
        // {
        //     var dataPoints = propellantReport.PressureFrameReports
        //                                      .Select(pfr => new DataPoint(pfr.Pressure,
        //                                          pfr.PocketPropellantReport.PocketSurfaceFraction))
        //                                      .ToList();
        //
        //     seriesData[propellantReport.Propellant.Name] = dataPoints;
        // }

        return seriesData;
    }

    protected void AddLineSeries(PlotModel plotModel,
                                 IEnumerable<DataPoint> data,
                                 string seriesTitle,
                                 OxyColor color)
    {
        var series = new LineSeries
        {
            Title = seriesTitle,
            ItemsSource = data,
            Color = color,
            LineStyle = LineStyle.Solid
        };

        plotModel.Series.Add(series);
    }
}
