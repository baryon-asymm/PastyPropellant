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
            IsLegendVisible = settings.ShowLegend,
            DefaultFontSize = 22
        };

        plotModel.Axes.Add(CreateAxis(
            settings, AxisPosition.Bottom, settings.XAxisTitle, settings.XAxisMinimum, settings.XAxisMaximum));

        plotModel.Axes.Add(CreateAxis(
            settings, AxisPosition.Left, settings.YAxisTitle, settings.YAxisMinimum, settings.YAxisMaximum));

        plotModel.Legends.Add(new Legend
        {
            LegendPosition = LegendPosition.LeftTop,
            LegendBorder = OxyColors.DarkGray,
            LegendBackground = OxyColors.White
        });

        return plotModel;
    }

    // Builds one axis honouring PlotSettings.UseLogarithmicAxes; both LinearAxis and LogarithmicAxis
    // derive from Axis and share the title / range / gridline surface used here.
    private static Axis CreateAxis(
        PlotSettings settings,
        AxisPosition position,
        string title,
        double minimum,
        double maximum)
    {
        Axis axis = settings.UseLogarithmicAxes
            ? new LogarithmicAxis { Base = 10 }
            : new LinearAxis();

        axis.Position = position;
        axis.Title = title;
        axis.Minimum = minimum;
        axis.Maximum = maximum;
        axis.MajorGridlineStyle = settings.ShowGridlines ? LineStyle.Solid : LineStyle.None;
        axis.MajorGridlineColor = settings.GridlineColor;
        axis.MinorGridlineStyle = LineStyle.Dot;
        axis.MinorGridlineColor = settings.GridlineColor;

        return axis;
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
        ExportJpeg(plotModel, stream, settings);
    }

    public byte[] GetPlotAsByteArray(
        PlotModel plotModel,
        PlotSettings settings)
    {
        using var stream = new MemoryStream();
        ExportJpeg(plotModel, stream, settings);
        return stream.ToArray();
    }

    public Stream GetPlotAsStream(
        PlotModel plotModel,
        PlotSettings settings)
    {
        var stream = new MemoryStream();
        ExportJpeg(plotModel, stream, settings);
        stream.ResetToStart();
        return stream;
    }

    // OxyPlot swallows exceptions thrown by series/axes during rendering, draws a textual
    // error panel, and stores the original exception in PlotModel.GetLastPlotException().
    // The panel itself uses Regex via StringHelper.SplitLines, which on .NET 10 has been
    // observed to fail with FileNotFoundException for System.Text.RegularExpressions v8 —
    // masking the real cause. Always surface the primary plot exception when present.
    private static void ExportJpeg(
        PlotModel plotModel,
        Stream stream,
        PlotSettings settings)
    {
        try
        {
            JpegExporter.Export(plotModel, stream, settings.Width, settings.Height, settings.Quality, settings.Dpi);
        }
        catch (Exception exportEx)
        {
            var inner = plotModel.GetLastPlotException();
            if (inner != null && !ReferenceEquals(inner, exportEx))
            {
                throw new InvalidOperationException(
                    $"Plot rendering failed; underlying cause: {inner.GetType().Name}: {inner.Message}",
                    inner);
            }
            throw;
        }

        var renderException = plotModel.GetLastPlotException();
        if (renderException != null)
        {
            throw new InvalidOperationException(
                $"Plot rendering failed; underlying cause: {renderException.GetType().Name}: {renderException.Message}",
                renderException);
        }
    }
}
