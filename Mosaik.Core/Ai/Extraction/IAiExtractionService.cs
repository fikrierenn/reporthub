namespace Mosaik.Core.Ai.Extraction
{
    // Plan 16.5 Faz D — generic extraction orchestrator interface.
    // Modüle özgü implementasyonlar bu interface'i implemente eder.
    // Mosaik (host): AiExtractionWorker (contracts), Plan 19: DocumentExtractionService, vb.
    public interface IAiExtractionService
    {
        // Bir dosyayı (PDF veya diğer) AI ile analiz edip suggestion'lar üretir.
        // extractionId: entity-specific primary key (ContractAiExtraction.Id, vb.)
        Task<ExtractionResult> ExtractAsync(ExtractionRequest request, CancellationToken ct = default);
    }

    public sealed record ExtractionRequest(
        int ExtractionId,
        string FilePath,
        string EntityType,    // "Contract", "Document", vb.
        int EntityId,
        int FirmaId);

    public sealed record ExtractionResult(
        bool IsSuccess,
        string? Error,
        int SuggestionsCreated = 0,
        string? RawText = null);
}
