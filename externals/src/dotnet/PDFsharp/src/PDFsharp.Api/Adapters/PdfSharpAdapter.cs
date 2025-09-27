using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Shapes;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PastyPropellant.Core.Utils;
using PDFsharp.Api.FontResolvers;
using PDFsharp.Api.Interfaces;
using PDFsharp.Api.Models;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace PDFsharp.Api.Adapters;

public class PdfSharpAdapter : IPdfGeneratorAdapter
{
    private readonly Document _document;

    private readonly string _filePath;

    public PdfSharpAdapter(string filePath)
    {
        if (GlobalFontSettings.FontResolver is not PTAstraSerifFontResolver)
            GlobalFontSettings.FontResolver = new PTAstraSerifFontResolver();

        _document = new Document();
        var style = _document.Styles[StyleNames.Normal]!;

        style.Font.Name = "PTAstraSerif";
        style.Font.Size = 11;

        var section = _document.AddSection();
        section.PageSetup.PageFormat = PageFormat.A4;
        section.PageSetup.Orientation = Orientation.Portrait;
        section.PageSetup.TopMargin = "1cm";
        section.PageSetup.BottomMargin = "2cm";
        section.PageSetup.LeftMargin = "2cm";
        section.PageSetup.RightMargin = "2cm";

        _filePath = filePath;
    }

    public void AddParagraph(TextAlignment textAlignment)
    {
        var paragraph = _document.LastSection.AddParagraph();
        paragraph.Format.Alignment = textAlignment switch
        {
            TextAlignment.Left => ParagraphAlignment.Left,
            TextAlignment.Center => ParagraphAlignment.Center,
            TextAlignment.Right => ParagraphAlignment.Right,
            TextAlignment.Justify => ParagraphAlignment.Justify,
            _ => paragraph.Format.Alignment
        };
    }

    public void AddLineBreak()
    {
        _document.LastSection.LastParagraph!.AddLineBreak();
    }

    public void AddTab()
    {
        _document.LastSection.LastParagraph!.AddTab();
    }

    public void AddText(string text,
                        bool useBold = false,
                        bool useUnderline = false,
                        bool useItalic = false)
    {
        var formattedText = _document.LastSection.LastParagraph!.AddFormattedText(text);
        SetStyle(formattedText, useBold, useUnderline, useItalic);
    }

    public void AddTable(PressureTable table)
    {
        var section = _document.AddSection();
        section.PageSetup.PageFormat = PageFormat.A4;
        section.PageSetup.Orientation = Orientation.Landscape;
        section.PageSetup.TopMargin = "2cm";
        section.PageSetup.BottomMargin = "2cm";
        section.PageSetup.LeftMargin = "1cm";
        section.PageSetup.RightMargin = "2cm";

        var documentTable = _document.LastSection.AddTable();
        documentTable.Borders.Width = 0.75;

        const double totalWidth = 27.0;
        var totalProportion = table.ColumnProportions.Sum();

        foreach (var proportion in table.ColumnProportions)
        {
            var columnWidth = (proportion / totalProportion) * totalWidth;
            documentTable.AddColumn(Unit.FromCentimeter(columnWidth));
        }

        for (int i = 0; i < table.Rows.Count; i++)
        {
            var startMergeIndex = 0;
            var row = documentTable.AddRow();
            for (int j = 0; j < table.Rows[i].Count; j++)
            {
                if (string.IsNullOrWhiteSpace(table.Rows[i][j]) == false)
                {
                    if (startMergeIndex < j - 1)
                    {
                        row.Cells[startMergeIndex].MergeRight = j - 1 - startMergeIndex;
                    }
                    
                    row.Cells[j].AddParagraph(table.Rows[i][j]);
                    startMergeIndex = j;
                }
                else
                {
                    if (j + 1 == table.Rows[i].Count && startMergeIndex < j)
                    {
                        row.Cells[startMergeIndex].MergeRight = j - startMergeIndex;
                    }
                }

                row.Cells[j].Format.Alignment = ParagraphAlignment.Center;
                row.Cells[j].VerticalAlignment = VerticalAlignment.Center;
            }
        }
    }

    public void AddImage(string filePath, bool isPortrait = true)
    {
        var section = _document.AddSection();
        section.PageSetup.PageFormat = PageFormat.A4;
        section.PageSetup.Orientation = isPortrait ? Orientation.Portrait : Orientation.Landscape;
        section.PageSetup.TopMargin = "1cm";
        section.PageSetup.BottomMargin = "2cm";
        section.PageSetup.LeftMargin = "2cm";
        section.PageSetup.RightMargin = "2cm";

        var image = section.AddImage(filePath);
        image.LockAspectRatio = true;
        image.Width = isPortrait ? Unit.FromCentimeter(17.0) : Unit.FromCentimeter(25.0);
    }

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

    public void AddFooterForLastPage(string footerText,
                                     bool useBold = false,
                                     bool useUnderline = false,
                                     bool useItalic = false)
    {
        var emptyPar = _document.LastSection.AddParagraph();
        emptyPar.Format.SpaceBefore = "2cm";

        var textFrame = _document.LastSection.AddTextFrame();
        textFrame.Height = "2cm";
        textFrame.RelativeVertical = RelativeVertical.Page;
        textFrame.Top = "19cm";
        textFrame.Width = "25cm";

        var paragraph = textFrame.AddParagraph();
        paragraph.Format.Alignment = ParagraphAlignment.Right;

        var formattedText = paragraph.AddFormattedText(footerText);
        formattedText.Font.Size = 8;
        SetStyle(formattedText, useBold, useUnderline, useItalic);
    }

    private void SetStyle(FormattedText formattedText,
                          bool useBold,
                          bool useUnderline,
                          bool useItalic)
    {
        formattedText.Font.Bold = useBold;
        formattedText.Font.Underline = useUnderline ? Underline.Single : Underline.None;
        formattedText.Italic = useItalic;
    }

    private void TryGenerate()
    {
        var pdfDocument = new PdfDocument
        {
            ViewerPreferences = { FitWindow = true }
        };

        var pdfRenderer = new PdfDocumentRenderer
        {
            Document = _document,
            PdfDocument = pdfDocument
        };

        pdfRenderer.RenderDocument();
        pdfRenderer.Save(_filePath);
    }
}
