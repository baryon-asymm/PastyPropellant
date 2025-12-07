using System.Collections.ObjectModel;
using ParametricCombustionModel.ReportMaking.ReportMakers;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using PDFsharp.Api.Adapters;
using PDFsharp.Api.FontResolvers;
using PastyPropellant.ConsoleApp.Helpers;
using UnitsNet;

namespace PastyPropellant.ConsoleApp.Tests;

public class PreparePropellantDataHelperTest
{
    public const string ArtifactDirectoryPath = "../../../../../artifacts/output_prepare";
    public const string PyMapperScriptPath = "../../../../../src/python/RegionMapper/src/main.py";
    public const string PyThermodynamicsScriptPath = "../../../../../externals/src/python/AerospacePropellantThermodynamics/src/main.py";
    public const string PyPorosityScriptPath = "../../../../../src/python/PorosityCalculation/src/main.py";

    // Very long running test
    [Fact]
    [Trait("Category", "LongRunning")]
    public async Task PrepareAsync_ShouldSuccessReturnOperationResult()
    {
        // Arrange
        var pressures = GetPressures();
        var preparePropellantHelper = new PreparePropellantDataHelper(
            ArtifactDirectoryPath, PyMapperScriptPath, PyThermodynamicsScriptPath, PyPorosityScriptPath, pressures);

        var propellantsFilePath = "../../../../../src/python/RegionMapper/data/propellants.json";
        var componentsFilePath = "../../../../../src/python/RegionMapper/data/components.json";
        var combustionProductsFilePath = "../../../../../externals/src/python/AerospacePropellantThermodynamics/data/combustion_products.json";

        // Act
        var result = await preparePropellantHelper.PrepareAsync(
            propellantsFilePath, componentsFilePath, combustionProductsFilePath);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task PrepareAsync_ShouldExceptionReturnOperationResult()
    {
        // Arrange
        var pressures = GetPressures();
        var preparePropellantHelper = new PreparePropellantDataHelper(
            ArtifactDirectoryPath, PyMapperScriptPath, PyThermodynamicsScriptPath, PyPorosityScriptPath, pressures);

        var propellantsFilePath = "../../../../../src/python/RegionMapper/fake/propellants.json";
        var componentsFilePath = "../../../../../src/python/RegionMapper/fake/components.json";
        var combustionProductsFilePath = "../../../../../externals/src/python/AerospacePropellantThermodynamics/fake/combustion_products.json";

        // Act
        var result = await preparePropellantHelper.PrepareAsync(
            propellantsFilePath, componentsFilePath, combustionProductsFilePath);

        // Assert
        Assert.False(result.IsSuccess);
    }

    private ReadOnlyCollection<Pressure> GetPressures()
    {
        var maxPressure = Pressure.FromMegapascals(6.5);
        var minPressure = Pressure.FromMegapascals(1);
        const int pressurePoints = 2;
        return new ReadOnlyCollection<Pressure>(Enumerable.Range(0, pressurePoints)
            .Select(x => minPressure + (maxPressure - minPressure) / (pressurePoints - 1) * x)
            .ToArray());
    }

    [Fact]
    public void GeneratePdfReport_ShouldCreateAndDeleteFile()
    {
        // Arrange
        PTAstraSerifFontResolver.Apply();
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_report_{Guid.NewGuid()}.pdf");
        var document = new Document();
        var section = document.AddSection();
        var paragraph = section.AddParagraph("Test Report");
        paragraph.Format.Font.Name = "PTAstraSerif";
        var para2 = section.AddParagraph("This is a test PDF document for PreparePropellantDataHelper tests.");
        para2.Format.Font.Name = "PTAstraSerif";

        try
        {
            // Act
            var renderer = new PdfDocumentRenderer
            {
                Document = document
            };
            renderer.RenderDocument();
            renderer.PdfDocument.Save(outputPath);

            // Assert
            Assert.True(File.Exists(outputPath), "PDF file should be created");
            var fileInfo = new FileInfo(outputPath);
            Assert.True(fileInfo.Length > 0, "PDF file should not be empty");
        }
        finally
        {
            // Cleanup
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public void GenerateMultiplePdfReports_ShouldCreateAndDeleteFiles()
    {
        // Arrange
        PTAstraSerifFontResolver.Apply();
        var outputPaths = new List<string>();
        for (int i = 0; i < 3; i++)
        {
            outputPaths.Add(Path.Combine(Path.GetTempPath(), $"test_report_{i}_{Guid.NewGuid()}.pdf"));
        }

        try
        {
            // Act & Assert
            for (int i = 0; i < outputPaths.Count; i++)
            {
                var document = new Document();
                var section = document.AddSection();
                var para1 = section.AddParagraph($"Test Report {i + 1}");
                para1.Format.Font.Name = "PTAstraSerif";
                var para2 = section.AddParagraph("Testing multiple PDF generation.");
                para2.Format.Font.Name = "PTAstraSerif";

                var renderer = new PdfDocumentRenderer
                {
                    Document = document
                };
                renderer.RenderDocument();
                renderer.PdfDocument.Save(outputPaths[i]);

                Assert.True(File.Exists(outputPaths[i]), $"PDF file {outputPaths[i]} should be created");
            }
        }
        finally
        {
            // Cleanup
            foreach (var outputPath in outputPaths)
            {
                if (File.Exists(outputPath))
                {
                    File.Delete(outputPath);
                }
            }
        }
    }

    [Fact]
    public void GeneratePdfReportWithTables_ShouldCreateAndDeleteFile()
    {
        // Arrange
        PTAstraSerifFontResolver.Apply();
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_table_report_{Guid.NewGuid()}.pdf");
        var document = new Document();
        var section = document.AddSection();
        var title = section.AddParagraph("Propellant Data Test Report");
        title.Format.Font.Name = "PTAstraSerif";
        
        var table = section.AddTable();
        table.Format.Font.Name = "PTAstraSerif";
        table.AddColumn(Unit.FromCentimeter(5.0));
        table.AddColumn(Unit.FromCentimeter(5.0));
        table.AddColumn(Unit.FromCentimeter(5.0));

        var headerRow = table.AddRow();
        headerRow.Cells[0].AddParagraph("Component");
        headerRow.Cells[1].AddParagraph("Mass %");
        headerRow.Cells[2].AddParagraph("Type");

        var dataRow = table.AddRow();
        dataRow.Cells[0].AddParagraph("AP");
        dataRow.Cells[1].AddParagraph("70.0");
        dataRow.Cells[2].AddParagraph("Oxidizer");

        try
        {
            // Act
            var renderer = new PdfDocumentRenderer
            {
                Document = document
            };
            renderer.RenderDocument();
            renderer.PdfDocument.Save(outputPath);

            // Assert
            Assert.True(File.Exists(outputPath), "PDF file with tables should be created");
            var fileInfo = new FileInfo(outputPath);
            Assert.True(fileInfo.Length > 0, "PDF file should not be empty");
        }
        finally
        {
            // Cleanup
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }
}
