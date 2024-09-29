using PDFsharp.Api.Adapters;
using PDFsharp.Api.Interfaces;

namespace PDFsharp.Api.Test;

public class PDFsharpApiTester
{
    [Fact]
    public void TestGeneratePdfFile()
    {
        var pdfGenerator = new PdfSharpAdapter("tmp.pdf");

        // Add simple paragraph with left text alignment
        AddSimpleParagraph(pdfGenerator, TextAlignment.Left);
        pdfGenerator.AddLineBreak();

        // ... right text alignment
        AddSimpleParagraph(pdfGenerator, TextAlignment.Right);
        pdfGenerator.AddLineBreak();

        // ... center text alignment
        AddSimpleParagraph(pdfGenerator, TextAlignment.Center);
        pdfGenerator.AddLineBreak();

        // ... justify text alignment
        AddSimpleParagraph(pdfGenerator, TextAlignment.Justify);

        var operationResult = pdfGenerator.Generate();
        Assert.True(operationResult.IsSuccess);
    }

    private static void AddSimpleParagraph(PdfSharpAdapter adapter, TextAlignment textAlignment)
    {
        adapter.AddParagraph(textAlignment);
        adapter.AddText("Just text\n");
        adapter.AddText("Bold text\n", true);
        adapter.AddText("Underlined text\n", useUnderline: true);
        adapter.AddText("Italic text\n", useItalic: true);
        adapter.AddText("Bold and underlined text\n", true, true);
        adapter.AddText("Bold and italic text\n", true, useItalic: true);
        adapter.AddText("Underlined and italic text\n", useUnderline: true, useItalic: true);
        adapter.AddText("Bold, underlined and italic text\n", true, true, true);
    }
}
