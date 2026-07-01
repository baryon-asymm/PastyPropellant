using PastyPropellant.Core.Utils;
using PdfSharp;
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
    public void SetOrientation(PageOrientation orientation);
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

    public void AddImage(string filePath, bool isPortrait = true);

    /// <summary>
    /// Registers a file to be embedded as an attachment in the generated PDF (viewer "attachments"
    /// pane). The attachment is written when <see cref="Generate"/> runs; a missing file at that point
    /// is skipped so generation never fails on it. <paramref name="name"/> is the display name shown to
    /// the reader and defaults to the file name of <paramref name="path"/>.
    /// </summary>
    public void AddEmbeddedFile(string path, string? name = null);

    public OperationResult Generate();
}
