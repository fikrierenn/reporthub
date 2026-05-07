using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Logging;
using Mosaik.Modules.Circular.Models;

namespace Mosaik.Modules.Circular.Services
{
    // Plan 17 Faz C — Circular read-only service. Yayınlama Faz D'de cron job tarafından.
    // Read tracking IAuditLog ile (EventType="circular_read", target="circular").
    public class CircularService
    {
        private readonly DbContext _db;
        private readonly IAuditLog _audit;

        public CircularService(DbContext db, IAuditLog audit)
        {
            _db = db;
            _audit = audit;
        }

        private DbSet<Models.Circular> Circulars => _db.Set<Models.Circular>();
        private DbSet<DailyBlock> Blocks => _db.Set<DailyBlock>();

        public async Task<List<Models.Circular>> ListAsync(int sonNGun = 30)
        {
            var sinir = DateTime.UtcNow.Date.AddDays(-sonNGun);
            return await Circulars.AsNoTracking()
                .Where(t => t.CircularDate >= sinir)
                .OrderByDescending(t => t.CircularDate)
                .ToListAsync();
        }

        public Task<Models.Circular?> GetAsync(int id) =>
            Circulars.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);

        // Bugünün tamimini getir — Dashboard widget için.
        // Yoksa null (henüz 17:00 olmadı / hiç blok yoktu / cron çalışmadı).
        public Task<Models.Circular?> GetTodaysAsync()
        {
            var bugun = DateTime.UtcNow.Date;
            return Circulars.AsNoTracking()
                .Where(t => t.CircularDate == bugun)
                .OrderByDescending(t => t.PublishedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<List<DailyBlock>> GetBlocksAsync(int tamimId)
        {
            return await Blocks.AsNoTracking()
                .Where(b => b.CircularId == tamimId)
                .OrderBy(b => b.Id)
                .ToListAsync();
        }

        // Detail sayfası ziyaret — audit log'a "circular_read" event yaz.
        // Username AuditLogService HttpContext'ten alır.
        public async Task TrackReadAsync(int tamimId)
        {
            await _audit.LogAsync(
                eventType: "circular_read",
                targetType: "circular",
                targetKey: tamimId.ToString());
        }
    }
}
