using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Domain;
using Mosaik.Core.Intelligence;
using Mosaik.Models;
using Mosaik.Models.Intelligence;

namespace Mosaik.Services.Intelligence
{
    // Plan 38 — Living Org Map polymorphic ilişki servisi.
    // Idempotent: aynı tuple (Firma + Source + Relation + Target) tekrar gelirse ValidFrom yenilenir, yeni satır yok.
    public class EntityRelationService : IEntityRelationService
    {
        private readonly MosaikContext _context;
        private readonly ILogger<EntityRelationService> _logger;

        public EntityRelationService(MosaikContext context, ILogger<EntityRelationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ServiceResult<int>> AddAsync(EntityRelationInput input, CancellationToken ct = default)
        {
            if (!EntityType.IsValid(input.SourceType))
                return ServiceResult<int>.Failure($"Geçersiz SourceType: {input.SourceType}", "INVALID_SOURCE_TYPE");
            if (!EntityType.IsValid(input.TargetType))
                return ServiceResult<int>.Failure($"Geçersiz TargetType: {input.TargetType}", "INVALID_TARGET_TYPE");
            if (!RelationType.IsValid(input.RelationType))
                return ServiceResult<int>.Failure($"Geçersiz RelationType: {input.RelationType}", "INVALID_RELATION_TYPE");
            if (input.FirmaId <= 0 || input.SourceId <= 0 || input.TargetId <= 0)
                return ServiceResult<int>.Failure("FirmaId, SourceId, TargetId pozitif olmalı.", "INVALID_ID");

            var existing = await _context.EntityRelations
                .FirstOrDefaultAsync(r =>
                    r.FirmaId == input.FirmaId &&
                    r.SourceType == input.SourceType &&
                    r.SourceId == input.SourceId &&
                    r.RelationType == input.RelationType &&
                    r.TargetType == input.TargetType &&
                    r.TargetId == input.TargetId, ct);

            if (existing is not null)
            {
                existing.ValidFrom = input.ValidFrom ?? DateTime.UtcNow;
                existing.ValidTo = input.ValidTo;
                existing.Weight = input.Weight ?? existing.Weight;
                await _context.SaveChangesAsync(ct);
                return ServiceResult<int>.Ok(existing.Id, "Mevcut ilişki güncellendi.");
            }

            var entity = new EntityRelation
            {
                FirmaId = input.FirmaId,
                SourceType = input.SourceType,
                SourceId = input.SourceId,
                RelationType = input.RelationType,
                TargetType = input.TargetType,
                TargetId = input.TargetId,
                Weight = input.Weight,
                ValidFrom = input.ValidFrom ?? DateTime.UtcNow,
                ValidTo = input.ValidTo,
                SourceSystem = input.SourceSystem,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = input.CreatedBy
            };
            _context.EntityRelations.Add(entity);
            await _context.SaveChangesAsync(ct);
            return ServiceResult<int>.Ok(entity.Id, "İlişki eklendi.");
        }

        public async Task<ServiceResult> RemoveAsync(int id, CancellationToken ct = default)
        {
            var entity = await _context.EntityRelations.FirstOrDefaultAsync(r => r.Id == id, ct);
            if (entity is null) return ServiceResult.Failure("İlişki bulunamadı.", "NOT_FOUND");
            _context.EntityRelations.Remove(entity);
            await _context.SaveChangesAsync(ct);
            return ServiceResult.Ok("İlişki silindi.");
        }

        public async Task<IReadOnlyList<EntityRelationDto>> GetBySourceAsync(
            string sourceType, int sourceId, string? relationType = null, CancellationToken ct = default)
        {
            var query = _context.EntityRelations.AsNoTracking()
                .Where(r => r.SourceType == sourceType && r.SourceId == sourceId);
            if (!string.IsNullOrEmpty(relationType))
                query = query.Where(r => r.RelationType == relationType);
            return await query.Select(r => Map(r)).ToListAsync(ct);
        }

        public async Task<IReadOnlyList<EntityRelationDto>> GetByTargetAsync(
            string targetType, int targetId, string? relationType = null, CancellationToken ct = default)
        {
            var query = _context.EntityRelations.AsNoTracking()
                .Where(r => r.TargetType == targetType && r.TargetId == targetId);
            if (!string.IsNullOrEmpty(relationType))
                query = query.Where(r => r.RelationType == relationType);
            return await query.Select(r => Map(r)).ToListAsync(ct);
        }

        private static EntityRelationDto Map(EntityRelation r) => new(
            r.Id, r.FirmaId, r.SourceType, r.SourceId, r.RelationType,
            r.TargetType, r.TargetId, r.Weight, r.ValidFrom, r.ValidTo,
            r.SourceSystem, r.CreatedAt);
    }
}
