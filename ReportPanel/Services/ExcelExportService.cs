using ClosedXML.Excel;

namespace ReportPanel.Services;

/// <summary>
/// Rapor sonuçlarını Excel (.xlsx) byte array'ine dönüştürür.
/// 2 sheet: Summary (rapor adı + kullanıcı + tarih + parametreler) + Results (data rows).
/// ReportsController.BuildExcelFile static helper'ından çıkarıldı (M-13 R6.1, 28 Nisan 2026).
/// </summary>
public class ExcelExportService
{
    public byte[] BuildReportXlsx(
        List<Dictionary<string, object>> rows,
        string reportTitle,
        string username,
        DateTime runAt,
        Dictionary<string, string> paramValues)
    {
        using var workbook = new XLWorkbook();

        var summary = workbook.Worksheets.Add("Summary");
        summary.Cell(1, 1).Value = "Rapor Ozeti";
        summary.Range(1, 1, 1, 2).Merge().Style.Font.SetBold();

        summary.Cell(2, 1).Value = "Rapor";
        summary.Cell(2, 2).Value = reportTitle;
        summary.Cell(3, 1).Value = "Kullanici";
        summary.Cell(3, 2).Value = username;
        summary.Cell(4, 1).Value = "Tarih";
        summary.Cell(4, 2).Value = runAt.ToString("yyyy-MM-dd HH:mm:ss");
        summary.Cell(5, 1).Value = "Parametreler";
        summary.Cell(5, 2).Value = paramValues.Count == 0
            ? "-"
            : string.Join(", ", paramValues.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        summary.Columns().AdjustToContents();

        var results = workbook.Worksheets.Add("Results");
        if (rows.Count > 0)
        {
            var headers = rows[0].Keys.ToList();
            for (var i = 0; i < headers.Count; i++)
            {
                results.Cell(1, i + 1).Value = headers[i];
                results.Cell(1, i + 1).Style.Font.SetBold();
            }

            for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                var row = rows[rowIndex];
                for (var colIndex = 0; colIndex < headers.Count; colIndex++)
                {
                    var header = headers[colIndex];
                    var value = row.TryGetValue(header, out var v) ? v : "";
                    results.Cell(rowIndex + 2, colIndex + 1).Value = value?.ToString() ?? "";
                }
            }
        }
        else
        {
            results.Cell(1, 1).Value = "Bos sonuc bulundu.";
        }

        results.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
