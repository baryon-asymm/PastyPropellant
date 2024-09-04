using System.Collections.ObjectModel;
using ParametricCombustionModel.PdfReportMaking.Models;
using ParametricCombustionModel.ReportMaking.Models;
using PDFsharp.Api.Adapters;
using PDFsharp.Api.Models;

namespace ParametricCombustionModel.PdfReportMaking.Builders;

public class PdfReportMakerBuilder
{
    private readonly Report _originalReport;
    private readonly PdfReport _pdfReport;

    private string _fileName = string.Empty;
    private IEnumerable<PdfLine>? _titleLines = null;
    private IEnumerable<PdfLine>? _footerLines = null;

    private PdfReportMakerBuilder(Report report)
    {
        _originalReport = report;
        _pdfReport = new PdfReport(report);
    }

    public static PdfReportMakerBuilder FromReport(Report report)
    {
        return new PdfReportMakerBuilder(report);
    }

    public PdfReportMakerBuilder WithFileName(string fileName)
    {
        _fileName = fileName;
        return this;
    }

    public PdfReportMakerBuilder WithTitle(IEnumerable<PdfLine> titleLines)
    {
        _titleLines = titleLines;
        return this;
    }

    public PdfReportMakerBuilder WithFooter(IEnumerable<PdfLine> footerLines)
    {
        _footerLines = footerLines;
        return this;
    }

    public PdfReportMaker Build()
    {
        if (string.IsNullOrWhiteSpace(_fileName))
            throw new InvalidOperationException("File name is not set");

        var generator = new PDFsharpAdapter(_fileName);
        var contentHolder = new PdfContentHolder(_titleLines ?? [],
            _pdfReport.Transform(),
            GetPressureTables(_originalReport),
            ["burning_rate_plot.jpg", "pocket_surface_fraction_plot.jpg"],
            _footerLines ?? []);

        return new PdfReportMaker(generator, contentHolder);
    }

    private static IEnumerable<PressureTable> GetPressureTables(Report report)
    {
        var propellantCount = report.PropellantReports.Count();
        var pressurePointCount = report.PropellantReports.First().PressureFrameReports.Count();
        var tables = new PressureTable[pressurePointCount];

        var rowCount = 17;
        var columnCount = 1 + 2 * propellantCount;

        var table = new string[rowCount, columnCount];

        for (int i = 0; i < pressurePointCount; i++)
        {
            table[0, 0] = $"{report.PropellantReports.First().PressureFrameReports.ToArray()[i].Pressure:N1} Па";
            for (int j = 1; j < columnCount; j++)
            {
                table[0, j] = "";
            }
            
            table[1, 0] = "Характеристики";
            var names = report.PropellantReports.Select(x => x.Propellant.Name).ToArray();
            for (int j = 0; j < names.Length; j++)
            {
                table[1, 2 * j + 1] = names[j];
            }

            table[2, 0] = "Экспериментальная скорость горения";
            var expRates = report.PropellantReports
                                 .Select(pr =>
                                     (pr.PressureFrameReports.ToArray()[i].ExperimentalBurningRate * 1e3).ToString("F6"))
                                 .ToArray();
            for (int j = 0; j < expRates.Length; j++)
            {
                table[2, 2 * j + 1] = expRates[j];
            }

            // Расчетная скорость горения
            table[3, 0] = "Расчетная скорость горения";
            var calcRates = report.PropellantReports
                                  .Select(pr => (pr.PressureFrameReports.ToArray()[i].BurningRate * 1e3).ToString("F6"))
                                  .ToArray();
            for (int j = 0; j < calcRates.Length; j++)
            {
                table[3, 2 * j + 1] = calcRates[j];
            }

            // Разница между расчетной и экспериментальной скоростью горения
            table[4, 0] = "Разность";
            var rateDiffs = report.PropellantReports
                                  .Select(pr =>
                                      (pr.PressureFrameReports.ToArray()[i].BurningRate * 1e3 -
                                       pr.PressureFrameReports.ToArray()[i].ExperimentalBurningRate * 1e3).ToString("F6"))
                                  .ToArray();
            for (int j = 0; j < rateDiffs.Length; j++)
            {
                table[4, 2 * j + 1] = rateDiffs[j];
            }

            // Характеристики кинетического и диффузионного пламени
            table[5, 0] = "-";
            //table[5, 1] = "II";
            for (int j = 0; j < propellantCount; j++)
            {
                table[5, 2 * j + 1] = "I";
                table[5, 2 * j + 2] = "II";
            }

            // Линейная скорость горения
            table[6, 0] = "Линейная скорость горения";
            for (int j = 0; j < propellantCount; j++)
            {
                var pr = report.PropellantReports.ElementAt(j);
                table[6, 2 * j + 1] = (pr.PressureFrameReports.ToArray()[i]
                                        .InterPocketPropellantReport.BurningRate * 1e3).ToString("F6");
                table[6, 2 * j + 2] = (pr.PressureFrameReports.ToArray()[i]
                                        .PocketPropellantReport.BurningRate * 1e3).ToString("F6");
            }

            // Температура поверхности
            table[7, 0] = "Температура поверхности";
            for (int j = 0; j < propellantCount; j++)
            {
                var pr = report.PropellantReports.ElementAt(j);
                table[7, 2 * j + 1] = pr.PressureFrameReports.ToArray()[i]
                                        .InterPocketPropellantReport.SurfaceTemperature.ToString("F6");
                table[7, 2 * j + 2] = pr.PressureFrameReports.ToArray()[i]
                                        .PocketPropellantReport.SurfaceTemperature.ToString("F6");
            }

            // Массовая скорость разложения
            table[8, 0] = "Массовая скорость разложения";
            for (int j = 0; j < propellantCount; j++)
            {
                var pr = report.PropellantReports.ElementAt(j);
                table[8, 2 * j + 1] = pr.PressureFrameReports.ToArray()[i]
                                        .InterPocketPropellantReport.DecomposingRate.ToString("F6");
                table[8, 2 * j + 2] = pr.PressureFrameReports.ToArray()[i]
                                        .PocketPropellantReport.DecomposingRate.ToString("F6");
            }

            // Плотность теплового потока кинетического пламени (МКМ)
            table[9, 0] = "Плотность теплового потока кинетического пламени (МКМ)";
            var kFlameHeatFlows = report.PropellantReports.Select(pr =>
                                            pr.PressureFrameReports.ToArray()[i]
                                              .InterPocketPropellantReport.KineticFlameHeatFlow.ToString("N6"))
                                        .ToArray();
            for (int j = 0; j < kFlameHeatFlows.Length; j++)
            {
                table[9, 2 * j + 1] = kFlameHeatFlows[j];
            }

            // Высота кинетического пламени (МКМ)
            table[10, 0] = "Высота кинетического пламени (МКМ)";
            var kFlameHeights = report.PropellantReports.Select(pr =>
                                          (pr.PressureFrameReports.ToArray()[i]
                                            .InterPocketPropellantReport.KineticFlameHeight * 1e6).ToString("F6"))
                                      .ToArray();
            for (int j = 0; j < kFlameHeights.Length; j++)
            {
                table[10, 2 * j + 1] = kFlameHeights[j];
            }

            // Плотность теплового потока кинетического пламени (КС)
            table[11, 0] = "Плотность теплового потока кинетического пламени (КС)";
            var ksHeatFlows = report.PropellantReports.Select(pr =>
                                        pr.PressureFrameReports.ToArray()[i]
                                          .PocketPropellantReport.SkeletonKineticFlameHeatFlow.ToString("F6"))
                                    .ToArray();
            for (int j = 0; j < ksHeatFlows.Length; j++)
            {
                table[11, 2 * j + 1] = ksHeatFlows[j];
            }

            // Высота кинетического пламени (КС)
            table[12, 0] = "Высота кинетического пламени (КС)";
            var ksHeights = report.PropellantReports.Select(pr =>
                                      (pr.PressureFrameReports.ToArray()[i]
                                        .PocketPropellantReport.SkeletonKineticFlameHeight * 1e6).ToString("F6"))
                                  .ToArray();
            for (int j = 0; j < ksHeights.Length; j++)
            {
                table[12, 2 * j + 1] = ksHeights[j];
            }

            // Плотность теплового потока кинетического пламени (без КС)
            table[13, 0] = "Плотность теплового потока кинетического пламени (без КС)";
            var noKsHeatFlows = report.PropellantReports.Select(pr =>
                                          pr.PressureFrameReports.ToArray()[i]
                                            .PocketPropellantReport.OutSkeletonKineticFlameHeatFlow.ToString("F6"))
                                      .ToArray();
            for (int j = 0; j < noKsHeatFlows.Length; j++)
            {
                table[13, 2 * j + 1] = noKsHeatFlows[j];
            }

            // Высота кинетического пламени (без КС)
            table[14, 0] = "Высота кинетического пламени (без КС)";
            var noKsHeights = report.PropellantReports.Select(pr =>
                                        (pr.PressureFrameReports.ToArray()[i]
                                          .PocketPropellantReport.OutSkeletonKineticFlameHeight * 1e6).ToString("F6"))
                                    .ToArray();
            for (int j = 0; j < noKsHeights.Length; j++)
            {
                table[14, 2 * j + 1] = noKsHeights[j];
            }

            // Плотность теплового потока диффузионного пламени
            table[15, 0] = "Плотность теплового потока диффузионного пламени";
            var diffusionHeatFlows = report.PropellantReports.Select(pr =>
                                               pr.PressureFrameReports.ToArray()[i]
                                                 .PocketPropellantReport.DiffusionFlameHeatFlow.ToString("F6"))
                                           .ToArray();
            for (int j = 0; j < diffusionHeatFlows.Length; j++)
            {
                table[15, 2 * j + 1] = diffusionHeatFlows[j];
            }

            // Плотность теплового потока от горящего металла
            table[16, 0] = "Плотность теплового потока от горящего металла";
            var metalHeatFlows = report.PropellantReports.Select(pr =>
                                           pr.PressureFrameReports.ToArray()[i]
                                             .PocketPropellantReport.MetalBurningHeatFlow.ToString("F6"))
                                       .ToArray();
            for (int j = 0; j < metalHeatFlows.Length; j++)
            {
                table[16, 2 * j + 1] = metalHeatFlows[j];
            }

            var proportions = new double[columnCount];
            proportions[0] = 1;
            proportions = proportions.Select(x => x == 0 ? 0.5 : 1).ToArray();

            // Преобразование двумерного массива в список строк
            var tableRows = new List<ReadOnlyCollection<string>>();
            for (int row = 0; row < rowCount; row++)
            {
                var rowItems = new List<string>();
                for (int col = 0; col < columnCount; col++)
                {
                    rowItems.Add(table[row, col] ?? string.Empty);
                }
                tableRows.Add(rowItems.AsReadOnly());
            }
            
            // Создаем таблицу PressureTable и добавляем её в массив tables
            tables[i] = new PressureTable(
                proportions,
                tableRows
            );
        }

        return tables;
    }
}
