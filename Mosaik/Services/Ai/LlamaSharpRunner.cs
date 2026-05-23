using System.Text;
using LLama;
using LLama.Common;
using Microsoft.Extensions.Logging;
using Mosaik.Core.AI.Local;

namespace Mosaik.Services.Ai
{
    // Plan 34.1 Faz 2 A-13 — LLamaSharp 0.27 + Qwen 2.5 3B Q4 in-process runner.
    // Singleton — LLamaWeights mmap RAM 2.4 GB. Çoklu LLamaContext = RAM 1.5x;
    // MVP tek-worker queue (SemaphoreSlim) ile sırayla yürür.
    //
    // Qwen chat template: <|im_start|>system\n{sys}<|im_end|>\n<|im_start|>user\n{user}<|im_end|>\n<|im_start|>assistant\n
    // Anti-prompt: <|im_end|> (model'in cevap kapanışı).
    public sealed class LlamaSharpRunner : ILlmRunner, IDisposable
    {
        private const string ModelFileName = "qwen25-3b-instruct-q4_k_m.gguf";
        // Plan 34.2: Skill catalog inject için 4096 (RAM ~+600MB). Multi-skill için yeterli.
        private const int ContextSize = 4096;

        private readonly ILogger<LlamaSharpRunner> _logger;
        private readonly LLamaWeights? _weights;
        private readonly ModelParams? _modelParams;
        private readonly SemaphoreSlim _gate = new(1, 1);
        private readonly bool _ready;

        public bool IsReady => _ready;

        public LlamaSharpRunner(ILogger<LlamaSharpRunner> logger, string contentRoot)
        {
            _logger = logger;

            var modelPath = Path.Combine(contentRoot, "App_Data", "models", "llm", ModelFileName);
            if (!File.Exists(modelPath))
            {
                _logger.LogWarning(
                    "LlamaSharpRunner: model dosyası bulunamadı, runner DEVRE DIŞI. Path={Path}",
                    modelPath);
                _ready = false;
                return;
            }

            try
            {
                _modelParams = new ModelParams(modelPath)
                {
                    ContextSize = ContextSize,
                    GpuLayerCount = 0
                };
                _weights = LLamaWeights.LoadFromFile(_modelParams);
                _ready = true;
                _logger.LogInformation(
                    "LlamaSharpRunner hazır: model {ModelName}, ctx={Ctx}",
                    ModelFileName, ContextSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LlamaSharpRunner yükleme hatası, runner DEVRE DIŞI.");
                _ready = false;
            }
        }

        public async Task<LlmRunResult> RunAsync(
            string systemPrompt,
            string userPrompt,
            LlmRunOptions options,
            CancellationToken ct = default)
        {
            if (!_ready) throw new InvalidOperationException("LlamaSharpRunner hazır değil — model dosyası eksik.");

            var prompt = BuildQwenPrompt(systemPrompt, userPrompt);

            await _gate.WaitAsync(ct);
            try
            {
                var executor = new StatelessExecutor(_weights!, _modelParams!);
                var inferenceParams = new InferenceParams
                {
                    MaxTokens = options.MaxTokens,
                    AntiPrompts = new List<string> { "<|im_end|>", "<|im_start|>" }
                };

                var buffer = new StringBuilder();
                int tokenCount = 0;
                await foreach (var token in executor.InferAsync(prompt, inferenceParams, ct))
                {
                    buffer.Append(token);
                    tokenCount++;
                    if (ct.IsCancellationRequested) break;
                }

                var answer = StripAntiPrompts(buffer.ToString()).Trim();
                if (string.IsNullOrWhiteSpace(answer))
                {
                    return new LlmRunResult(false, null, 0, 0, "LLM boş cevap üretti.");
                }

                return new LlmRunResult(
                    IsSuccess: true,
                    Answer: answer,
                    TokensIn: EstimateTokens(prompt),
                    TokensOut: tokenCount);
            }
            catch (OperationCanceledException)
            {
                return new LlmRunResult(false, null, 0, 0, "İşlem iptal edildi.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LlamaSharpRunner inference hatası");
                return new LlmRunResult(false, null, 0, 0, ex.GetType().Name);
            }
            finally
            {
                _gate.Release();
            }
        }

        private static string BuildQwenPrompt(string systemPrompt, string userPrompt) =>
            $"<|im_start|>system\n{systemPrompt}<|im_end|>\n" +
            $"<|im_start|>user\n{userPrompt}<|im_end|>\n" +
            "<|im_start|>assistant\n";

        private static string StripAntiPrompts(string output)
        {
            int idx = output.IndexOf("<|im_end|>", StringComparison.Ordinal);
            if (idx >= 0) output = output[..idx];
            idx = output.IndexOf("<|im_start|>", StringComparison.Ordinal);
            if (idx >= 0) output = output[..idx];
            return output;
        }

        // Token sayım kaba tahmin (~4 char/token Türkçe). Tam sayım için LLamaTokenizer kullanılabilir.
        private static int EstimateTokens(string text) => Math.Max(1, text.Length / 4);

        public void Dispose()
        {
            _weights?.Dispose();
            _gate.Dispose();
        }
    }
}
