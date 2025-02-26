using OxyPlot;
using OxyPlot.Series;
using ParametricCombustionModel.Optimization.Models;
using ParametricCombustionModel.PlotRenderer.Drawers;
using ParametricCombustionModel.PlotRenderer.Extensions;
using ParametricCombustionModel.PlotRenderer.Models;

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
    
    private static readonly MarkerType[] MarkerTypes =
    [
        MarkerType.Circle,
        MarkerType.Triangle,
        MarkerType.Plus,
        MarkerType.Square
    ];

    public override void Render(
        OptimizationResult result,
        PlotSettings settings)
    {
        var plotModel = CreatePlotModel(settings);

        // Prepare data and colors for each propellant
        var seriesData = PrepareBurningRateData(result);

        // Add series to the plot
        foreach (var (propellantName, data) in seriesData)
        {
            var colorIndex = seriesData.Keys.ToList().IndexOf(propellantName);
            var color = Colors[colorIndex % Colors.Length];
            var markerType = MarkerTypes[colorIndex % MarkerTypes.Length];

            // Add calculated burning rate series (lighter color)
            AddLineSeries(
                plotModel,
                data.CalculatedBurningRates,
                $"{propellantName} (Calculated)",
                color,
                markerType,
                renderInLegend: true);

            // Add experimental burning rate series (darker color)
            AddLineSeries(
                plotModel,
                data.ExperimentalBurningRates,
                $"{propellantName} (Experimental)",
                color,
                markerType,
                renderInLegend: true,
                lineStyle: LineStyle.Dash);

            // Add confidence intervals
            var propellantConfidenceIntervals = result
                                                .OptimizedContext.ProblemContextMatrix[colorIndex, 0].Propellant
                                                .ConfidenceIntervals;
            if (propellantConfidenceIntervals != null)
            {
                foreach (var confidenceInterval in propellantConfidenceIntervals)
                {
                    var confidenceIntervalDrawer = new ConfidenceIntervalDrawer(
                        confidenceInterval.XValue,
                        confidenceInterval.YValue,
                        confidenceInterval.SizeOfConfidenceInterval,
                        0.1);

                    confidenceIntervalDrawer.Draw(plotModel, color);
                }
            }
        }

        // Optionally save or return the plot as needed
        SavePlotToFile(plotModel, "burning_rate_plot.jpg", settings);
    }

    private Dictionary<string, (IEnumerable<DataPoint> CalculatedBurningRates, IEnumerable<DataPoint>
        ExperimentalBurningRates)> PrepareBurningRateData(
        OptimizationResult result)
    {
        var seriesData =
            new Dictionary<string, (IEnumerable<DataPoint> CalculatedBurningRates, IEnumerable<DataPoint>
                ExperimentalBurningRates)>();

        for (int i = 0; i < result.OptimizedContext.PropellantCount; i++)
        {
            // skip if the propellant name is Bas_21 or bas_22
            if (result.OptimizedContext.ProblemContextMatrix[i, 0].Propellant.Name.Equals("Bas_21")
                || result.OptimizedContext.ProblemContextMatrix[i, 0].Propellant.Name.Equals("Bas_22"))
            {
                continue;
            }
            
            var calculatedDataPoints = new List<DataPoint>();
            var experimentalDataPoints = new List<DataPoint>();
            for (int j = 0; j < result.OptimizedContext.PressureCount; j++)
            {
                calculatedDataPoints.Add(new DataPoint(
                                             result.OptimizedContext.ProblemContextMatrix[i, j].Pressure.Megapascals,
                                             result.OptimizedContext.ProblemContextMatrix[i, j].MixedCombustionParams
                                                   .BurnRate.MillimetersPerSecond));
                experimentalDataPoints.Add(new DataPoint(
                                               result.OptimizedContext.ProblemContextMatrix[i, j].Pressure.Megapascals,
                                               result.OptimizedContext.ExperimentalBurnRates[i, j]
                                                     .MillimetersPerSecond));
            }

            seriesData[result.OptimizedContext.ProblemContextMatrix[i, 0].Propellant.Name] =
                (calculatedDataPoints, experimentalDataPoints);
        }

        return seriesData;
    }

    protected void AddLineSeries(
        PlotModel plotModel,
        IEnumerable<DataPoint> data,
        string seriesTitle,
        OxyColor color,
        MarkerType markerType,
        bool renderInLegend = true,
        LineStyle lineStyle = LineStyle.Solid)
    {
        var series = new LineSeries
        {
            Title = seriesTitle,
            ItemsSource = data,
            Color = color,
            LineStyle = lineStyle,
            RenderInLegend = renderInLegend,
            MarkerType = markerType,
            MarkerSize = 4
        };

        plotModel.Series.Add(series);
    }
}
