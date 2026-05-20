using Mosaik.Core.Domain;

namespace Mosaik.Core.Intelligence
{
    // Plan 38 — Living Org Map ilişki katmanı.
    // Polymorphic (SourceType+SourceId → TargetType+TargetId). FK yok, validation servis katmanında.
    // Çift-yazma kuralı (Plan 38 §8.1): yeni feature hem eski FK hem buraya yazar.
    public interface IEntityRelationService
    {
        Task<ServiceResult<int>> AddAsync(EntityRelationInput input, CancellationToken ct = default);

        Task<ServiceResult> RemoveAsync(int id, CancellationToken ct = default);

        Task<IReadOnlyList<EntityRelationDto>> GetBySourceAsync(
            string sourceType, int sourceId, string? relationType = null, CancellationToken ct = default);

        Task<IReadOnlyList<EntityRelationDto>> GetByTargetAsync(
            string targetType, int targetId, string? relationType = null, CancellationToken ct = default);
    }

    // Add input — idempotent: aynı (FirmaId, Source, Target, Relation) varsa update ValidFrom + return existing Id.
    public sealed record EntityRelationInput(
        int FirmaId,
        string SourceType,
        int SourceId,
        string RelationType,
        string TargetType,
        int TargetId,
        decimal? Weight = null,
        DateTime? ValidFrom = null,
        DateTime? ValidTo = null,
        string? SourceSystem = null,
        int? CreatedBy = null);

    public sealed record EntityRelationDto(
        int Id,
        int FirmaId,
        string SourceType,
        int SourceId,
        string RelationType,
        string TargetType,
        int TargetId,
        decimal? Weight,
        DateTime ValidFrom,
        DateTime? ValidTo,
        string? SourceSystem,
        DateTime CreatedAt);
}
