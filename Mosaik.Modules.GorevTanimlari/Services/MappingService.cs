using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Mosaik.Modules.GorevTanimlari.Entities;

namespace Mosaik.Modules.GorevTanimlari.Services;

// Zirve personel ↔ görev-tanımı eşleme. Kişi-düzeyi (Personelno) authoritative;
// kişi eşlemesi yoksa rol-varsayılanı (GorevZirveMap Departman+Unvan) önerilir.
// Ana MosaikContext modülden görünmez → base DbContext inject edilir (SopController da
// base DbContext alır). IK datasource connstring'i DataSources tablosundan raw SQL ile
// okunur (bu modüle özel; SP'li StoredProcedureExecutor deseninin hafif varyantı).
public class MappingService
{
    private readonly DbContext _db;
    public MappingService(DbContext db) => _db = db;

    public sealed class PersonRow
    {
        public string Personelno { get; set; } = "";
        public string AdSoyad { get; set; } = "";
        public string Departman { get; set; } = "";
        public string Unvan { get; set; } = "";
        public int? DocId { get; set; }
        public string Source { get; set; } = ""; // kişi | rol | (boş=GAP)
    }

    public async Task<List<PersonRow>> GetPeopleAsync()
    {
        var ik = await _db.Database
            .SqlQueryRaw<string>("SELECT ConnString AS Value FROM dbo.DataSources WHERE DataSourceKey = 'IK'")
            .FirstOrDefaultAsync();
        if (string.IsNullOrWhiteSpace(ik))
            throw new InvalidOperationException("IK veri kaynağı (DataSources.DataSourceKey='IK') bulunamadı.");

        var people = new List<PersonRow>();
        await using (var cn = new SqlConnection(ik))
        {
            await cn.OpenAsync();
            using var cmd = cn.CreateCommand();
            cmd.CommandText =
                "SELECT Personelno, AdSoyad, ISNULL(Departman,N'—') AS Departman, ISNULL(Unvan,N'—') AS Unvan " +
                "FROM dbo.vw_PersonelDepartman WHERE Ict IS NULL ORDER BY Departman, AdSoyad";
            using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
                people.Add(new PersonRow
                {
                    // vw_PersonelDepartman kolon tipleri değişken (Personelno string dönebilir) → defansif dönüşüm.
                    Personelno = rd["Personelno"]?.ToString() ?? "",
                    AdSoyad = rd["AdSoyad"]?.ToString() ?? "",
                    Departman = rd["Departman"]?.ToString() ?? "—",
                    Unvan = rd["Unvan"]?.ToString() ?? "—",
                });
        }

        var personMap = await _db.Set<GorevPersonelMap>().AsNoTracking()
            .ToDictionaryAsync(m => m.Personelno, m => m.GorevDocumentId);
        var roleMap = await _db.Set<GorevZirveMap>().AsNoTracking().ToListAsync();
        var roleDict = roleMap.ToDictionary(m => (m.ZirveDepartman ?? "—") + "||" + m.ZirveUnvan, m => m.GorevDocumentId);

        foreach (var p in people)
        {
            if (personMap.TryGetValue(p.Personelno, out var did)) { p.DocId = did; p.Source = "kişi"; }
            else if (roleDict.TryGetValue(p.Departman + "||" + p.Unvan, out var rid)) { p.DocId = rid; p.Source = "rol"; }
        }
        return people;
    }

    public async Task<List<GorevDocument>> DocsAsync() =>
        await _db.Set<GorevDocument>().AsNoTracking()
            .Where(d => d.IsActive).OrderBy(d => d.PositionCode).ToListAsync();

    // docId null → kişi override'ı sil (rol varsayılanına düşer).
    public async Task SaveAsync(string personelno, int? docId, string? user)
    {
        var existing = await _db.Set<GorevPersonelMap>().FirstOrDefaultAsync(m => m.Personelno == personelno);
        if (docId is null)
        {
            if (existing != null) { _db.Remove(existing); await _db.SaveChangesAsync(); }
            return;
        }
        if (existing == null)
            _db.Add(new GorevPersonelMap
            {
                Personelno = personelno,
                GorevDocumentId = docId.Value,
                Source = "manual",
                MappedBy = user,
                MappedAt = DateTime.UtcNow
            });
        else
        {
            existing.GorevDocumentId = docId.Value;
            existing.Source = "manual";
            existing.MappedBy = user;
            existing.MappedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();
    }
}
