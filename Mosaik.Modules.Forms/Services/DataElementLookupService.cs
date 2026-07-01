using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mosaik.Modules.Forms.Services
{
    // Plan 41 Faz 4 — KVKK DataElement picker (admin field→DataElement eşleme UI).
    // Cross-modül: Kvkk.DataElement CLR tipine compile bağı YOK (ADR-002) → raw SQL (Faz 3 pattern,
    // KvkkProcessService.cs:150). KvkkDataElements 60 global seed, FirmaId YOK → firma filtresi gereksiz.
    // Kvkk modülü kurulu değilse tablo yok → SqlException → boş liste + LogWarning (graceful degrade).
    public class DataElementLookupService(DbContext db, ILogger<DataElementLookupService> logger)
    {
        public sealed record DataElementLookup(int Id, string ElementCode, string DisplayName, bool IsSpecialCategory);

        public async Task<IReadOnlyList<DataElementLookup>> ListActiveAsync(CancellationToken ct = default)
        {
            try
            {
                return await db.Database.SqlQueryRaw<DataElementLookup>(
                        "SELECT Id, ElementCode, DisplayName, IsSpecialCategory FROM dbo.KvkkDataElements WHERE IsActive = 1 ORDER BY DisplayName")
                    .ToListAsync(ct);
            }
            catch (DbException ex)
            {
                // KvkkDataElements sorgusu başarısız — KVKK modülü kurulu DEĞİL (tablo yok) VEYA DB geçici
                // erişilemez. İkisi de opsiyonel picker için boş liste demek; ayırt etmiyoruz (log ikisini de kapsar).
                logger.LogWarning(ex, "KvkkDataElements sorgulanamadı — boş liste dönülüyor (KVKK modülü yok olabilir ya da DB geçici erişilemez).");
                return [];
            }
        }

        public async Task<bool> ExistsAsync(int dataElementId, CancellationToken ct = default)
        {
            try
            {
                var rows = await db.Database.SqlQueryRaw<int>(
                        "SELECT Id AS Value FROM dbo.KvkkDataElements WHERE Id = {0} AND IsActive = 1", dataElementId)
                    .ToListAsync(ct);
                return rows.Count > 0;
            }
            catch (DbException ex)
            {
                // Fail-closed: doğrulanamadı → false (map reddedilir; unvalidated DataElementId persist edilmez).
                logger.LogWarning(ex, "KvkkDataElements doğrulanamadı (Id={Id}) — fail-closed, eşleme reddedildi.", dataElementId);
                return false;
            }
        }
    }
}
