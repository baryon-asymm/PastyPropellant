using PastyPropellant.Core.Utils;
using PDFsharp.Api.Models;

namespace PDFsharp.Api.Interfaces;

public enum TextAlignment : byte
{
    Left = 0,
    Center = 1,
    Right = 2,
    Justify = 3
}

public interface IPdfGeneratorAdapter
{
    public void AddParagraph(TextAlignment textAlignment);
    public void AddLineBreak();
    public void AddTab();

    public void AddText(string text,
                        bool useBold = false,
                        bool useUnderline = false,
                        bool useItalic = false);

    public void AddFooterForLastPage(string footerText,
                                     bool useBold = false,
                                     bool useUnderline = false,
                                     bool useItalic = false);

    public void AddTable(PressureTable table);

    public void AddImage(string filePath);

    public OperationResult Generate();
}
