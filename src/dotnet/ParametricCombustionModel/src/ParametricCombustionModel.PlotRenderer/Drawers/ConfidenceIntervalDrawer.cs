using OxyPlot;
using OxyPlot.Series;

namespace ParametricCombustionModel.PlotRenderer.Drawers;

public class ConfidenceIntervalDrawer
{
    public double XValue { get; init; }
    public double UpperBoundConfidenceInterval { get; init; }
    public double LowerBoundConfidenceInterval { get; init; }
    public double SizeOfWhiskers { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfidenceIntervalDrawer"/> class with a symmetrical confidence interval.
    /// </summary>
    /// <param name="xValue">The X coordinate value of the mean of the confidence interval.</param>
    /// <param name="yValue">The Y coordinate value of the mean of the confidence interval.</param>
    /// <param name="sizeOfConfidenceInterval">The total size of the confidence interval (sum of the upper and lower bounds).</param>
    /// <param name="sizeOfWhiskers">The total size of the whiskers of the confidence interval.</param>
    public ConfidenceIntervalDrawer(
        double xValue,
        double yValue,
        double sizeOfConfidenceInterval,
        double sizeOfWhiskers)
    {
        XValue = xValue;
        UpperBoundConfidenceInterval = yValue + sizeOfConfidenceInterval / 2;
        LowerBoundConfidenceInterval = yValue - sizeOfConfidenceInterval / 2;
        SizeOfWhiskers = sizeOfWhiskers;
    }

    public void Draw(
        PlotModel plotModel,
        OxyColor color)
    {
        DrawMeanLine(plotModel, color);
        DrawWhiskers(plotModel, color);
    }

    private void DrawMeanLine(
        PlotModel plotModel,
        OxyColor color)
    {
        var meanLine = new LineSeries
        {
            Color = color,
            LineStyle = LineStyle.Solid,
            ItemsSource = new[]
            {
                new DataPoint(XValue, UpperBoundConfidenceInterval),
                new DataPoint(XValue, LowerBoundConfidenceInterval)
            },
            RenderInLegend = false
        };
        plotModel.Series.Add(meanLine);
    }

    private void DrawWhiskers(
        PlotModel plotModel,
        OxyColor color)
    {
        var upperWhiskers = new LineSeries
        {
            Color = color,
            LineStyle = LineStyle.Solid,
            ItemsSource = new[]
            {
                new DataPoint(XValue - SizeOfWhiskers / 2, UpperBoundConfidenceInterval),
                new DataPoint(XValue + SizeOfWhiskers / 2, UpperBoundConfidenceInterval)
            },
            RenderInLegend = false
        };
        var lowerWhiskers = new LineSeries
        {
            Color = color,
            LineStyle = LineStyle.Solid,
            ItemsSource = new[]
            {
                new DataPoint(XValue - SizeOfWhiskers / 2, LowerBoundConfidenceInterval),
                new DataPoint(XValue + SizeOfWhiskers / 2, LowerBoundConfidenceInterval)
            },
            RenderInLegend = false
        };

        plotModel.Series.Add(upperWhiskers);
        plotModel.Series.Add(lowerWhiskers);
    }
}
