using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Domain;
using Mosaik.Modules.Tamim.Models;

namespace Mosaik.Modules.Tamim.Services
{
    // Plan 17 Faz B — Tamim CRUD service. DbContext base resolve edilir
    // (Plan 16.6 cross-csproj pattern). MosaikContext'e referans yok.
    public class TamimService
    {
        private readonly DbContext _db;

        public TamimService(DbContext db)
        {
            _db = db;
        }

        private DbSet<Models.Tamim> Tamim => _db.Set<Models.Tamim>();
        private DbSet<TamimReadLog> ReadLogs => _db.Set<TamimReadLog>();

        public async Task<List<Models.Tamim>> ListAsync(TamimStatus? statusFilter = null)
        {
            var q = Tamim.AsNoTracking();
            if (statusFilter.HasValue) q = q.Where(t => t.Status == statusFilter.Value);
            return await q.OrderByDescending(t => t.CreatedAt).ToListAsync();
        }

        public Task<Models.Tamim?> GetAsync(int id) =>
            Tamim.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);

        public async Task<ServiceResult<Models.Tamim>> CreateAsync(
            string title, string body, int createdById, DateTime? publishDate, DateTime? expiresAt)
        {
            if (string.IsNullOrWhiteSpace(title))
                return ServiceResult<Models.Tamim>.Failure("Başlık zorunludur.");
            if (string.IsNullOrWhiteSpace(body))
                return ServiceResult<Models.Tamim>.Failure("İçerik zorunludur.");

            var tamim = new Models.Tamim
            {
                Title = title.Trim(),
                Body = body,
                Status = TamimStatus.Draft,
                PublishDate = publishDate,
                ExpiresAt = expiresAt,
                CreatedById = createdById,
                CreatedBy = createdById.ToString()
            };
            Tamim.Add(tamim);
            await _db.SaveChangesAsync();
            return ServiceResult<Models.Tamim>.Ok(tamim, "Tamim olusturuldu.");
        }

        public async Task<ServiceResult> UpdateAsync(
            int id, string title, string body, DateTime? publishDate, DateTime? expiresAt, int updatedById)
        {
            var tamim = await Tamim.FirstOrDefaultAsync(t => t.Id == id);
            if (tamim == null) return ServiceResult.Failure("Tamim bulunamadi.");
            if (tamim.Status == TamimStatus.Published)
                return ServiceResult.Failure("Yayindaki tamim duzenlenemez.");

            tamim.Title = (title ?? "").Trim();
            tamim.Body = body ?? "";
            tamim.PublishDate = publishDate;
            tamim.ExpiresAt = expiresAt;
            tamim.UpdatedAt = DateTime.UtcNow;
            tamim.UpdatedBy = updatedById.ToString();
            await _db.SaveChangesAsync();
            return ServiceResult.Ok("Tamim guncellendi.");
        }

        public async Task<ServiceResult> DeleteAsync(int id, int deletedById)
        {
            var tamim = await Tamim.FirstOrDefaultAsync(t => t.Id == id);
            if (tamim == null) return ServiceResult.Failure("Tamim bulunamadi.");
            if (tamim.Status == TamimStatus.Published)
                return ServiceResult.Failure("Yayindaki tamim silinemez. Once arsivleyin.");

            Tamim.Remove(tamim);
            await _db.SaveChangesAsync();
            return ServiceResult.Ok("Tamim silindi.");
        }

        public async Task<ServiceResult> ChangeStatusAsync(int id, TamimStatus newStatus, int actorUserId)
        {
            var tamim = await Tamim.FirstOrDefaultAsync(t => t.Id == id);
            if (tamim == null) return ServiceResult.Failure("Tamim bulunamadi.");

            tamim.Status = newStatus;
            tamim.UpdatedAt = DateTime.UtcNow;
            tamim.UpdatedBy = actorUserId.ToString();

            if (newStatus == TamimStatus.Approved || newStatus == TamimStatus.Published)
                tamim.ApprovedById = actorUserId;
            if (newStatus == TamimStatus.Published && tamim.PublishDate == null)
                tamim.PublishDate = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return ServiceResult.Ok($"Status: {newStatus}.");
        }
    }
}
