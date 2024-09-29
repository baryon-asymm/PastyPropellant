using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;
using OxyPlot.SkiaSharp;
using ParametricCombustionModel.Optimization.Models;
using ParametricCombustionModel.PlotRenderer.Extensions;
using ParametricCombustionModel.PlotRenderer.Interfaces;
using ParametricCombustionModel.PlotRenderer.Models;

namespace ParametricCombustionModel.PlotRenderer.Renderers;

public abstract class BasePlotRenderer : IPlotRenderer
{
    public abstract void Render(
        OptimizationResult result,
        PlotSettings settings);

    protected PlotModel CreatePlotModel(
        PlotSettings settings)
    {
        var plotModel = new PlotModel
        {
            Title = settings.Title,
            TitleFontSize = settings.TitleFontSize,
            Background = settings.BackgroundColor,
            IsLegendVisible = settings.ShowLegend
        };

        plotModel.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = settings.XAxisTitle,
            Minimum = settings.XAxisMinimum,
            Maximum = settings.XAxisMaximum,
            MajorGridlineStyle = settings.ShowGridlines ? LineStyle.Solid : LineStyle.None,
            MajorGridlineColor = settings.GridlineColor,
            MinorGridlineStyle = LineStyle.Dot,
            MinorGridlineColor = settings.GridlineColor
        });

        plotModel.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = settings.YAxisTitle,
            Minimum = settings.YAxisMinimum,
            Maximum = settings.YAxisMaximum,
            MajorGridlineStyle = settings.ShowGridlines ? LineStyle.Solid : LineStyle.None,
            MajorGridlineColor = settings.GridlineColor,
            MinorGridlineStyle = LineStyle.Dot,
            MinorGridlineColor = settings.GridlineColor
        });

        plotModel.Legends.Add(new Legend
        {
            LegendPosition = LegendPosition.LeftTop,
            LegendBorder = OxyColors.DarkGray,
            LegendBackground = OxyColors.White
        });

        return plotModel;
    }

    protected void AddLineSeries(
        PlotModel plotModel,
        IEnumerable<DataPoint> data,
        string seriesTitle)
    {
        var series = new LineSeries
        {
            Title = seriesTitle,
            ItemsSource = data,
            LineStyle = LineStyle.Solid
        };

        plotModel.Series.Add(series);
    }

    public void SavePlotToFile(
        PlotModel plotModel,
        string filePath,
        PlotSettings settings)
    {
        using var stream = File.Create(filePath);
        JpegExporter.Export(plotModel, stream, settings.Width, settings.Height, settings.Quality, settings.Dpi);
    }

    public byte[] GetPlotAsByteArray(
        PlotModel plotModel,
        PlotSettings settings)
    {
        using var stream = new MemoryStream();
        JpegExporter.Export(plotModel, stream, settings.Width, settings.Height, settings.Quality, settings.Dpi);
        return stream.ToArray();
    }

    public Stream GetPlotAsStream(
        PlotModel plotModel,
        PlotSettings settings)
    {
        var stream = new MemoryStream();
        JpegExporter.Export(plotModel, stream, settings.Width, settings.Height, settings.Quality, settings.Dpi);
        stream.ResetToStart();
        return stream;
    }
}
