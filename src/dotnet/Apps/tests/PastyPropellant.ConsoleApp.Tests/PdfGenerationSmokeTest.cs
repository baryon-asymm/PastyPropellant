using PDFsharp.Api.Adapters;
using PDFsharp.Api.Interfaces;
using PDFsharp.Api.Models;
using PdfSharp;
using PdfSharp.Fonts;
using PDFsharp.Api.FontResolvers;

namespace PastyPropellant.ConsoleApp.Tests;

/// <summary>
/// End-to-end smoke coverage for the one thing that only shows up when a PDF is actually rendered:
/// the embedded PT Astra Serif font resolving, and MigraDoc/PDFsharp producing real bytes on disk.
///
/// <para><b>Why this file exists.</b> These tests are the salvaged kernel of a block that used to sit
/// in <c>PreparePropellantDataHelperTest</c>, where it tested nothing that file was named for. As
/// written there it drove raw MigraDoc types (<c>Document</c>, <c>PdfDocumentRenderer</c>) directly and
/// so asserted almost nothing about this repository — it was three near-identical copies of "the
/// third-party renderer can save a file". The residual value was real but narrow: rendering forces
/// <see cref="PTAstraSerifFontResolver"/> to hand PDFsharp the bytes of an embedded <c>.ttf</c>
/// resource, so a renamed or dropped resource surfaces here. These tests keep that coverage and point
/// it at <see cref="PdfSharpAdapter"/> — the type the report pipeline actually uses — instead of at
/// MigraDoc.</para>
///
/// <para>This is deliberately NOT duplicated coverage of <c>ParametricCombustionModel.ReportMaking.Tests</c>.
/// Those 48 tests stop at the <c>Queue&lt;IPdfOperation&gt;</c> a report emits and never render anything;
/// nothing anywhere else in the solution executes the queue-to-PDF step. <c>PDFsharp.Api</c> is in the
/// solution and has no test project of its own, so these live in the nearest project that can reach it.
/// If <c>PDFsharp.Api.Tests</c> is ever created, this file belongs there.</para>
/// </summary>
public class PdfGenerationSmokeTest : IDisposable
{
    private readonly string _directory =
        Path.Combine(Path.GetTempPath(), $"pastypropellant_pdf_{Guid.NewGuid():N}");

    public PdfGenerationSmokeTest() => Directory.CreateDirectory(_directory);

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// The full paragraph/text/footer path, ending in a file that starts with the PDF magic number.
    /// The header check is what makes this more than "a file appeared": a renderer that failed
    /// half-way could still leave a non-empty file behind.
    /// </summary>
    [Fact]
    public void Generate_WritesARealPdfFile()
    {
        var path = Path.Combine(_directory, "report.pdf");
        var adapter = new PdfSharpAdapter(path);

        adapter.AddParagraph(TextAlignment.Center);
        adapter.AddText("Propellant data test report", useBold: true);
        adapter.AddLineBreak();
        // Cyrillic is the reason PT Astra Serif is embedded at all: the report text is Russian.
        // A resolver that silently fell back to a Latin-only face would not fail here, but a
        // resolver that could not load its font data at all would.
        adapter.AddText("Скорость горения");
        adapter.AddFooterForLastPage("page 1");

        var result = adapter.Generate();

        Assert.True(result.IsSuccess, result.Exception?.ToString());
        Assert.True(File.Exists(path), "Generate() must write the file it was constructed with");

        var bytes = File.ReadAllBytes(path);
        Assert.True(bytes.Length > 0, "the rendered PDF must not be empty");
        Assert.Equal("%PDF"u8.ToArray(), bytes.Take(4).ToArray());
    }

    /// <summary>
    /// The table and landscape-orientation path, which the pressure tables in the report use. Kept
    /// separate from the text path because <c>AddTable</c> starts its own section and drives the
    /// merge logic, which is the part most likely to throw on a degenerate row.
    /// </summary>
    [Fact]
    public void Generate_WithATable_WritesARealPdfFile()
    {
        var path = Path.Combine(_directory, "table_report.pdf");
        var adapter = new PdfSharpAdapter(path);

        adapter.SetOrientation(PageOrientation.Landscape);
        adapter.AddParagraph(TextAlignment.Left);
        adapter.AddText("Components");

        var table = new PressureTable(
            columnProportions: [1.0, 1.0, 1.0],
            rows:
            [
                ["Component", "Mass %", "Type"],
                ["AP", "70.0", "Oxidizer"]
            ]);

        adapter.AddTable(table);

        var result = adapter.Generate();

        Assert.True(result.IsSuccess, result.Exception?.ToString());

        var bytes = File.ReadAllBytes(path);
        Assert.True(bytes.Length > 0, "the rendered PDF must not be empty");
        Assert.Equal("%PDF"u8.ToArray(), bytes.Take(4).ToArray());
    }

    /// <summary>
    /// Constructing the adapter is what installs the font resolver process-wide, and
    /// <see cref="PTAstraSerifFontResolver.Apply"/> is documented as being safe to call repeatedly.
    /// Pins both halves: the resolver is installed, and a second adapter neither throws nor swaps it.
    /// </summary>
    [Fact]
    public void Constructing_TheAdapter_InstallsTheFontResolver_Idempotently()
    {
        var first = new PdfSharpAdapter(Path.Combine(_directory, "first.pdf"));
        var installed = GlobalFontSettings.FontResolver;

        Assert.IsType<PTAstraSerifFontResolver>(installed);

        var second = new PdfSharpAdapter(Path.Combine(_directory, "second.pdf"));

        Assert.Same(installed, GlobalFontSettings.FontResolver);

        // Both adapters must still be usable; the resolver is shared, the documents are not.
        first.AddParagraph(TextAlignment.Left);
        first.AddText("first");
        second.AddParagraph(TextAlignment.Left);
        second.AddText("second");

        Assert.True(first.Generate().IsSuccess);
        Assert.True(second.Generate().IsSuccess);
    }

    /// <summary>
    /// Characterisation: <c>Generate</c> never throws. Every failure — here, an output directory that
    /// does not exist — comes back as a failed <c>OperationResult</c>. The report pipeline relies on
    /// this, since it calls Generate at the end of an hours-long run.
    /// </summary>
    [Fact]
    public void Generate_ToAnUnwritablePath_ReturnsAFailedResultRatherThanThrowing()
    {
        var path = Path.Combine(_directory, "no_such_directory", "report.pdf");
        var adapter = new PdfSharpAdapter(path);

        adapter.AddParagraph(TextAlignment.Left);
        adapter.AddText("text");

        var result = adapter.Generate();

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Exception);
        Assert.False(File.Exists(path));
    }
}
