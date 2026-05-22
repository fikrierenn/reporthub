using Mosaik.Core.Ai;

namespace Mosaik.Services.Ai
{
    // Plan 16.5 Faz C — ILlmService implementasyonu.
    // IAiSummaryProvider üzerinden proxy — fallback zinciri AiSummaryProvider'da yaşar.
    // D-02-4 (2026-05-22): FallbackLlmService → LlmServiceAdapter rename.
    public sealed class LlmServiceAdapter : ILlmService
    {
        private readonly IAiSummaryProvider _ai;

        public LlmServiceAdapter(IAiSummaryProvider ai) => _ai = ai;

        public async Task<LlmResponse> GenerateAsync(LlmRequest request, CancellationToken ct = default)
        {
            var aiResult = await _ai.GenerateAsync(new AiRequest(
                SystemPrompt: request.SystemPrompt,
                UserPrompt: request.UserPrompt,
                RequireJson: request.RequireJson,
                Purpose: request.Purpose), ct);

            return new LlmResponse(
                IsSuccess: aiResult.IsSuccess,
                Content: aiResult.RawJson,
                Error: aiResult.Error,
                InputTokens: aiResult.InputTokens,
                OutputTokens: aiResult.OutputTokens,
                ModelUsed: aiResult.ModelUsed);
        }
    }
}
