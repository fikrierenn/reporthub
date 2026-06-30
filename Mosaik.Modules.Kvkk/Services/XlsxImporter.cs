using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mosaik.Modules.Kvkk.Entities;

namespace Mosaik.Modules.Kvkk.Services
{
    // Plan 40 Faz 1 — BKM v7 "Tüm Envanter" (20 sütun) → KvkkProcesses idempotent UPSERT.
    // Natural key: (FirmaId, Department, Name). DataElement alias-match → ProcessDataLinks (+EntityRelations).
    // Yurt dışı aktarım ("Yok" değilse) → CrossBorderTransfer. Re-import: 0 yeni kayıt.
    public class XlsxImporter
    {
        private const string SheetName = "Tüm Envanter";
        private const string FallbackLegalBasisArticle = "5/2-f"; // meşru menfaat — parse edilemeyen sebep için

        private readonly DbContext _db;
        private readonly KvkkProcessService _processes;
        private readonly Mosaik.Core.Logging.IAuditLog _audit;
        private readonly ILogger<XlsxImporter> _logger;

        public XlsxImporter(DbContext db, KvkkProcessService processes,
            Mosaik.Core.Logging.IAuditLog audit, ILogger<XlsxImporter> logger)
        {
            _db = db;
            _processes = processes;
            _audit = audit;
            _logger = logger;
        }

        public sealed record ImportResult(
            int Inserted, int Updated, int Skipped, int LinksCreated, int CrossBorderCreated,
            List<string> Errors, List<string> Warnings);

        public async Task<ImportResult> ImportAsync(string filePath, int firmaId, CancellationToken ct = default)
        {
            int inserted = 0, updated = 0, skipped = 0, links = 0, cbt = 0;
            var errors = new List<string>();
            var warnings = new List<string>();

            // Lookuplar: Article → LegalBasisId; aktif DataElement listesi.
            var legalByArticle = await _db.Set<LegalBasis>().AsNoTracking()
                .ToDictionaryAsync(l => l.Article, l => l.Id, ct);
            if (legalByArticle.Count == 0)
                return new ImportResult(0, 0, 0, 0, 0,
                    new List<string> { "LegalBasis tablosu boş — önce lookup seed'i (02) çalıştırın." }, warnings);
            var fallbackLegalId = legalByArticle.TryGetValue(FallbackLegalBasisArticle, out var fb) ? fb : legalByArticle.Values.First();
            var elements = await _db.Set<DataElement>().AsNoTracking()
                .Where(d => d.IsActive)
                .Select(d => new { d.Id, d.DisplayName, d.Aliases })
                .ToListAsync(ct);

            using var wb = new XLWorkbook(filePath);
            if (!wb.TryGetWorksheet(SheetName, out var ws))
                return new ImportResult(0, 0, 0, 0, 0, new List<string> { $"'{SheetName}' sayfası bulunamadı." }, warnings);

            // (legalByArticle boşluğu yukarıda guard edildi)
            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
            for (int r = 2; r <= lastRow; r++)
            {
                ct.ThrowIfCancellationRequested();
                var name = Cell(ws, r, 5);
                var dept = Cell(ws, r, 2);
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(dept))
                {
                    skipped++;
                    continue;
                }

                try
                {
                    var article = KvkkImportParsers.ParseLegalBasisArticle(Cell(ws, r, 10));
                    var legalId = article != null && legalByArticle.TryGetValue(article, out var lid) ? lid : fallbackLegalId;
                    if (article == null)
                        warnings.Add($"Satır {r}: hukuki sebep parse edilemedi → fallback ({FallbackLegalBasisArticle}).");
                    else if (!legalByArticle.ContainsKey(article))
                        warnings.Add($"Satır {r}: hukuki sebep '{article}' DB'de tanımlı değil → fallback ({FallbackLegalBasisArticle}).");

                    var set = _db.Set<KvkkProcess>();
                    var existing = await set.FirstOrDefaultAsync(
                        p => p.FirmaId == firmaId && p.Department == dept && p.Name == name, ct);

                    bool isNew = existing == null;
                    var p = existing ?? new KvkkProcess { FirmaId = firmaId, CreatedAt = DateTime.UtcNow };
                    p.Department = Trunc(dept, 120)!;   // dept yukarıda null-check'li
                    p.Unit = Trunc(Cell(ws, r, 3), 120);
                    p.Owner = Trunc(Cell(ws, r, 4), 200);
                    p.Name = Trunc(name, 300)!;          // name yukarıda null-check'li
                    p.Purpose = Cell(ws, r, 9);
                    p.LegalBasisId = legalId;
                    p.DataSource = Trunc(Cell(ws, r, 11), 500);
                    p.StorageMedium = Trunc(Cell(ws, r, 12), 500);
                    p.AccessAuthority = Trunc(Cell(ws, r, 13), 500);
                    p.RecipientGroups = Trunc(Cell(ws, r, 14), 500);
                    p.RetentionText = Trunc(Cell(ws, r, 16), 500);  // Saklama Süresi (serbest metin)
                    p.DisposalText = Trunc(Cell(ws, r, 17), 500);   // İmha Yöntemi (serbest metin)
                    p.RiskLevel = KvkkImportParsers.ParseRiskLevel(Cell(ws, r, 20));
                    p.IsActive = true;
                    p.UpdatedAt = DateTime.UtcNow;

                    if (isNew) { set.Add(p); inserted++; } else { updated++; }
                    await _db.SaveChangesAsync(ct);

                    // DataElement tespiti: Veri Kategorisi + Veri Türü serbest metni.
                    var haystack = Cell(ws, r, 6) + " " + Cell(ws, r, 7);
                    foreach (var el in elements)
                    {
                        if (!DataElementMatcher.TextContainsElement(haystack, el.DisplayName, el.Aliases))
                            continue;
                        var lr = await _processes.LinkDataElementAsync(p.Id, el.Id, firmaId, 0, null, ct);
                        if (lr.IsSuccess)
                        {
                            if (lr.Data) links++;   // yalnız yeni yaratılan bağ sayılır (re-import 0)
                        }
                        else
                        {
                            warnings.Add($"Satır {r}: '{el.DisplayName}' veri bağı kurulamadı.");
                            _logger.LogWarning("XlsxImporter: ProcessId={Pid} Element={Eid} bağ kurulamadı: {Msg}",
                                p.Id, el.Id, lr.Message);
                        }
                    }

                    // Yurt dışı aktarım.
                    var yd = Cell(ws, r, 15);
                    if (KvkkImportParsers.HasCrossBorder(yd))
                    {
                        var recipient = Trunc(yd, 300)!;   // yd HasCrossBorder ile non-empty
                        var cbtSet = _db.Set<CrossBorderTransfer>();
                        var dup = await cbtSet.AnyAsync(x => x.ProcessId == p.Id && x.RecipientName == recipient, ct);
                        if (!dup)
                        {
                            cbtSet.Add(new CrossBorderTransfer
                            {
                                ProcessId = p.Id,
                                RecipientName = recipient,
                                Country = Trunc(KvkkImportParsers.FirstCountry(yd), 120) ?? "",
                                Mechanism = KvkkImportParsers.GuessMechanism(yd),
                                LegalReference = Trunc(yd, 200)
                            });
                            await _db.SaveChangesAsync(ct);
                            cbt++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Satır {r}: {name} — işlenemedi.");
                    _logger.LogError(ex, "XlsxImporter: satır {Row} ({Name}) hata.", r, name);
                }
            }

            await _audit.LogAsync(
                eventType: "kvkk_xlsx_import",
                targetType: "kvkk_import",
                targetKey: firmaId.ToString(),
                description: $"KVKK envanter import: +{inserted} ekl, {updated} günc, {skipped} atl, {links} yeni bağ, {cbt} yurtdışı",
                newValuesJson: $"{{\"inserted\":{inserted},\"updated\":{updated},\"links\":{links},\"crossBorder\":{cbt},\"warnings\":{warnings.Count},\"errors\":{errors.Count}}}",
                isSuccess: errors.Count == 0);

            _logger.LogInformation(
                "XlsxImporter tamamlandı. Eklenen:{I} Güncellenen:{U} Atlanan:{S} YeniBağ:{L} YurtDışı:{C} Uyarı:{W} Hata:{E}",
                inserted, updated, skipped, links, cbt, warnings.Count, errors.Count);
            return new ImportResult(inserted, updated, skipped, links, cbt, errors, warnings);
        }

        private static string Cell(IXLWorksheet ws, int row, int col) => ws.Cell(row, col).GetString().Trim();

        private static string? Trunc(string? s, int max) =>
            string.IsNullOrEmpty(s) ? s : (s.Length <= max ? s : s[..max]);
    }
}
