using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using ParametricCombustionModel.PlotRenderer.Models;
using ParametricCombustionModel.PlotRenderer.Renderers;

namespace ParametricCombustionModel.PlotRenderer.Tests;

public class BasePlotRendererErrorSurfacingTest
{
    // BasePlotRenderer no longer declares a Render entry point — that lives on the result-specific
    // interfaces — so this probe only has to expose the protected export path under test.
    private sealed class TestablePlotRenderer : BasePlotRenderer
    {
        public void Save(PlotModel m, string p, PlotSettings s) => SavePlotToFile(m, p, s);
    }

    private sealed class ThrowingEnumerableSource : System.Collections.IEnumerable
    {
        private readonly string _message;
        public ThrowingEnumerableSource(string message) => _message = message;
        public System.Collections.IEnumerator GetEnumerator() =>
            throw new InvalidOperationException(_message);
    }

    private static PlotSettings MakeSettings() => new()
    {
        Title = "T",
        TitleFontSize = 14,
        BackgroundColor = OxyColors.White,
        ShowLegend = false,
        XAxisTitle = "X",
        YAxisTitle = "Y",
        Width = 400,
        Height = 300,
        Quality = 80,
        Dpi = 96
    };

    [Fact]
    public void SavePlotToFile_SurfacesPrimaryRenderException_NotTheSkiaErrorPanelFailure()
    {
        var plotModel = new PlotModel { Title = "Diag" };
        plotModel.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom });
        plotModel.Axes.Add(new LinearAxis { Position = AxisPosition.Left });

        const string sentinel = "SENTINEL-PRIMARY-FAILURE";
        plotModel.Series.Add(new LineSeries { ItemsSource = new ThrowingEnumerableSource(sentinel) });

        var renderer = new TestablePlotRenderer();
        var path = Path.Combine(Path.GetTempPath(), $"diag_{Guid.NewGuid():N}.jpg");

        try
        {
            var ex = Assert.ThrowsAny<Exception>(() => renderer.Save(plotModel, path, MakeSettings()));
            var chain = ex;
            var found = false;
            while (chain != null)
            {
                if (chain.Message.Contains(sentinel, StringComparison.Ordinal))
                {
                    found = true;
                    break;
                }
                chain = chain.InnerException;
            }

            Assert.True(found,
                $"Expected the primary render exception (containing '{sentinel}') to surface, " +
                $"but got: {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
