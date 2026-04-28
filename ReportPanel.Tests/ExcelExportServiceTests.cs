using ClosedXML.Excel;
using ReportPanel.Services;

namespace ReportPanel.Tests;

/// <summary>
/// M-13 R6.1 regression coverage: ExcelExportService.
/// In-memory MemoryStream + ClosedXML — DB veya filesystem yok.
/// </summary>
public class ExcelExportServiceTests
{
    private readonly ExcelExportService _sut = new();

    [Fact]
    public void BuildReportXlsx_empty_rows_creates_summary_and_empty_results_marker()
    {
        var bytes = _sut.BuildReportXlsx(
            rows: new List<Dictionary<string, object>>(),
            reportTitle: "Boş Rapor",
            username: "tester",
            runAt: new DateTime(2026, 4, 28, 10, 30, 0),
            paramValues: new Dictionary<string, string>());

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);

        using var ms = new MemoryStream(bytes);
        using var wb = new XLWorkbook(ms);

        Assert.Equal(2, wb.Worksheets.Count);
        Assert.Equal("Summary", wb.Worksheet(1).Name);
        Assert.Equal("Results", wb.Worksheet(2).Name);
        Assert.Equal("Bos sonuc bulundu.", wb.Worksheet(2).Cell(1, 1).GetString());
    }

    [Fact]
    public void BuildReportXlsx_summary_contains_metadata()
    {
        var bytes = _sut.BuildReportXlsx(
            rows: new List<Dictionary<string, object>>
            {
                new() { ["Col"] = "A" }
            },
            reportTitle: "Test Raporu",
            username: "fikri",
            runAt: new DateTime(2026, 4, 28, 14, 0, 0),
            paramValues: new Dictionary<string, string> { ["StartDate"] = "2026-04-01", ["Count"] = "10" });

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        var summary = wb.Worksheet("Summary");

        Assert.Equal("Rapor Ozeti", summary.Cell(1, 1).GetString());
        Assert.Equal("Test Raporu", summary.Cell(2, 2).GetString());
        Assert.Equal("fikri", summary.Cell(3, 2).GetString());
        Assert.Equal("2026-04-28 14:00:00", summary.Cell(4, 2).GetString());
        Assert.Contains("StartDate=2026-04-01", summary.Cell(5, 2).GetString());
        Assert.Contains("Count=10", summary.Cell(5, 2).GetString());
    }

    [Fact]
    public void BuildReportXlsx_results_renders_dynamic_headers_and_rows()
    {
        var rows = new List<Dictionary<string, object>>
        {
            new() { ["Id"] = "1", ["Title"] = "Alpha", ["Count"] = 100 },
            new() { ["Id"] = "2", ["Title"] = "Beta",  ["Count"] = 200 }
        };

        var bytes = _sut.BuildReportXlsx(rows, "X", "u", DateTime.UtcNow,
            new Dictionary<string, string>());

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        var results = wb.Worksheet("Results");

        Assert.Equal("Id", results.Cell(1, 1).GetString());
        Assert.Equal("Title", results.Cell(1, 2).GetString());
        Assert.Equal("Count", results.Cell(1, 3).GetString());

        Assert.Equal("1", results.Cell(2, 1).GetString());
        Assert.Equal("Alpha", results.Cell(2, 2).GetString());
        Assert.Equal("100", results.Cell(2, 3).GetString());

        Assert.Equal("Beta", results.Cell(3, 2).GetString());
        Assert.Equal("200", results.Cell(3, 3).GetString());
    }

    [Fact]
    public void BuildReportXlsx_no_params_renders_dash()
    {
        var bytes = _sut.BuildReportXlsx(
            rows: new List<Dictionary<string, object>>(),
            reportTitle: "X",
            username: "u",
            runAt: DateTime.UtcNow,
            paramValues: new Dictionary<string, string>());

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        Assert.Equal("-", wb.Worksheet("Summary").Cell(5, 2).GetString());
    }

    [Fact]
    public void BuildReportXlsx_handles_null_cell_values()
    {
        var rows = new List<Dictionary<string, object>>
        {
            new() { ["Id"] = "1", ["Title"] = null! }
        };

        var bytes = _sut.BuildReportXlsx(rows, "X", "u", DateTime.UtcNow,
            new Dictionary<string, string>());

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        Assert.Equal("", wb.Worksheet("Results").Cell(2, 2).GetString());
    }
}
