using Mosaik.Core.Domain;

namespace Mosaik.Core.Intelligence
{
    // Plan 38 — Decision Memory.
    // AuditLog "ne oldu" der; DecisionLog "neden + alternatifler + sonuç" der.
    // Plan 36 W-13 (Approve/Reject) ilk yazıcı. Plan 34 SOP onayı, Plan 35 Comment de yazıcı olur.
    public interface IDecisionLogService
    {
        Task<ServiceResult<int>> LogAsync(DecisionLogEntry entry, CancellationToken ct = default);

        Task<IReadOnlyList<DecisionLogDto>> GetByEntityAsync(
            string relatedEntityType, int relatedEntityId, CancellationToken ct = default);
    }

    public sealed record DecisionLogEntry(
        int FirmaId,
        string Title,
        int MadeBy,
        string? Rationale = null,
        string? AlternativesJson = null,
        string? ExpectedOutcome = null,
        string? KpiImpactJson = null,
        string? RelatedEntityType = null,
        int? RelatedEntityId = null,
        long? RelatedAuditId = null,
        DateTime? MadeAt = null);

    public sealed record DecisionLogDto(
        int Id,
        int FirmaId,
        string Title,
        string? Rationale,
        int MadeBy,
        DateTime MadeAt,
        string? AlternativesJson,
        string? ExpectedOutcome,
        string? ActualOutcome,
        string? KpiImpactJson,
        string? RelatedEntityType,
        int? RelatedEntityId,
        long? RelatedAuditId,
        string Status);
}
