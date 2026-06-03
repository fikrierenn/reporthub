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
    }

    public class DocumentPermissionService(MosaikContext db, ILogger<DocumentPermissionService> logger)
        : IDocumentPermissionService
    {
        // Admin ve yönetici rolleri her zaman tam erişim alır — izin tablosu yalnızca
        // kısıtlı dosyalar için gerekli. Tablo boşsa = herkese açık (varsayılan izin).
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
