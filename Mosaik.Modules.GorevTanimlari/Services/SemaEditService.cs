using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Domain;
using Mosaik.Modules.GorevTanimlari.Entities;

namespace Mosaik.Modules.GorevTanimlari.Services;

// Org şeması düzenleme — DB yazma (OrgPositions omurgası + GorevVersions içerik). Admin-only (controller gate).
// Beklenen iş sonuçları (bulunamadı/çakışma/döngü) → (ok,err) tuple; gerçek arıza → SaveChanges throw eder.
public class SemaEditService
{
    private readonly DbContext _db;
    public SemaEditService(DbContext db) => _db = db;

    public sealed record KpiDto(string Text, bool Active);

    public async Task<(bool ok, string? err)> RenameAsync(int id, string? title, string? holder, string? user, CancellationToken ct)
    {
        var t = (title ?? "").Trim();
        if (t.Length == 0) return (false, "Ünvan boş olamaz.");
        if (t.Length > 150) return (false, "Ünvan en fazla 150 karakter olabilir.");
        var h = string.IsNullOrWhiteSpace(holder) ? null : holder.Trim();
        if (h is { Length: > 150 }) return (false, "Kişi adı en fazla 150 karakter olabilir.");
        var p = await _db.Set<OrgPosition>().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return (false, "Pozisyon bulunamadı.");
        p.Title = t;
        p.HolderName = h;
        // ZirveMatchKey Title ile senkron kalmalı — aksi halde OrgChart Zirve incumbent match
        // (ZirveMatchKey ?? Title) eski değerde takılır, rename sonrası eşleşme sessizce kırılır.
        p.ZirveMatchKey = t;
        p.UpdatedAt = DateTime.UtcNow;
        p.UpdatedBy = user;
        await _db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<(bool ok, string? err, int id)> AddAsync(int? parentId, string? title, string? holder, string? user, CancellationToken ct)
    {
        var baseCode = (title ?? "").Trim();
        if (baseCode.Length == 0) return (false, "Ünvan boş olamaz.", 0);
        if (baseCode.Length > 150) return (false, "Ünvan en fazla 150 karakter olabilir.", 0);
        // Code UNIQUE + MaxLength(100). baseCode + " (n)" suffix 100'ü aşabilir → 90'a kırp.
        if (baseCode.Length > 90) return (false, "Ünvan çok uzun (Code için en fazla 90 karakter).", 0);
        var h = string.IsNullOrWhiteSpace(holder) ? null : holder.Trim();
        if (h is { Length: > 150 }) return (false, "Kişi adı en fazla 150 karakter olabilir.", 0);
        if (parentId is not null && !await _db.Set<OrgPosition>().AnyAsync(x => x.Id == parentId && x.IsActive, ct))
            return (false, "Üst pozisyon bulunamadı.", 0);

        var code = baseCode;
        var n = 2;
        while (await _db.Set<OrgPosition>().AnyAsync(x => x.Code == code, ct))
            code = baseCode + " (" + n++ + ")";

        var maxOrder = await _db.Set<OrgPosition>().Where(x => x.ParentPositionId == parentId)
            .Select(x => (int?)x.DisplayOrder).MaxAsync(ct) ?? -1;

        var p = new OrgPosition
        {
            Code = code,
            Title = baseCode,
            HolderName = h,
            ParentPositionId = parentId,
            DisplayOrder = maxOrder + 1,
            IsActive = true,
            ZirveMatchKey = baseCode,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = user
        };
        _db.Set<OrgPosition>().Add(p);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Eşzamanlı ekleme yarışında Code UNIQUE çakışması — actionable mesaj (generic 500 değil).
            return (false, "Bu ünvan aynı anda eklendi. Lütfen tekrar deneyin.", 0);
        }
        return (true, null, p.Id);
    }

    public async Task<(bool ok, string? err)> DeleteAsync(int id, string? user, CancellationToken ct)
    {
        var p = await _db.Set<OrgPosition>().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return (false, "Pozisyon bulunamadı.");
        if (await _db.Set<OrgPosition>().AnyAsync(x => x.ParentPositionId == id && x.IsActive, ct))
            return (false, "Alt pozisyonu olan düğüm silinemez. Önce altları taşıyın veya silin.");
        p.IsActive = false;
        p.UpdatedAt = DateTime.UtcNow;
        p.UpdatedBy = user;
        await _db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<(bool ok, string? err)> MoveAsync(int id, int? newParentId, string? user, CancellationToken ct)
    {
        if (id == newParentId) return (false, "Bir pozisyon kendine bağlanamaz.");
        var p = await _db.Set<OrgPosition>().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return (false, "Pozisyon bulunamadı.");
        if (newParentId is not null && !await _db.Set<OrgPosition>().AnyAsync(x => x.Id == newParentId && x.IsActive, ct))
            return (false, "Hedef üst pozisyon bulunamadı.");

        if (newParentId is not null)
        {
            var pairs = await _db.Set<OrgPosition>().AsNoTracking()
                .Select(x => new { x.Id, x.ParentPositionId }).ToListAsync(ct);
            var parentOf = pairs.ToDictionary(x => x.Id, x => x.ParentPositionId);
            int? cur = newParentId;
            var hops = 0;
            while (cur is not null && hops++ < 500)
            {
                if (cur == id) return (false, "Döngü oluşur — pozisyon kendi alt ağacına taşınamaz.");
                cur = parentOf.TryGetValue(cur.Value, out var pp) ? pp : null;
            }
        }
        // Yeni sibling kümesinde DisplayOrder yeniden hesaplanmalı — aksi halde eski order
        // yeni parent altındaki bir kardeşle çakışır, düğüm beklenmedik sıraya düşer (Add ile tutarlı).
        var maxOrder = await _db.Set<OrgPosition>().Where(x => x.ParentPositionId == newParentId && x.Id != id)
            .Select(x => (int?)x.DisplayOrder).MaxAsync(ct) ?? -1;
        p.ParentPositionId = newParentId;
        p.DisplayOrder = maxOrder + 1;
        p.UpdatedAt = DateTime.UtcNow;
        p.UpdatedBy = user;
        await _db.SaveChangesAsync(ct);
        return (true, null);
    }

    // Görev tanımı içeriği (paragraflar + KPI) → yeni yayın versiyonu (eskiyi süperse). SopVersions deseni.
    public async Task<(bool ok, string? err)> SaveDefinitionAsync(int orgPositionId, List<string> paragraphs, List<KpiDto> kpi, string? user, CancellationToken ct)
    {
        var pos = await _db.Set<OrgPosition>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == orgPositionId, ct);
        if (pos is null) return (false, "Pozisyon bulunamadı.");

        // İki yazma (doc create + version) tek transaction — arada arıza olursa orphan doc kalmasın.
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var doc = await _db.Set<GorevDocument>().FirstOrDefaultAsync(d => d.OrgPositionId == orgPositionId, ct);
        if (doc is null)
        {
            doc = new GorevDocument
            {
                PositionCode = pos.Code,
                Title = pos.Title,
                OrgPositionId = orgPositionId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = user
            };
            _db.Set<GorevDocument>().Add(doc);
            await _db.SaveChangesAsync(ct);
        }

        var maxVer = await _db.Set<GorevVersion>().Where(v => v.GorevDocumentId == doc.Id)
            .Select(v => (int?)v.VersionNumber).MaxAsync(ct) ?? 0;
        var published = await _db.Set<GorevVersion>()
            .Where(v => v.GorevDocumentId == doc.Id && v.Status == 1).ToListAsync(ct);
        foreach (var v in published) { v.Status = 2; v.SupersededDate = DateTime.UtcNow; }

        var cleanParas = paragraphs.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList();
        var cleanKpi = kpi.Where(k => !string.IsNullOrWhiteSpace(k.Text)).Select(k => new { text = k.Text.Trim(), active = k.Active }).ToList();
        var content = JsonSerializer.Serialize(new { paragraphs = cleanParas, kpi = cleanKpi },
            new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
        var plain = string.Join("\n", cleanParas.Concat(cleanKpi.Select(k => "- " + k.text)));

        _db.Set<GorevVersion>().Add(new GorevVersion
        {
            GorevDocumentId = doc.Id,
            VersionNumber = maxVer + 1,
            ContentJson = content,
            PlainTextContent = plain,
            EffectiveDate = DateTime.UtcNow,
            Status = 1,
            CreatedBy = user,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return (true, null);
    }
}
