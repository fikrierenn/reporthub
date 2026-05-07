using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Logging;
using Mosaik.Modules.Tamim.Models;

namespace Mosaik.Modules.Tamim.Services
{
    // Plan 17 Faz C — Tamim read-only service. Yayınlama Faz D'de cron job tarafından.
    // Read tracking IAuditLog ile (EventType="tamim_okundu", target="tamim").
    public class TamimService
    {
        private readonly DbContext _db;
        private readonly IAuditLog _audit;

        public TamimService(DbContext db, IAuditLog audit)
        {
            _db = db;
            _audit = audit;
        }

        private DbSet<Models.Tamim> Tamimler => _db.Set<Models.Tamim>();
        private DbSet<GunlukBlok> Bloklar => _db.Set<GunlukBlok>();

        public async Task<List<Models.Tamim>> ListAsync(int sonNGun = 30)
        {
            var sinir = DateTime.UtcNow.Date.AddDays(-sonNGun);
            return await Tamimler.AsNoTracking()
                .Where(t => t.TamimTarihi >= sinir)
                .OrderByDescending(t => t.TamimTarihi)
                .ToListAsync();
        }

        public Task<Models.Tamim?> GetAsync(int id) =>
            Tamimler.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);

        public async Task<List<GunlukBlok>> GetBloklarAsync(int tamimId)
        {
            return await Bloklar.AsNoTracking()
                .Where(b => b.TamimId == tamimId)
                .OrderBy(b => b.Id)
                .ToListAsync();
        }

        // Detail sayfası ziyaret — audit log'a "tamim_okundu" event yaz.
        // Username AuditLogService HttpContext'ten alır.
        public async Task TrackReadAsync(int tamimId)
        {
            await _audit.LogAsync(
                eventType: "tamim_okundu",
                targetType: "tamim",
                targetKey: tamimId.ToString());
        }
    }
}
