using Microsoft.EntityFrameworkCore;
using Mosaik.Models;

namespace Mosaik.Services
{
    public interface IDocumentPermissionService
    {
        Task<bool> CanReadAsync(int userId, int firmaId, int contractFileId, CancellationToken ct = default);
        Task<bool> CanWriteAsync(int userId, int firmaId, int contractFileId, CancellationToken ct = default);
        Task GrantAsync(int contractFileId, string subjectType, int subjectId, int firmaId, byte level, int grantedById, DateTime? validUntil, CancellationToken ct = default);
        Task RevokeAsync(int id, CancellationToken ct = default);
        Task<IReadOnlyList<DocumentPermission>> GetForFileAsync(int contractFileId, CancellationToken ct = default);

        // Plan 57/M-B D2 — liste görünümü için batch: verilen dosyalardan kullanıcının OKUYABİLDİKLERİ.
        // Kuralsız dosya = serbest (açık varsayılan); kurallı dosya = user/role eşleşmesi şart.
        Task<IReadOnlyList<int>> FilterReadableIdsAsync(int userId, IReadOnlyList<int> firmaIds, IReadOnlyList<int> fileIds, CancellationToken ct = default);
    }

    public class DocumentPermissionService(MosaikContext db, ILogger<DocumentPermissionService> logger)
        : IDocumentPermissionService
    {
        // Admin bypass CONTROLLER'da (User.IsInRole — bu servis rol adı bilmez, comment-rot fix
        // 2026-07-05). İzin kaydı olmayan dosya = herkese açık (varsayılan); kayıt varsa eşleşme şart.
        public async Task<bool> CanReadAsync(int userId, int firmaId, int contractFileId, CancellationToken ct = default)
            => await HasLevelAsync(userId, firmaId, contractFileId, 1, ct);

        public async Task<bool> CanWriteAsync(int userId, int firmaId, int contractFileId, CancellationToken ct = default)
            => await HasLevelAsync(userId, firmaId, contractFileId, 2, ct);

        private async Task<bool> HasLevelAsync(int userId, int firmaId, int contractFileId, byte minLevel, CancellationToken ct)
        {
            var now = DateTime.UtcNow;

            // İzin kaydı yoksa → erişim serbest (açık varsayılan)
            var anyRule = await db.DocumentPermissions
                .AsNoTracking()
                .AnyAsync(p => p.ContractFileId == contractFileId && p.FirmaId == firmaId, ct);

            if (!anyRule) return true;

            // İzin kaydı varsa — kullanıcı doğrudan veya bir rol üzerinden izinli mi?
            var userRoleIds = await db.UserRoles
                .AsNoTracking()
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.RoleId)
                .ToListAsync(ct);

            return await db.DocumentPermissions
                .AsNoTracking()
                .Where(p => p.ContractFileId == contractFileId
                         && p.FirmaId == firmaId
                         && p.Level >= minLevel
                         && (p.ValidUntil == null || p.ValidUntil > now)
                         && (
                             (p.SubjectType == "user" && p.SubjectId == userId) ||
                             (p.SubjectType == "role" && userRoleIds.Contains(p.SubjectId))
                         ))
                .AnyAsync(ct);
        }

        public async Task GrantAsync(int contractFileId, string subjectType, int subjectId, int firmaId,
            byte level, int grantedById, DateTime? validUntil, CancellationToken ct = default)
        {
            var perm = new DocumentPermission
            {
                ContractFileId = contractFileId,
                SubjectType    = subjectType,
                SubjectId      = subjectId,
                FirmaId        = firmaId,
                Level          = level,
                GrantedAt      = DateTime.UtcNow,
                GrantedById    = grantedById,
                ValidUntil     = validUntil
            };
            db.DocumentPermissions.Add(perm);
            await db.SaveChangesAsync(ct);
            logger.LogInformation("DocPerm granted: file={FileId} subject={Type}:{Id} level={Level}", contractFileId, subjectType, subjectId, level);
        }

        public async Task RevokeAsync(int id, CancellationToken ct = default)
        {
            var perm = await db.DocumentPermissions.FindAsync([id], ct);
            if (perm is null) return;
            db.DocumentPermissions.Remove(perm);
            await db.SaveChangesAsync(ct);
            logger.LogInformation("DocPerm revoked: id={Id}", id);
        }

        public async Task<IReadOnlyList<int>> FilterReadableIdsAsync(
            int userId, IReadOnlyList<int> firmaIds, IReadOnlyList<int> fileIds, CancellationToken ct = default)
        {
            if (fileIds.Count == 0) return [];
            var now = DateTime.UtcNow;

            // Tek sorgu: listelenen dosyalara ait tüm kurallar (N+1 yok — Index 200 satır cap'li).
            var rules = await db.DocumentPermissions.AsNoTracking()
                .Where(p => p.ContractFileId != null
                         && fileIds.Contains(p.ContractFileId.Value)
                         && firmaIds.Contains(p.FirmaId))
                .Select(p => new { FileId = p.ContractFileId!.Value, p.SubjectType, p.SubjectId, p.Level, p.ValidUntil })
                .ToListAsync(ct);

            if (rules.Count == 0) return fileIds; // hiç kural yok — hepsi serbest

            var userRoleIds = await db.UserRoles.AsNoTracking()
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.RoleId)
                .ToListAsync(ct);

            var restricted = rules.GroupBy(r => r.FileId).ToDictionary(g => g.Key, g => g.ToList());
            var result = new List<int>(fileIds.Count);
            foreach (var id in fileIds)
            {
                if (!restricted.TryGetValue(id, out var fileRules)) { result.Add(id); continue; } // kuralsız = serbest
                var allowed = fileRules.Any(r => r.Level >= 1
                    && (r.ValidUntil == null || r.ValidUntil > now)
                    && ((r.SubjectType == "user" && r.SubjectId == userId)
                     || (r.SubjectType == "role" && userRoleIds.Contains(r.SubjectId))));
                if (allowed) result.Add(id);
            }
            return result;
        }

        public Task<IReadOnlyList<DocumentPermission>> GetForFileAsync(int contractFileId, CancellationToken ct = default)
            => db.DocumentPermissions
                .AsNoTracking()
                .Where(p => p.ContractFileId == contractFileId)
                .OrderBy(p => p.SubjectType)
                .ThenBy(p => p.SubjectId)
                .ToListAsync(ct)
                .ContinueWith(t => (IReadOnlyList<DocumentPermission>)t.Result, ct);
    }
}
