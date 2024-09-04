using ParametricCombustionModel.PdfReportMaking.Enums;
using ParametricCombustionModel.PdfReportMaking.Models;
using PastyPropellant.Core.Utils;
using PDFsharp.Api.Interfaces;

namespace ParametricCombustionModel.PdfReportMaking;

public class PdfReportMaker(
    IPdfGeneratorAdapter generator,
    PdfContentHolder contentHolder
)
{
    public OperationResult Generate()
    {
        try
        {
            TryGenerate();
            return new OperationResult();
        }
        catch (Exception ex)
        {
            return new OperationResult(ex);
        }
    }

    private void TryGenerate()
    {
        AppendTitle();
        AppendBody();
        AppendFooter();

        var result = generator.Generate();
        if (result.IsSuccess == false)
            throw new InvalidOperationException("Failed to generate PDF report", result.Exception);
    }

    private void AppendTitle()
    {
        generator.AddParagraph(TextAlignment.Justify);
        generator.AddTab();
        foreach (var line in contentHolder.Title)
        {
            generator.AddText(line.Text,
                              useBold: line.Style.HasFlag(LineStyle.Bold),
                              useUnderline: line.Style.HasFlag(LineStyle.Underline),
                              useItalic: line.Style.HasFlag(LineStyle.Italic));
            generator.AddLineBreak();
        }

        generator.AddLineBreak();
    }

    private void AppendBody()
    {
        generator.AddParagraph(TextAlignment.Left);
        foreach (var line in contentHolder.Body)
        {
            generator.AddText(line.Text,
                              useBold: line.Style.HasFlag(LineStyle.Bold),
                              useUnderline: line.Style.HasFlag(LineStyle.Underline),
                              useItalic: line.Style.HasFlag(LineStyle.Italic));
            generator.AddLineBreak();
        }
        
        // Tables
        foreach (var table in contentHolder.Tables)
        {
            generator.AddTable(table);
        }

        foreach (var image in contentHolder.Images)
        {
            generator.AddImage(image);
        }
    }

    private void AppendFooter()
    {
        var footerText = string.Join("\n", contentHolder.Footer.Select(line => line.Text));
        generator.AddFooterForLastPage(footerText,
                                       useBold: false,
                                       useUnderline: false,
                                       useItalic: false);
    }
}
