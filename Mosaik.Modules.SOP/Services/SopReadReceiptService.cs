using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Domain;
using Mosaik.Core.Logging;
using Mosaik.Modules.SOP.Entities;

namespace Mosaik.Modules.SOP.Services
{
    // Plan 34 Faz D — Okundu disiplini.
    // AssignToUsers: SOP yeni versiyonu Approved olunca atanmış departman üyelerine atama.
    //                MarkVersionApprovedAsync hook'undan değil, ayrı flow (departman üye
    //                listesi Plan 18B HR sync sonrası — şimdilik manuel UserId listesi).
    // MarkReadAsync: SOP detay sayfası ilk açıldığında ReadAt set.
    // ConfirmAsync: "Okudum + onayladım" tıklandığında ConfirmedAt + audit log.
    public class SopReadReceiptService
    {
        private readonly DbContext _db;
        private readonly IAuditLog _audit;

        public SopReadReceiptService(DbContext db, IAuditLog audit)
        {
            _db = db;
            _audit = audit;
        }

        private DbSet<SopReadReceipt> Receipts => _db.Set<SopReadReceipt>();
        private DbSet<SopVersion> Versions => _db.Set<SopVersion>();

        // Yeni versiyon Approved → bu user'lara atama. Idempotent (zaten varsa atla).
        public async Task<int> AssignToUsersAsync(int versionId, IEnumerable<int> userIds)
        {
            var existing = await Receipts
                .Where(r => r.SopVersionId == versionId)
                .Select(r => r.UserId)
                .ToListAsync();
            var existingSet = existing.ToHashSet();

            var added = 0;
            foreach (var uid in userIds.Distinct())
            {
                if (existingSet.Contains(uid)) continue;
                Receipts.Add(new SopReadReceipt
                {
                    SopVersionId = versionId,
                    UserId = uid,
                    AssignedAt = DateTime.UtcNow
                });
                added++;
            }

            if (added > 0)
            {
                await _db.SaveChangesAsync();
                await _audit.LogAsync(
                    eventType: "sop_assigned",
                    targetType: "sop_version",
                    targetKey: versionId.ToString(),
                    description: $"SOP versiyon {added} kullanıcıya atandı.");
            }
            return added;
        }

        // SOP detay açıldığında — ReadAt boşsa set, audit log.
        public async Task MarkReadAsync(int versionId, int userId)
        {
            var receipt = await Receipts
                .FirstOrDefaultAsync(r => r.SopVersionId == versionId && r.UserId == userId);
            if (receipt == null) return;                                  // atanmamış kullanıcı (admin önizleme)
            if (receipt.ReadAt != null) return;                            // zaten okundu

            receipt.ReadAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await _audit.LogAsync(
                eventType: "sop_read",
                targetType: "sop_version",
                targetKey: versionId.ToString(),
                description: $"User {userId} SOP versiyonu okudu.");
        }

        // "Okudum + onayladım" — ConfirmedAt set, audit log.
        public async Task<ServiceResult> ConfirmAsync(int versionId, int userId)
        {
            var receipt = await Receipts
                .FirstOrDefaultAsync(r => r.SopVersionId == versionId && r.UserId == userId);
            if (receipt == null) return ServiceResult.Failure("Bu prosedür size atanmamış.");
            if (receipt.ConfirmedAt != null) return ServiceResult.Ok("Zaten onaylandı.");

            var now = DateTime.UtcNow;
            if (receipt.ReadAt == null) receipt.ReadAt = now;
            receipt.ConfirmedAt = now;
            await _db.SaveChangesAsync();

            await _audit.LogAsync(
                eventType: "sop_confirmed",
                targetType: "sop_version",
                targetKey: versionId.ToString(),
                description: $"User {userId} SOP versiyonunu okudum+onayladım.");

            return ServiceResult.Ok("Okundu işaretlendi.");
        }

        // "Prosedürlerim" listesi — bekleyen (ConfirmedAt NULL) + onaylanan birlikte.
        public Task<List<SopReadReceipt>> GetMyReceiptsAsync(int userId, bool onlyPending = false)
        {
            var query = Receipts
                .Include(r => r.SopVersion)
                    .ThenInclude(v => v!.SopDocument)
                .Where(r => r.UserId == userId);
            if (onlyPending) query = query.Where(r => r.ConfirmedAt == null);
            return query
                .OrderBy(r => r.ConfirmedAt == null ? 0 : 1)              // bekleyen üstte
                .ThenByDescending(r => r.AssignedAt)
                .ToListAsync();
        }

        public Task<SopReadReceipt?> GetByVersionUserAsync(int versionId, int userId) =>
            Receipts.AsNoTracking()
                .FirstOrDefaultAsync(r => r.SopVersionId == versionId && r.UserId == userId);

        // Reminder query — deadline countdown, Faz E Hangfire job tüketecek.
        // 7 gün kala (ReminderSentCount=0) + 1 gün kala (ReminderSentCount=1) ayrımı için
        // job tarafında daysRemaining hesaplanır.
        public Task<List<SopReadReceipt>> GetPendingDueAsync(int withinDays)
        {
            var now = DateTime.UtcNow;
            return Receipts
                .Include(r => r.SopVersion)
                    .ThenInclude(v => v!.SopDocument)
                .Where(r => r.ConfirmedAt == null
                         && r.SopVersion != null
                         && r.SopVersion.SopDocument != null)
                .ToListAsync()
                .ContinueWith(t => t.Result
                    .Where(r =>
                    {
                        var deadline = r.AssignedAt.AddDays(r.SopVersion!.SopDocument!.ReadDeadlineDays);
                        var daysRemaining = (deadline - now).TotalDays;
                        return daysRemaining > 0 && daysRemaining <= withinDays;
                    })
                    .ToList());
        }
    }
}
