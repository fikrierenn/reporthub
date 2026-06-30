using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Mosaik.Modules.Kvkk.Entities;

namespace Mosaik.Modules.Kvkk.Services
{
    // Plan 54 M6 / Plan 40 Faz 6 — VERBİS / denetim-hazır envanter Excel'i (ClosedXML).
    // Aktif firma süreçleri → KVKK Mart 2025 rehber sütunları. RetentionText/DisposalText
    // (serbest-metin, import'tan korundu) "Saklama Süresi"/"İmha" sütunlarını besler.
    public class VerbisExporter
    {
        private static readonly string[] Headers =
        {
            "Sıra", "Departman", "Birim", "Süreç Sahibi", "Süreç / Faaliyet Adı", "İşleme Amacı",
            "Hukuki Sebep", "Veri Kaynağı", "Saklandığı Ortam", "Erişim Yetkisi",
            "Aktarılan Alıcı Grupları", "Saklama Süresi", "İmha Yöntemi", "Risk", "Onay Durumu"
        };

        private readonly DbContext _db;

        public VerbisExporter(DbContext db) => _db = db;

        // Excel/CSV formula injection guard: =,+,-,@,tab,CR ile başlayan hücreyi
        // tek-tırnakla literal metne çevir (denetçi Excel'de açınca formül çalışmasın).
        public static string Safe(string? s)
        {
            if (string.IsNullOrEmpty(s)) return s ?? "";
            return "=+-@\t\r".IndexOf(s[0]) >= 0 ? "'" + s : s;
        }

        public async Task<byte[]> ExportAsync(int firmaId, CancellationToken ct = default)
        {
            var rows = await _db.Set<KvkkProcess>().AsNoTracking()
                .Include(p => p.LegalBasis)
                .Where(p => p.FirmaId == firmaId && p.IsActive)
                .OrderBy(p => p.Department).ThenBy(p => p.Name)
                .ToListAsync(ct);

            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("VERBIS Envanteri");

            for (int c = 0; c < Headers.Length; c++)
            {
                var cell = ws.Cell(1, c + 1);
                cell.Value = Headers[c];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#111827");
                cell.Style.Font.FontColor = XLColor.White;
            }

            int r = 2;
            int sira = 1;
            foreach (var p in rows)
            {
                // Sütun 2-13 serbest-metin (kullanıcı/import girdisi) → formula-injection guard.
                ws.Cell(r, 1).Value = sira++;
                ws.Cell(r, 2).Value = Safe(p.Department);
                ws.Cell(r, 3).Value = Safe(p.Unit);
                ws.Cell(r, 4).Value = Safe(p.Owner);
                ws.Cell(r, 5).Value = Safe(p.Name);
                ws.Cell(r, 6).Value = Safe(p.Purpose);
                ws.Cell(r, 7).Value = Safe(p.LegalBasis != null ? $"{p.LegalBasis.Article} — {p.LegalBasis.Name}" : "");
                ws.Cell(r, 8).Value = Safe(p.DataSource);
                ws.Cell(r, 9).Value = Safe(p.StorageMedium);
                ws.Cell(r, 10).Value = Safe(p.AccessAuthority);
                ws.Cell(r, 11).Value = Safe(p.RecipientGroups);
                ws.Cell(r, 12).Value = Safe(p.RetentionText);
                ws.Cell(r, 13).Value = Safe(p.DisposalText);
                ws.Cell(r, 14).Value = KvkkLabels.Risk(p.RiskLevel);          // server-üretimi (enum) — güvenli
                ws.Cell(r, 15).Value = KvkkLabels.ReviewStatus(p.ReviewStatus);
                r++;
            }

            ws.SheetView.FreezeRows(1);
            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }
    }
}
