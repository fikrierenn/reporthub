using System.Globalization;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Mosaik.Modules.Forms.Entities;

namespace Mosaik.Modules.Forms.Services
{
    // Plan 56 M-A G2 — submission admin görüntüleme + Excel export. Şifreli alan (IsEncrypted) decrypt
    // YETKİ-GATED (canDecrypt = İhbar Komitesi/admin) — yetkisize "🔒 Şifreli" maske (mosaik-security danış).
    // Export şifreli alanı HER ZAMAN maskeler (bulk-exfil riski) — çözme sadece tekil detay + yetki + audit.
    public class FormSubmissionQueryService(DbContext db, FormEncryptionService encryption)
    {
        public sealed record ListRow(int Id, DateTime SubmittedAt, string? SubmitterEmail, byte Status, int ValueCount, bool HasEncrypted);
        public sealed record ValueRow(string Label, string FieldKey, string Display, bool Encrypted, bool Masked, int? FileId);
        public sealed record Detail(int Id, int FormDefinitionId, DateTime SubmittedAt, string? SubmitterEmail, string? SubmitterIp, byte Status, IReadOnlyList<ValueRow> Values, bool AnyEncrypted, bool AnyDecrypted);

        // Form firma-ownership doğrula (yetkisiz firma submission'ı görmesin).
        private async Task<FormDefinition?> OwnedFormAsync(int formId, int firmaId, CancellationToken ct)
            => await db.Set<FormDefinition>().AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == formId && f.FirmaId == firmaId, ct);

        public async Task<(FormDefinition Form, List<ListRow> Rows)?> ListAsync(int formId, int firmaId, CancellationToken ct)
        {
            var form = await OwnedFormAsync(formId, firmaId, ct);
            if (form is null) return null;
            var rows = await db.Set<FormSubmission>().AsNoTracking()
                .Where(s => s.FormDefinitionId == formId && s.FirmaId == firmaId)
                .OrderByDescending(s => s.Id)
                .Select(s => new ListRow(s.Id, s.SubmittedAt, s.SubmitterEmail, s.Status,
                    s.Values.Count, s.Values.Any(v => v.IsEncrypted)))
                .ToListAsync(ct);
            return (form, rows);
        }

        public async Task<Detail?> DetailAsync(int submissionId, int firmaId, bool canDecrypt, CancellationToken ct)
        {
            var sub = await db.Set<FormSubmission>().AsNoTracking()
                .Include(s => s.Values)
                .FirstOrDefaultAsync(s => s.Id == submissionId && s.FirmaId == firmaId, ct);
            if (sub is null) return null;

            var labels = await db.Set<FormField>().AsNoTracking()
                .Where(f => f.FormDefinitionId == sub.FormDefinitionId)
                .ToDictionaryAsync(f => f.FieldKey, f => f.Label, ct);

            var rows = sub.Values.Select(v =>
            {
                if (v.IsEncrypted)
                {
                    if (canDecrypt && !string.IsNullOrEmpty(v.ValueText) && encryption.TryDecrypt(v.ValueText, out var plain))
                        return new ValueRow(labels.GetValueOrDefault(v.FieldKey, v.FieldKey), v.FieldKey, plain, true, false, v.ValueFileId);
                    return new ValueRow(labels.GetValueOrDefault(v.FieldKey, v.FieldKey), v.FieldKey, "🔒 Şifreli", true, true, v.ValueFileId);
                }
                return new ValueRow(labels.GetValueOrDefault(v.FieldKey, v.FieldKey), v.FieldKey, Plain(v), false, false, v.ValueFileId);
            }).ToList();

            // AnyDecrypted = gerçekten çözülen alan var mı (Encrypted ama Masked değil) — audit "ne oldu"
            // olmalı "ne izinli" değil (security-reviewer M-1: key kaybında yetkili görse de çözülmez).
            return new Detail(sub.Id, sub.FormDefinitionId, sub.SubmittedAt, sub.SubmitterEmail, sub.SubmitterIp, sub.Status, rows,
                rows.Any(r => r.Encrypted), rows.Any(r => r.Encrypted && !r.Masked));
        }

        // Excel export — şifreli alan HER ZAMAN "🔒" (bulk export'ta çözme yok). Safe() formül-injection guard.
        // Export bellekte tüm satırı materialize eder → Take(ExportRowCap) runaway guard (code-reviewer).
        // Aşılırsa en yeni ExportRowCap satır alınır; controller audit'e sayı yazar.
        public const int ExportRowCap = 5000;

        public async Task<(FormDefinition Form, byte[] Bytes, int Count)?> ExportAsync(int formId, int firmaId, CancellationToken ct)
        {
            var form = await OwnedFormAsync(formId, firmaId, ct);
            if (form is null) return null;

            var fields = await db.Set<FormField>().AsNoTracking()
                .Where(f => f.FormDefinitionId == formId && f.FieldType != FormFieldType.Section && f.FieldType != FormFieldType.Hidden)
                .OrderBy(f => f.Order).Select(f => new { f.FieldKey, f.Label }).ToListAsync(ct);

            var subs = await db.Set<FormSubmission>().AsNoTracking()
                .Where(s => s.FormDefinitionId == formId && s.FirmaId == firmaId)
                .Include(s => s.Values)
                .OrderByDescending(s => s.Id).Take(ExportRowCap).ToListAsync(ct);

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Yanıtlar");
            ws.Cell(1, 1).Value = "Tarih"; ws.Cell(1, 2).Value = "Durum"; ws.Cell(1, 3).Value = "E-posta";
            for (var c = 0; c < fields.Count; c++) ws.Cell(1, 4 + c).Value = Safe(fields[c].Label);
            ws.Row(1).Style.Font.Bold = true;

            for (var r = 0; r < subs.Count; r++)
            {
                var s = subs[r];
                ws.Cell(r + 2, 1).Value = s.SubmittedAt;
                ws.Cell(r + 2, 2).Value = Safe(FormLabels.Status(s.Status));
                ws.Cell(r + 2, 3).Value = Safe(s.SubmitterEmail ?? "");
                var byKey = s.Values.ToDictionary(v => v.FieldKey, v => v);
                for (var c = 0; c < fields.Count; c++)
                {
                    if (!byKey.TryGetValue(fields[c].FieldKey, out var v)) continue;
                    ws.Cell(r + 2, 4 + c).Value = v.IsEncrypted ? "🔒" : Safe(Plain(v));
                }
            }
            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return (form, ms.ToArray(), subs.Count);
        }

        private static string Plain(FormSubmissionFieldValue v)
        {
            if (v.ValueText is not null) return v.ValueText;
            if (v.ValueNumber is not null) return v.ValueNumber.Value.ToString(CultureInfo.InvariantCulture);
            if (v.ValueDate is not null) return v.ValueDate.Value.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            if (v.ValueBool is not null) return v.ValueBool.Value ? "Evet" : "Hayır";
            if (v.ValueFileId is not null) return "[dosya #" + v.ValueFileId + "]";
            return "";
        }

        // Formül-injection guard (=,+,-,@ ile başlayan hücre) — VerbisExporter deseni.
        private static string Safe(string s)
            => s.Length > 0 && (s[0] is '=' or '+' or '-' or '@') ? "'" + s : s;
    }
}
