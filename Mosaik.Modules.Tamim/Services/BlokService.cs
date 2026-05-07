using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Domain;
using Mosaik.Core.Logging;
using Mosaik.Modules.Tamim.Models;

namespace Mosaik.Modules.Tamim.Services
{
    // Plan 17 Faz C — GunlukBlok CRUD service. DbContext base resolve (Plan 16.6).
    // Audit log: blok_create / blok_update / blok_delete / blok_yayinda
    public class BlokService
    {
        private readonly DbContext _db;
        private readonly IAuditLog _audit;

        public BlokService(DbContext db, IAuditLog audit)
        {
            _db = db;
            _audit = audit;
        }

        private DbSet<GunlukBlok> Bloklar => _db.Set<GunlukBlok>();

        public async Task<List<GunlukBlok>> ListAsync(bool? bekleyenler = null)
        {
            var q = Bloklar.AsNoTracking().Where(b => b.IsActive);
            if (bekleyenler == true) q = q.Where(b => b.TamimId == null);
            else if (bekleyenler == false) q = q.Where(b => b.TamimId != null);
            return await q.OrderByDescending(b => b.BlokTarihi).ThenByDescending(b => b.Id).ToListAsync();
        }

        public Task<GunlukBlok?> GetAsync(int id) =>
            Bloklar.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id && b.IsActive);

        public async Task<ServiceResult<GunlukBlok>> CreateAsync(
            int olusturanId, string departmanAdi, string konu, string aciklama,
            int blokTuruId, bool acil)
        {
            if (string.IsNullOrWhiteSpace(konu))
                return ServiceResult<GunlukBlok>.Failure("Konu zorunludur.");
            if (string.IsNullOrWhiteSpace(aciklama))
                return ServiceResult<GunlukBlok>.Failure("Aciklama zorunludur.");
            if (blokTuruId <= 0)
                return ServiceResult<GunlukBlok>.Failure("Blok turu secilmeli.");

            var bugun = DateTime.UtcNow.Date;
            var blokNo = await GenerateBlokNoAsync(bugun);

            var blok = new GunlukBlok
            {
                BlokNo = blokNo,
                OlusturanId = olusturanId,
                DepartmanAdi = (departmanAdi ?? "").Trim(),
                Konu = konu.Trim(),
                Aciklama = aciklama,
                BlokTarihi = bugun,
                BlokTuruId = blokTuruId,
                Acil = acil,
                IsActive = true,
                CreatedBy = olusturanId.ToString()
            };
            Bloklar.Add(blok);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(
                eventType: "blok_create",
                targetType: "gunlukBlok",
                targetKey: blok.Id.ToString(),
                description: $"Blok olusturuldu: {konu}");

            return ServiceResult<GunlukBlok>.Ok(blok, "Blok olusturuldu.");
        }

        public async Task<ServiceResult> UpdateAsync(
            int id, string konu, string aciklama, int blokTuruId, bool acil, int updatedById)
        {
            var blok = await Bloklar.FirstOrDefaultAsync(b => b.Id == id && b.IsActive);
            if (blok == null) return ServiceResult.Failure("Blok bulunamadi.");
            if (blok.TamimId.HasValue)
                return ServiceResult.Failure("Yayinlanmis blok duzenlenemez.");

            blok.Konu = (konu ?? "").Trim();
            blok.Aciklama = aciklama ?? "";
            blok.BlokTuruId = blokTuruId;
            blok.Acil = acil;
            blok.UpdatedAt = DateTime.UtcNow;
            blok.UpdatedBy = updatedById.ToString();
            await _db.SaveChangesAsync();

            await _audit.LogAsync(
                eventType: "blok_update",
                targetType: "gunlukBlok",
                targetKey: id.ToString());

            return ServiceResult.Ok("Blok guncellendi.");
        }

        public async Task<ServiceResult> DeleteAsync(int id, int deletedById)
        {
            var blok = await Bloklar.FirstOrDefaultAsync(b => b.Id == id && b.IsActive);
            if (blok == null) return ServiceResult.Failure("Blok bulunamadi.");
            if (blok.TamimId.HasValue)
                return ServiceResult.Failure("Yayinlanmis blok silinemez.");

            blok.IsActive = false;
            blok.UpdatedAt = DateTime.UtcNow;
            blok.UpdatedBy = deletedById.ToString();
            await _db.SaveChangesAsync();

            await _audit.LogAsync(
                eventType: "blok_delete",
                targetType: "gunlukBlok",
                targetKey: id.ToString(),
                description: "Soft-delete (IsActive=0)");

            return ServiceResult.Ok("Blok silindi.");
        }

        // BLK-YYYYMMDD-NNN — günlük 1'den başlayan sayaç
        private async Task<string> GenerateBlokNoAsync(DateTime gun)
        {
            var prefix = $"BLK-{gun:yyyyMMdd}-";
            var sonNo = await _db.Set<GunlukBlok>()
                .Where(b => b.BlokNo.StartsWith(prefix))
                .OrderByDescending(b => b.BlokNo)
                .Select(b => b.BlokNo)
                .FirstOrDefaultAsync();

            int next = 1;
            if (!string.IsNullOrEmpty(sonNo))
            {
                var sayiKismi = sonNo.Substring(prefix.Length);
                if (int.TryParse(sayiKismi, out var n)) next = n + 1;
            }
            return $"{prefix}{next:D3}";
        }
    }
}
