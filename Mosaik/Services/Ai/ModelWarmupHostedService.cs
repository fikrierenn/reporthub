using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mosaik.Core.AI.Embed;
using Mosaik.Core.AI.Local;

namespace Mosaik.Services.Ai
{
    // Plan 34.1 Faz 2 A-16 — Uygulama başlangıcında AI modellerini arka planda warm-up et.
    // Mosaik startup'ını BLOKLAMAZ — StartAsync hemen döner, warmup Task.Run ile arka planda.
    //
    // Etki:
    //   - LLamaWeights mmap ilk istek için hazır (3-6 sn cold start gizler)
    //   - E5Embedder ONNX session hazır (~1-2 sn)
    //   - Bir dummy inference ile native lib + KV cache prewarm
    public sealed class ModelWarmupHostedService : IHostedService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<ModelWarmupHostedService> _logger;
        private Task? _warmupTask;
        private CancellationTokenSource? _cts;

        public ModelWarmupHostedService(IServiceProvider services, ILogger<ModelWarmupHostedService> logger)
        {
            _services = services;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _warmupTask = Task.Run(() => RunAsync(_cts.Token), _cts.Token);
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_cts is not null) await _cts.CancelAsync();
            if (_warmupTask is not null)
            {
                try { await _warmupTask.WaitAsync(TimeSpan.FromSeconds(2), cancellationToken); }
                catch { /* shutdown best-effort */ }
            }
        }

        private async Task RunAsync(CancellationToken ct)
        {
            try
            {
                var embedder = _services.GetService(typeof(IMosaikEmbedder)) as IMosaikEmbedder;
                var llm = _services.GetService(typeof(ILlmRunner)) as ILlmRunner;

                if (embedder is null || llm is null)
                {
                    _logger.LogInformation("ModelWarmup: AI servisleri DI'da yok, atlandı.");
                    return;
                }

                _logger.LogInformation("ModelWarmup başladı...");
                var sw = System.Diagnostics.Stopwatch.StartNew();

                if (embedder.IsReady)
                {
                    try
                    {
                        await embedder.EmbedAsync("test", EmbedRole.Query, ct);
                        _logger.LogInformation("ModelWarmup: embedder hazır ({Ms} ms)", sw.ElapsedMilliseconds);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "ModelWarmup: embedder warmup başarısız");
                    }
                }
                else
                {
                    _logger.LogInformation("ModelWarmup: embedder hazır değil (model dosyası eksik), atlandı.");
                }

                if (ct.IsCancellationRequested) return;
                sw.Restart();

                if (llm.IsReady)
                {
                    try
                    {
                        await llm.RunAsync(
                            systemPrompt: "Kısa cevap ver.",
                            userPrompt: "Test.",
                            options: new LlmRunOptions(MaxTokens: 4, Temperature: 0.1f),
                            ct: ct);
                        _logger.LogInformation("ModelWarmup: LLM hazır ({Ms} ms)", sw.ElapsedMilliseconds);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "ModelWarmup: LLM warmup başarısız");
                    }
                }
                else
                {
                    _logger.LogInformation("ModelWarmup: LLM hazır değil (model dosyası eksik), atlandı.");
                }

                _logger.LogInformation("ModelWarmup tamamlandı.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ModelWarmup beklenmeyen hata");
            }
        }
    }
}
