using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Ai;
using Mosaik.Models;

namespace Mosaik.Services.Ai
{
    // Plan 25 — ADR-012 Karar 2: IHostedService + Channel<int>, Hangfire yok.
    // AiPipelineQueue'dan ContractAiExtraction ID'si alır, sırayla işler.
    // Retry: 3 deneme, exponential backoff (2s → 8s → 32s).
    public sealed class AiExtractionWorker : BackgroundService
    {
        private readonly AiPipelineQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IPdfTextExtractor _pdfExtractor;
        private readonly ILogger<AiExtractionWorker> _logger;

        public AiExtractionWorker(
            AiPipelineQueue queue,
            IServiceScopeFactory scopeFactory,
            IPdfTextExtractor pdfExtractor,
            ILogger<AiExtractionWorker> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _pdfExtractor = pdfExtractor;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AiExtractionWorker başladı.");
            await foreach (var extractionId in _queue.ReadAllAsync(stoppingToken))
            {
                await ProcessWithRetryAsync(extractionId, stoppingToken);
            }
            _logger.LogInformation("AiExtractionWorker durdu.");
        }

        private async Task ProcessWithRetryAsync(int extractionId, CancellationToken ct)
        {
            const int maxAttempts = 3;
            var delay = TimeSpan.FromSeconds(2);

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    await ProcessAsync(extractionId, ct);
                    return;
                }
                catch (Exception ex) when (attempt < maxAttempts)
                {
                    _logger.LogWarning(ex,
                        "AI extraction başarısız (deneme {Attempt}/{Max}). ExtractionId={Id}. {Delay}s bekliyor.",
                        attempt, maxAttempts, extractionId, delay.TotalSeconds);
                    await Task.Delay(delay, ct);
                    delay *= 4; // exponential: 2s → 8s → 32s
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "AI extraction kalıcı hata (3 deneme tükendi). ExtractionId={Id}.", extractionId);
                    await MarkFailedAsync(extractionId, "Beklenmedik hata oluştu. Detaylar log'da.");
                }
            }
        }

        private async Task ProcessAsync(int extractionId, CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<Mosaik.Models.MosaikContext>();
            var ai = scope.ServiceProvider.GetRequiredService<IAiSummaryProvider>();

            var extraction = await db.ContractAiExtractions
                .Include(e => e.ContractFile)
                .Include(e => e.Contract)
                .FirstOrDefaultAsync(e => e.Id == extractionId, ct)
                ?? throw new InvalidOperationException($"ContractAiExtraction bulunamadı: {extractionId}");

            if (extraction.ContractFile is null)
            {
                await MarkFailedAsync(extractionId, "Sözleşme dosyası bulunamadı.");
                return;
            }

            if (extraction.Status == ExtractionStatus.Approved)
            {
                _logger.LogWarning("Zaten onaylı extraction, atlanıyor. Id={Id}", extractionId);
                return;
            }

            // RawText yoksa PdfPig ile dosyadan çek (Faz D)
            var rawText = extraction.RawText;
            if (string.IsNullOrWhiteSpace(rawText))
            {
                UpdateProgress(db, extraction, "ocr");
                await db.SaveChangesAsync(ct);

                rawText = await _pdfExtractor.ExtractAsync(extraction.ContractFile.FilePath, ct);
                if (string.IsNullOrWhiteSpace(rawText))
                {
                    await MarkFailedAsync(extractionId, "PDF'ten metin çıkarılamadı. Dosya taranmış görüntü olabilir.");
                    return;
                }

                extraction.RawText = rawText;
                db.Entry(extraction).Property(x => x.RawText).IsModified = true;
                await db.SaveChangesAsync(ct);
            }

            // Adım 1 — Aşama 1 AI çağrısı
            UpdateProgress(db, extraction, "ai_stage1");
            await db.SaveChangesAsync(ct);

            var title = extraction.Contract?.Title;
            var stage1Result = await ai.GenerateAsync(new AiRequest(
                SystemPrompt: ExtractionPrompts.Stage1SystemPrompt,
                UserPrompt: ExtractionPrompts.BuildUserPrompt(rawText, title),
                RequireJson: true,
                Purpose: "contract_extraction_stage1"
            ), ct);

            if (!stage1Result.IsSuccess)
                throw new InvalidOperationException($"AI Stage1 başarısız: {stage1Result.Error}");

            // Aşama 1 sonucundan kategori belirle → Aşama 2 prompt seç
            string? detectedCategory = null;
            try
            {
                using var doc1 = JsonDocument.Parse(stage1Result.RawJson ?? "{}");
                if (doc1.RootElement.TryGetProperty("contractCategory", out var cat))
                    detectedCategory = cat.GetString();
            }
            catch (JsonException jex)
            {
                _logger.LogWarning(jex,
                    "Stage1 kategori parse edilemedi; Stage2 'Other' prompt kullanılıyor. ExtractionId={Id}", extractionId);
            }

            // Adım 2 — Aşama 2 AI çağrısı (kategori-spesifik derinleştirme)
            UpdateProgress(db, extraction, "ai_stage2");
            await db.SaveChangesAsync(ct);

            var stage2Prompt = ExtractionPrompts.GetStage2SystemPrompt(detectedCategory ?? "Other");
            var stage2UserPrompt = $"""
                Stage 1 çıktısı:
                {stage1Result.RawJson}

                Orijinal sözleşme metni:
                {rawText}
                """;

            var stage2Result = await ai.GenerateAsync(new AiRequest(
                SystemPrompt: stage2Prompt,
                UserPrompt: stage2UserPrompt,
                RequireJson: false,
                Purpose: "contract_extraction_stage2"
            ), ct);

            if (!stage2Result.IsSuccess)
            {
                _logger.LogWarning(
                    "AI Stage2 başarısız, Stage1 sonuçlarıyla devam ediliyor. ExtractionId={Id}, Hata={Error}",
                    extractionId, stage2Result.Error);
            }

            // Adım 3 — Sonuçları kaydet
            extraction.Status = ExtractionStatus.AwaitingReview;
            extraction.ExtractionResultJson = stage1Result.RawJson;   // Stage1 yapısal JSON
            extraction.PromptVersion = ExtractionPrompts.PromptVersion;
            extraction.ModelUsed = stage1Result.ModelUsed ?? "";
            extraction.InputTokens = stage1Result.InputTokens + stage2Result.InputTokens;
            extraction.OutputTokens = stage1Result.OutputTokens + stage2Result.OutputTokens;
            extraction.ProcessedAt = DateTime.UtcNow;
            extraction.ProgressStep = "done";
            extraction.ErrorMessage = null;

            await db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "AI extraction tamamlandı. Id={Id}, Model={Model}, Tokens={In}+{Out}",
                extractionId, extraction.ModelUsed, extraction.InputTokens, extraction.OutputTokens);
        }

        private static void UpdateProgress(Mosaik.Models.MosaikContext db, ContractAiExtraction e, string step)
        {
            e.ProgressStep = step;
            db.Entry(e).Property(x => x.ProgressStep).IsModified = true;
        }

        private async Task MarkFailedAsync(int extractionId, string error)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<Mosaik.Models.MosaikContext>();
            var extraction = await db.ContractAiExtractions.FindAsync(extractionId);
            if (extraction is null) return;

            extraction.Status = ExtractionStatus.Failed;
            extraction.ErrorMessage = error.Length > 1000 ? error[..1000] : error;
            extraction.ProgressStep = "failed";
            await db.SaveChangesAsync();
        }
    }
}
