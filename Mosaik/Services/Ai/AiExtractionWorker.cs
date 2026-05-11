using System.Runtime.Versioning;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
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
        private readonly TesseractOcrExtractor _tesseract;
        private readonly PageImportanceScorer _scorer;
        private readonly ILogger<AiExtractionWorker> _logger;

        // Plan 27 Faz A — bir sayfanın Tesseract conf'u bu eşiğin altındaysa vision'a düşer.
        // Tesseract `MeanConfidence` 0-1 arası (1.0 = mükemmel).
        private const float LowConfidenceThreshold = 0.60f;
        // Vision fallback bir worker döngüsünde max bu kadar sayfaya çağrılır (token guard).
        private const int MaxVisionFallbackPages = 5;

        public AiExtractionWorker(
            AiPipelineQueue queue,
            IServiceScopeFactory scopeFactory,
            IPdfTextExtractor pdfExtractor,
            TesseractOcrExtractor tesseract,
            PageImportanceScorer scorer,
            ILogger<AiExtractionWorker> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _pdfExtractor = pdfExtractor;
            _tesseract = tesseract;
            _scorer = scorer;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AiExtractionWorker başladı.");

            // Plan 27 Faz A — startup recovery: önceki run'da Processing olarak kalmış
            // ama Channel queue'da olmayan extraction'ları geri yükle. Bunlar app restart
            // sırasında yarım kalmış oluyor (Channel<int> in-memory, restart'ta sıfırlanıyor).
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<Mosaik.Models.MosaikContext>();
                var orphans = await db.ContractAiExtractions
                    .Where(e => e.Status == ExtractionStatus.Processing)
                    .Select(e => e.Id)
                    .ToListAsync(stoppingToken);
                foreach (var id in orphans)
                {
                    await _queue.EnqueueAsync(id);
                    _logger.LogInformation("Startup recovery: orphan extraction {Id} queue'ya geri yüklendi.", id);
                }
                if (orphans.Count > 0)
                    _logger.LogInformation("Startup recovery tamam: {Count} extraction yeniden kuyruğa alındı.", orphans.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Startup recovery başarısız — manuel Retry gerekebilir.");
            }

            await foreach (var extractionId in _queue.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await ProcessWithRetryAsync(extractionId, stoppingToken);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogCritical(ex,
                        "ProcessWithRetryAsync beklenmedik hata — worker devam ediyor. ExtractionId={Id}", extractionId);
                }
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
                    await MarkFailedAsync(extractionId, $"Beklenmedik hata: {ex.GetType().Name}. Detay logda.");
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

            // RawText yoksa metin çıkarma zinciri: PdfPig → Tesseract OCR → z.ai vision
            var rawText = extraction.RawText;
            if (string.IsNullOrWhiteSpace(rawText))
            {
                UpdateProgress(db, extraction, "ocr");
                await db.SaveChangesAsync(ct);

                // 1) PdfPig — text-based PDF
                rawText = await _pdfExtractor.ExtractAsync(extraction.ContractFile.FilePath, ct);
                _logger.LogInformation("PdfPig sonuç: {Len} karakter. ExtractionId={Id}", rawText?.Length ?? 0, extractionId);

                string? visionDiag = null;

                // 2) Tesseract — taranmış PDF: per-page + Otsu + confidence (Plan 27 Faz A).
                //    Düşük güvenli sayfalar (LowConfidenceThreshold) için per-page vision fallback.
                if (string.IsNullOrWhiteSpace(rawText))
                {
                    _logger.LogInformation("PdfPig boş döndü, Tesseract OCR deneniyor. ExtractionId={Id}", extractionId);
                    var ocrPages = _tesseract.ExtractPages(extraction.ContractFile.FilePath, ct);
                    _logger.LogInformation(
                        "Tesseract: {Count} sayfa işlendi, ort. conf={Conf:F2}. ExtractionId={Id}",
                        ocrPages.Count, ocrPages.Count > 0 ? ocrPages.Average(p => p.MeanConfidence) : 0, extractionId);

                    // 3) Per-page vision rescue — düşük conf sayfaları için vision'a sor.
                    if (ocrPages.Count > 0)
                    {
                        var lowConfPages = ocrPages
                            .Where(p => p.MeanConfidence < LowConfidenceThreshold || p.CharCount < 100)
                            .OrderBy(p => p.MeanConfidence)
                            .Take(MaxVisionFallbackPages)
                            .Select(p => p.PageIndex)
                            .ToHashSet();

                        if (lowConfPages.Count > 0)
                        {
                            _logger.LogInformation(
                                "Düşük conf sayfalar vision'a gönderiliyor: [{Pages}]. ExtractionId={Id}",
                                string.Join(",", lowConfPages.Select(i => i + 1)), extractionId);
                            var rescued = await TryRescueLowConfPagesAsync(
                                extraction.ContractFile.FilePath, lowConfPages, ct);
                            if (rescued.diag is not null) visionDiag = rescued.diag;

                            // Vision sonucu olan sayfaları Tesseract sonuçlarıyla değiştir.
                            var merged = ocrPages.Select(p =>
                                rescued.byPage.TryGetValue(p.PageIndex, out var visionText) && !string.IsNullOrWhiteSpace(visionText)
                                    ? p with { Text = visionText, CharCount = visionText.Length, MeanConfidence = 0.95f }
                                    : p
                            ).ToList();
                            ocrPages = merged;
                        }

                        // Plan 27 Faz A — PageImportanceScorer ile kritik sayfa seçimi.
                        // 22 sayfalık sözleşmenin tamamını (64K char) AI'ya göndermek 15+ dk
                        // sürüyor. Skor + ilk/son sayfa garantisi ile ~12 sayfaya kırp.
                        var pageScores = ocrPages
                            .Select(p => _scorer.Score(p.PageIndex, p.Text))
                            .ToList();
                        var criticalIndices = _scorer.SelectCriticalPages(pageScores, targetCount: 12).ToHashSet();
                        var selectedPages = ocrPages
                            .Where(p => criticalIndices.Contains(p.PageIndex) && !string.IsNullOrWhiteSpace(p.Text))
                            .OrderBy(p => p.PageIndex)
                            .ToList();

                        // Sayfa numarası işaretli concat — AI "§5.1 sayfa 13'te" konum tutar.
                        rawText = string.Join("\n\n",
                            selectedPages.Select(p => $"[Sayfa {p.PageIndex + 1}]\n{p.Text}"));

                        _logger.LogInformation(
                            "AI'ya gönderilecek metin: {Selected}/{Total} kritik sayfa, {Chars} karakter (orijinal {OrigChars}). ExtractionId={Id}",
                            selectedPages.Count, ocrPages.Count, rawText.Length,
                            ocrPages.Sum(p => p.CharCount), extractionId);
                    }
                    _logger.LogInformation("Tesseract+rescue toplam: {Len} karakter. ExtractionId={Id}", rawText?.Length ?? 0, extractionId);
                }

                // 4) Tüm sayfaları kapsayan komple vision fallback — Tesseract da rescue da boş döndüyse.
                if (string.IsNullOrWhiteSpace(rawText))
                {
                    _logger.LogInformation("Tesseract+rescue boş, komple vision fallback. ExtractionId={Id}", extractionId);
                    var (text, diag) = await TryFullDocumentVisionAsync(extraction.ContractFile.FilePath, ct);
                    rawText = text;
                    visionDiag ??= diag;
                }

                if (string.IsNullOrWhiteSpace(rawText))
                {
                    var tesDiag = _tesseract.LastDiagnostic ?? "boş";
                    var visDiag = visionDiag ?? "denenmedi";
                    await MarkFailedAsync(extractionId,
                        $"PDF'ten metin çıkarılamadı. PdfPig=boş | Tesseract={tesDiag} | Vision={visDiag}");
                    return;
                }

                extraction.RawText = rawText;
                db.Entry(extraction).Property(x => x.RawText).IsModified = true;
                await db.SaveChangesAsync(ct);
            }

            // Adım 1 — Aşama 1 AI çağrısı
            // Retry: önceki denemede Stage1 başarılı olmuş ve ExtractionResultJson kaydedilmişse
            // tekrar LLM çağrısı yapmaktan kaçın (hem maliyet hem tutarsızlık).
            string? stage1Json = extraction.ExtractionResultJson;
            string? stage1ModelUsed = null;
            int stage1InputTokens = 0, stage1OutputTokens = 0;

            if (string.IsNullOrWhiteSpace(stage1Json))
            {
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

                stage1Json = stage1Result.RawJson;
                stage1ModelUsed = stage1Result.ModelUsed;
                stage1InputTokens = stage1Result.InputTokens;
                stage1OutputTokens = stage1Result.OutputTokens;

                extraction.ExtractionResultJson = stage1Json;
                db.Entry(extraction).Property(x => x.ExtractionResultJson).IsModified = true;
                await db.SaveChangesAsync(ct);

                _logger.LogInformation("Stage1 tamamlandı. ExtractionId={Id}, Model={Model}", extractionId, stage1ModelUsed);
            }
            else
            {
                _logger.LogInformation("Stage1 atlandı (retry — önceki denemeden JSON mevcut). ExtractionId={Id}", extractionId);
            }

            // Aşama 1 sonucundan kategori belirle → Aşama 2 prompt seç
            string? detectedCategory = null;
            try
            {
                using var doc1 = JsonDocument.Parse(stage1Json ?? "{}");
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
                {stage1Json}

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
            extraction.ExtractionResultJson = stage1Json;
            extraction.Stage2ResultText = stage2Result.IsSuccess ? stage2Result.RawJson : null;
            extraction.PromptVersion = ExtractionPrompts.PromptVersion;
            extraction.ModelUsed = stage1ModelUsed ?? "";
            extraction.InputTokens = stage1InputTokens + stage2Result.InputTokens;
            extraction.OutputTokens = stage1OutputTokens + stage2Result.OutputTokens;
            extraction.ProcessedAt = DateTime.UtcNow;
            UpdateProgress(db, extraction, "done");
            extraction.ErrorMessage = stage2Result.IsSuccess
                ? null
                : $"Uyarı: Kategori analizi (Stage 2) başarısız — temel çıkarım gösteriliyor. Hata: {stage2Result.Error}";

            // Adım 4 — JSON'daki obligations/events/risks → AiSuggestions tablosuna ekle.
            // Retry durumunda eski Pending önerileri sil; Approved/Rejected olanları koru.
            var oldPending = await db.AiSuggestions
                .Where(s => s.ExtractionId == extractionId && s.Status == SuggestionStatus.Pending)
                .ToListAsync(ct);
            db.AiSuggestions.RemoveRange(oldPending);

            var (newSuggestions, suggParseError) = BuildSuggestions(extractionId, extraction.FirmaId, stage1Json);
            if (newSuggestions.Count > 0)
                db.AiSuggestions.AddRange(newSuggestions);
            if (suggParseError is not null)
                extraction.ErrorMessage = extraction.ErrorMessage is null
                    ? $"Uyarı: {suggParseError}"
                    : extraction.ErrorMessage + $" Uyarı: {suggParseError}";

            await db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "AI extraction tamamlandı. Id={Id}, Model={Model}, Tokens={In}+{Out}",
                extractionId, extraction.ModelUsed, extraction.InputTokens, extraction.OutputTokens);
        }

        [SupportedOSPlatform("windows")]
        private async Task<(Dictionary<int, string> byPage, string? diag)> TryRescueLowConfPagesAsync(
            string filePath, HashSet<int> pageIndices, CancellationToken ct)
        {
            var byPage = new Dictionary<int, string>();
            if (pageIndices.Count == 0) return (byPage, null);

            using var scope = _scopeFactory.CreateScope();
            var vision = scope.ServiceProvider.GetRequiredService<Mosaik.Core.Ai.IAiVisionProvider>();
            var absolute = Path.IsPathRooted(filePath)
                ? filePath
                : Path.Combine(scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>().WebRootPath,
                    filePath.TrimStart('/', '\\'));

            if (!File.Exists(absolute))
                return (byPage, $"Vision rescue: dosya yok ({absolute})");

            var pdfBytes = await File.ReadAllBytesAsync(absolute, ct);
            int idx = 0;
            using var stream = new MemoryStream(pdfBytes);
            foreach (var bmp in PDFtoImage.Conversion.ToImages(stream, options: new(Dpi: 200)))
            {
                if (pageIndices.Contains(idx))
                {
                    try
                    {
                        using (bmp)
                        using (var jpgData = bmp.Encode(SkiaSharp.SKEncodedImageFormat.Jpeg, 90))
                        {
                            var b64 = Convert.ToBase64String(jpgData.ToArray());
                            var req = new Mosaik.Core.Ai.AiVisionRequest(
                                SystemPrompt: "Bu sözleşme sayfasındaki Türkçe metni eksiksiz oku. " +
                                              "Sadece gördüğün metni döndür, yorum/özet yapma. " +
                                              "Madde numaralarını ve paragraf yapısını koru.",
                                UserPrompt: $"Sayfa {idx + 1} metnini düz Türkçe olarak ver.",
                                Base64Images: new[] { b64 },
                                MimeType: "image/jpeg",
                                RequireJson: false,
                                Purpose: "contract_ocr_per_page_rescue");
                            var result = await vision.GenerateFromImagesAsync(req, ct);
                            if (result.IsSuccess && !string.IsNullOrWhiteSpace(result.RawJson))
                                byPage[idx] = result.RawJson!;
                            else
                                _logger.LogWarning("Vision rescue sayfa {Page} başarısız: {Err}", idx + 1, result.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Vision rescue sayfa {Page} exception", idx + 1);
                    }
                }
                else
                {
                    bmp.Dispose();
                }
                idx++;
            }

            return (byPage, byPage.Count == 0 ? "Vision rescue: tüm sayfalar boş döndü" : null);
        }

        [SupportedOSPlatform("windows")]
        private async Task<(string text, string? diag)> TryFullDocumentVisionAsync(string filePath, CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var vision = scope.ServiceProvider.GetRequiredService<Mosaik.Core.Ai.IAiVisionProvider>();
            var absolute = Path.IsPathRooted(filePath)
                ? filePath
                : Path.Combine(scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>().WebRootPath,
                    filePath.TrimStart('/', '\\'));

            if (!File.Exists(absolute))
                return (string.Empty, $"Vision full: dosya yok ({absolute})");

            var b64Images = new List<string>();
            var pdfBytes = await File.ReadAllBytesAsync(absolute, ct);
            using (var stream = new MemoryStream(pdfBytes))
            {
                int p = 0;
                foreach (var bmp in PDFtoImage.Conversion.ToImages(stream, options: new(Dpi: 150)))
                {
                    if (p >= 3) { bmp.Dispose(); break; }
                    using (bmp)
                    using (var jpgData = bmp.Encode(SkiaSharp.SKEncodedImageFormat.Jpeg, 85))
                        b64Images.Add(Convert.ToBase64String(jpgData.ToArray()));
                    p++;
                }
            }

            if (b64Images.Count == 0)
                return (string.Empty, "Vision full: PDF sayfası görüntüye çevrilemedi");

            var req = new Mosaik.Core.Ai.AiVisionRequest(
                SystemPrompt: ExtractionPrompts.Stage1SystemPrompt,
                UserPrompt: "Bu sözleşme sayfalarındaki tüm metni oku ve düz metin olarak döndür.",
                Base64Images: b64Images,
                MimeType: "image/jpeg",
                RequireJson: false,
                Purpose: "contract_ocr_vision_full");
            var result = await vision.GenerateFromImagesAsync(req, ct);
            if (result.IsSuccess && !string.IsNullOrWhiteSpace(result.RawJson))
                return (result.RawJson!, null);
            return (string.Empty, $"Vision full: IsSuccess={result.IsSuccess}, Hata={result.Error}");
        }

        private (List<ContractAiSuggestionDraft> Drafts, string? ParseError) ParseDrafts(string? rawJson)
        {
            var list = new List<ContractAiSuggestionDraft>();
            if (string.IsNullOrWhiteSpace(rawJson)) return (list, null);

            try
            {
                using var doc = JsonDocument.Parse(rawJson);
                var root = doc.RootElement;

                if (root.TryGetProperty("obligations", out var oa) && oa.ValueKind == JsonValueKind.Array)
                {
                    foreach (var o in oa.EnumerateArray())
                        list.Add(new ContractAiSuggestionDraft(
                            SuggestionType.Obligation,
                            Title: GetStr(o, "title") ?? "Yükümlülük",
                            Description: BuildObligationDescription(o),
                            Confidence: ParseConfidence(GetStr(o, "confidence")),
                            DataJson: o.GetRawText()));
                }

                if (root.TryGetProperty("events", out var ea) && ea.ValueKind == JsonValueKind.Array)
                {
                    foreach (var e in ea.EnumerateArray())
                    {
                        var date = GetStr(e, "date");
                        var desc = GetStr(e, "description");
                        list.Add(new ContractAiSuggestionDraft(
                            SuggestionType.ContractEvent,
                            Title: GetStr(e, "title") ?? "Tarih",
                            Description: string.IsNullOrWhiteSpace(date) ? desc : $"{date} — {desc}",
                            Confidence: ParseConfidence(GetStr(e, "confidence")),
                            DataJson: e.GetRawText()));
                    }
                }

                if (root.TryGetProperty("risks", out var ra) && ra.ValueKind == JsonValueKind.Array)
                {
                    foreach (var r in ra.EnumerateArray())
                        list.Add(new ContractAiSuggestionDraft(
                            SuggestionType.RiskWarning,
                            Title: GetStr(r, "title") ?? "Risk",
                            Description: GetStr(r, "description"),
                            Confidence: ParseConfidence(GetStr(r, "severity")),
                            DataJson: r.GetRawText()));
                }
            }
            catch (JsonException jex)
            {
                _logger.LogWarning(jex,
                    "AI öneri JSON parse başarısız — extraction sıfır öneriyle kaydediliyor. RawJson(ilk 200): {Raw}",
                    rawJson?[..Math.Min(200, rawJson.Length)]);
                return (list, $"AI öneri JSON parse başarısız: {jex.GetType().Name}.");
            }
            return (list, null);
        }

        private (List<AiSuggestion> Suggestions, string? ParseError) BuildSuggestions(int extractionId, int firmaId, string? rawJson)
        {
            var (drafts, parseError) = ParseDrafts(rawJson);
            var now = DateTime.UtcNow;
            var suggestions = drafts.Select(d => new AiSuggestion
            {
                ExtractionId = extractionId,
                FirmaId = firmaId,
                SuggestionType = d.SuggestionType,
                Title = Truncate(d.Title, 200) ?? string.Empty,
                Description = Truncate(d.Description, 2000),
                Confidence = d.Confidence,
                Status = SuggestionStatus.Pending,
                SuggestionDataJson = d.DataJson,
                CreatedAt = now,
                UpdatedAt = now
            }).ToList();
            return (suggestions, parseError);
        }

        private sealed record ContractAiSuggestionDraft(
            SuggestionType SuggestionType, string Title, string? Description, Confidence Confidence, string? DataJson);

        private static string BuildObligationDescription(JsonElement o)
        {
            var desc = GetStr(o, "description") ?? "";
            var amount = o.TryGetProperty("amount", out var a) && a.ValueKind == JsonValueKind.Number && a.TryGetDecimal(out var d) ? d : (decimal?)null;
            var currency = GetStr(o, "currency");
            var dueDate = GetStr(o, "dueDate");
            var recurrence = GetStr(o, "recurrenceType");

            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(desc)) parts.Add(desc);
            if (amount.HasValue) parts.Add($"Tutar: {amount:N2} {currency ?? "TRY"}");
            if (!string.IsNullOrWhiteSpace(dueDate)) parts.Add($"Vade: {dueDate}");
            if (!string.IsNullOrWhiteSpace(recurrence)) parts.Add($"Periyot: {recurrence}");
            return string.Join(" · ", parts);
        }

        private static string? GetStr(JsonElement el, string prop) =>
            el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(v.GetString())
                ? v.GetString() : null;

        private static Confidence ParseConfidence(string? s)
        {
            var v = s?.Trim().ToLowerInvariant();
            if (v == null) return Confidence.Medium;
            if (v.StartsWith("high") || v.Contains("yüksek")) return Confidence.High;
            if (v.StartsWith("low")  || v.Contains("düşük"))  return Confidence.Low;
            return Confidence.Medium;
        }

        private static string? Truncate(string? s, int max) =>
            string.IsNullOrEmpty(s) ? s : (s.Length <= max ? s : s[..max]);

        private static void UpdateProgress(Mosaik.Models.MosaikContext db, ContractAiExtraction e, string step)
        {
            e.ProgressStep = step;
            db.Entry(e).Property(x => x.ProgressStep).IsModified = true;

            // Plan 27 Faz A — adım başlangıç zamanını JSON'a ekle.
            // Aynı step tekrar set edilirse zaman üzerine yazılmaz (retry mantığı).
            var stamps = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(e.ProgressTimestampsJson))
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(e.ProgressTimestampsJson);
                    if (parsed != null) stamps = parsed;
                }
                catch (JsonException) { /* malformed → sıfırla */ }
            }
            if (!stamps.ContainsKey(step))
            {
                stamps[step] = DateTime.UtcNow.ToString("o");
                e.ProgressTimestampsJson = JsonSerializer.Serialize(stamps);
                db.Entry(e).Property(x => x.ProgressTimestampsJson).IsModified = true;
            }
        }

        private async Task MarkFailedAsync(int extractionId, string error)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "MarkFailed DB hatası — extraction {Id} 'Failed' olarak işaretlenemedi, state belirsiz.", extractionId);
            }
        }
    }
}
