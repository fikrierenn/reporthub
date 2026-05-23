namespace Mosaik.Core.AI.Local
{
    // Plan 34.1 Faz 2 A-13 — In-process LLM runner abstraction.
    // Impl Mosaik tarafında LlamaSharpRunner (LLamaSharp 0.27 + Qwen 2.5 3B Q4).
    // Model swap kararı yarın: Phi-3.5 (Microsoft.ML.OnnxRuntimeGenAI) için ayrı impl.
    public interface ILlmRunner
    {
        // Model + native lib yüklendi mi. False ise tüm RunAsync InvalidOperationException
        // fırlatır. RagAdvisor IsReady kontrol ederek graceful "AI hazır değil" döner.
        bool IsReady { get; }

        // System + user prompt → cevap. Token sayımı best-effort (LLamaSharp metadata varsa).
        Task<LlmRunResult> RunAsync(
            string systemPrompt,
            string userPrompt,
            LlmRunOptions options,
            CancellationToken ct = default);
    }

    public sealed record LlmRunOptions(
        int MaxTokens = 512,
        float Temperature = 0.3f,
        float TopP = 0.9f);

    public sealed record LlmRunResult(
        bool IsSuccess,
        string? Answer,
        int TokensIn,
        int TokensOut,
        string? Error = null);
}
