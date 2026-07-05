using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Domain;
using Mosaik.Core.Workflow;
using Mosaik.Models;

namespace Mosaik.Services.Workflow
{
    // Plan 57 Part C — org-omurga amir çözümleme (danışman karar tablosu 2026-07-05).
    // Zincir: User.Personelno → pozisyon bul: (i) HolderPersonelno doğrudan ÖNCE (resmi sahip),
    // (ii) GorevPersonelMap→GorevDocument→OrgPositionId fallback (frontline/gap kişisi) →
    // ParentPositionId yukarı-yürüyüş (visited + max-derinlik + kendini-amir-sayma guard,
    // OrgChartService cursor-loop emsali) → holder dolu ilk ata → Users.Personelno → UserId.
    public class ManagerResolverService(MosaikContext db, ILogger<ManagerResolverService> logger) : IManagerResolver
    {
        public async Task<int?> ResolveManagerUserIdAsync(int submitterUserId, CancellationToken ct = default)
        {
            var personelno = await db.Users.AsNoTracking()
                .Where(u => u.UserId == submitterUserId)
                .Select(u => u.Personelno)
                .FirstOrDefaultAsync(ct);
            if (string.IsNullOrWhiteSpace(personelno))
            {
                logger.LogInformation("ManagerResolver: user {UserId} Personelno boş — çözüm yok.", submitterUserId);
                return null;
            }
            personelno = personelno.Trim();

            var positions = await db.Set<OrgPosition>().AsNoTracking()
                .Where(p => p.IsActive)
                .Select(p => new PositionNode(p.Id, p.ParentPositionId, p.HolderPersonelno))
                .ToListAsync(ct);

            // (ii) fallback girişi: kişi-eşleme → görev dokümanı → omurga pozisyonu.
            var mappedPositionId = await (
                from m in db.Set<Mosaik.Modules.GorevTanimlari.Entities.GorevPersonelMap>().AsNoTracking()
                join d in db.Set<Mosaik.Modules.GorevTanimlari.Entities.GorevDocument>().AsNoTracking()
                    on m.GorevDocumentId equals d.Id
                where m.Personelno == personelno && d.OrgPositionId != null
                select d.OrgPositionId).FirstOrDefaultAsync(ct);

            var managerPersonelno = ManagerChainResolver.Resolve(positions, personelno, mappedPositionId);
            if (managerPersonelno is null)
            {
                logger.LogInformation("ManagerResolver: user {UserId} ({Personelno}) için amir çözülemedi (pozisyon/holder zinciri boş).", submitterUserId, personelno);
                return null;
            }

            var managerUserId = await db.Users.AsNoTracking()
                .Where(u => u.IsActive && u.Personelno == managerPersonelno)
                .Select(u => (int?)u.UserId)
                .FirstOrDefaultAsync(ct);
            if (managerUserId is null)
                logger.LogInformation("ManagerResolver: user {UserId} için amir {ManagerPersonelno} bulundu ama Mosaik user karşılığı yok.", submitterUserId, managerPersonelno);
            return managerUserId;
        }
    }

    public sealed record PositionNode(int Id, int? ParentPositionId, string? HolderPersonelno);

    // Saf zincir çözücü — DB'siz, unit-test edilebilir (OrgChartService saf-fonksiyon emsali).
    public static class ManagerChainResolver
    {
        private const int MaxDepth = 8; // GM + 9 müdürlük ağacı sığ; kopuk/bozuk veri guard'ı

        // Dönen: amirin Personelno'su (submitter'ınkinden farklı, holder'ı dolu ilk ata) veya null.
        public static string? Resolve(IReadOnlyList<PositionNode> positions, string submitterPersonelno, int? mappedPositionId)
        {
            var byId = positions.ToDictionary(p => p.Id);

            // Giriş noktası: (i) resmi holder olduğum pozisyon ÖNCE, (ii) kişi-eşleme pozisyonu.
            var start = positions.FirstOrDefault(p => Same(p.HolderPersonelno, submitterPersonelno));
            if (start is null && mappedPositionId is int mid)
                byId.TryGetValue(mid, out start);
            if (start is null)
                return null;

            var visited = new HashSet<int> { start.Id };
            var cursor = start;
            for (var depth = 0; depth < MaxDepth; depth++)
            {
                if (cursor.ParentPositionId is not int parentId || !byId.TryGetValue(parentId, out var parent))
                    return null; // kök (GM) geçildi / kopuk ağaç — çözülemedi
                if (!visited.Add(parent.Id))
                    return null; // cycle guard

                // Holder dolu VE submitter'ın kendisi değil → amir bulundu.
                if (!string.IsNullOrWhiteSpace(parent.HolderPersonelno)
                    && !Same(parent.HolderPersonelno, submitterPersonelno))
                    return parent.HolderPersonelno!.Trim();

                cursor = parent; // boş/kendisi → bir üst kademe
            }
            return null;
        }

        private static bool Same(string? a, string? b) =>
            !string.IsNullOrWhiteSpace(a) && !string.IsNullOrWhiteSpace(b)
            && string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
