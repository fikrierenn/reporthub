using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Domain;
using Mosaik.Core.Logging;
using Mosaik.Modules.Circular.Models;

namespace Mosaik.Modules.Circular.Services
{
    // Plan 17 Faz C — DailyBlock CRUD service. DbContext base resolve (Plan 16.6).
    // Audit log: blok_create / blok_update / blok_delete / blok_yayinda
    public class BlockService
    {
        private readonly DbContext _db;
        private readonly IAuditLog _audit;

        public BlockService(DbContext db, IAuditLog audit)
        {
            _db = db;
            _audit = audit;
        }

        private DbSet<DailyBlock> Blocks => _db.Set<DailyBlock>();

        public async Task<List<DailyBlock>> ListAsync(bool? bekleyenler = null, int? createdById = null)
        {
            var q = Blocks.AsNoTracking().Where(b => b.IsActive);
            if (createdById.HasValue) q = q.Where(b => b.CreatedById == createdById.Value);
            if (bekleyenler == true) q = q.Where(b => b.CircularId == null);
            else if (bekleyenler == false) q = q.Where(b => b.CircularId != null);
            return await q.OrderByDescending(b => b.BlockDate).ThenByDescending(b => b.Id).ToListAsync();
        }

        public Task<DailyBlock?> GetAsync(int id) =>
            Blocks.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id && b.IsActive);

        public async Task<ServiceResult<DailyBlock>> CreateAsync(
            int createdById, string department, string subject, string content,
            int blockTypeId, bool isUrgent)
        {
            if (string.IsNullOrWhiteSpace(subject))
                return ServiceResult<DailyBlock>.Failure("Konu zorunludur.");
            if (string.IsNullOrWhiteSpace(content))
                return ServiceResult<DailyBlock>.Failure("İçerik zorunludur.");
            if (blockTypeId <= 0)
                return ServiceResult<DailyBlock>.Failure("Blok türü seçilmeli.");

            var bugun = DateTime.UtcNow.Date;
            var blockNumber = await GenerateBlockNumberAsync(bugun);

            var block = new DailyBlock
            {
                BlockNumber = blockNumber,
                CreatedById = createdById,
                Department = (department ?? "").Trim(),
                Subject = subject.Trim(),
                Content = QuillContentSanitizer.Sanitize(content),
                BlockDate = bugun,
                BlockTypeId = blockTypeId,
                IsUrgent = isUrgent,
                IsActive = true,
                CreatedBy = createdById.ToString()
            };
            Blocks.Add(block);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(
                eventType: "block_create",
                targetType: "dailyBlock",
                targetKey: block.Id.ToString(),
                description: $"Blok oluşturuldu: {subject}");

            return ServiceResult<DailyBlock>.Ok(block, "Blok oluşturuldu.");
        }

        public async Task<ServiceResult> UpdateAsync(
            int id, string subject, string content, int blockTypeId, bool isUrgent, int updatedById)
        {
            var block = await Blocks.FirstOrDefaultAsync(b => b.Id == id && b.IsActive);
            if (block == null) return ServiceResult.Failure("Blok bulunamadı.");
            if (block.CircularId.HasValue)
                return ServiceResult.Failure("Yayınlanmış blok düzenlenemez.");

            block.Subject = (subject ?? "").Trim();
            block.Content = QuillContentSanitizer.Sanitize(content);
            block.BlockTypeId = blockTypeId;
            block.IsUrgent = isUrgent;
            block.UpdatedAt = DateTime.UtcNow;
            block.UpdatedBy = updatedById.ToString();
            await _db.SaveChangesAsync();

            await _audit.LogAsync(
                eventType: "block_update",
                targetType: "dailyBlock",
                targetKey: id.ToString());

            return ServiceResult.Ok("Blok güncellendi.");
        }

        public async Task<ServiceResult> DeleteAsync(int id, int deletedById)
        {
            var block = await Blocks.FirstOrDefaultAsync(b => b.Id == id && b.IsActive);
            if (block == null) return ServiceResult.Failure("Blok bulunamadı.");
            if (block.CircularId.HasValue)
                return ServiceResult.Failure("Yayınlanmış blok silinemez.");

            block.IsActive = false;
            block.UpdatedAt = DateTime.UtcNow;
            block.UpdatedBy = deletedById.ToString();
            await _db.SaveChangesAsync();

            await _audit.LogAsync(
                eventType: "block_delete",
                targetType: "dailyBlock",
                targetKey: id.ToString(),
                description: "Soft-delete (IsActive=0)");

            return ServiceResult.Ok("Blok silindi.");
        }

        // BLK-YYYYMMDD-NNN — günlük 1'den başlayan sayaç
        private async Task<string> GenerateBlockNumberAsync(DateTime gun)
        {
            var prefix = $"BLK-{gun:yyyyMMdd}-";
            var sonNo = await _db.Set<DailyBlock>()
                .Where(b => b.BlockNumber.StartsWith(prefix))
                .OrderByDescending(b => b.BlockNumber)
                .Select(b => b.BlockNumber)
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
