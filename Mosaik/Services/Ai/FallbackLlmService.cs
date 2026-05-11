using Mosaik.Core.Ai;

namespace Mosaik.Services.Ai
{
    // Plan 16.5 Faz C — ILlmService implementasyonu.
    // IAiSettingsProvider'dan aktif config'leri sırayla okur, başarı gelene kadar dener.
    // AiSummaryProvider ile aynı fallback mantığını paylaşır; bu sınıf daha düşük seviyeli
    // (extraction pipeline, Faz D).
    public sealed class FallbackLlmService : ILlmService
    {
        private readonly IAiSummaryProvider _ai;

        public FallbackLlmService(IAiSummaryProvider ai) => _ai = ai;

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
