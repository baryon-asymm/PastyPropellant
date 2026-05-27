using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.SkiaSharp;

namespace ParametricCombustionModel.PlotRenderer.Tests;

public class JpegExporterDiagnosticTest
{
    [Fact]
    public void EmptyPlotModel_ExportsToJpegWithoutAssemblyLoadException()
    {
        var plotModel = new PlotModel
        {
            Title = "Diag",
            DefaultFontSize = 22
        };
        plotModel.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "X" });
        plotModel.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Y" });

        using var stream = new MemoryStream();
        JpegExporter.Export(plotModel, stream, 800, 600, 90, 96);

        Assert.True(stream.Length > 0, "JPEG stream should contain bytes.");
    }

    [Fact]
    public void PlotWithSingleLineSeries_ExportsToJpeg()
    {
        var plotModel = new PlotModel
        {
            Title = "Diag with data",
            DefaultFontSize = 22
        };
        plotModel.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Pressure, MPa" });
        plotModel.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Burn rate, mm/s" });

        var series = new LineSeries { Title = "Calc" };
        series.Points.Add(new DataPoint(1, 10));
        series.Points.Add(new DataPoint(4, 25));
        series.Points.Add(new DataPoint(7, 35));
        plotModel.Series.Add(series);

        using var stream = new MemoryStream();
        JpegExporter.Export(plotModel, stream, 800, 600, 90, 96);

        Assert.True(stream.Length > 0);
    }

    [Fact]
    public void RegexSplitLines_FromCurrentRuntime_Works()
    {
        // Direct check that System.Text.RegularExpressions is resolvable on this runtime.
        var parts = System.Text.RegularExpressions.Regex.Split("a\nb\r\nc", "\r\n|\n|\r");
        Assert.Equal(3, parts.Length);
    }
}
