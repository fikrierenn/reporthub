namespace Mosaik.Core.Ai.Suggestion
{
    // Plan 16.5 Faz D — generic suggestion-approval pattern.
    // Modüle özgü implementasyonlar (sözleşme, doküman, vb.) bu interface'i implemente eder.
    // Pattern: AI hiçbir zaman direkt yazmaz — her zaman AiSuggestion → review → approve.
    public interface IAiSuggestionService<TSuggestion>
    {
        Task<IReadOnlyList<TSuggestion>> GetPendingAsync(int firmaId, CancellationToken ct = default);
        Task<ApprovalOutcome> ApproveAsync(int suggestionId, string approvedBy, CancellationToken ct = default);
        Task<ApprovalOutcome> RejectAsync(int suggestionId, string rejectedBy, string? reason, CancellationToken ct = default);
    }

    public enum ApprovalOutcome { Ok, NotFound, AlreadyProcessed, Error }
}
