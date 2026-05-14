using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Mosaik.Core.Ai;
using System.Text.Json;

namespace Mosaik.Services.Ai
{
    // Plan 25 wizard — senkron mantıkla AI extraction. Mevcut AiPipelineQueue/AiExtractionWorker
    // pattern'i "yükle → kuyruğa at → arka planda işle → review et" akışı içindi (DB'ye yazıyor).
    // Wizard akışında: "yükle → AI sonucunu bekle → form'a doldur → kullanıcı kaydederse DB'ye yaz".
    //
    // Pattern: jobId üret, Task.Run ile fire-and-forget, sonucu IMemoryCache'e koy (10dk TTL).
    // Frontend ExtractionStatus endpoint'inden polling yapar.
    public sealed class WizardExtractionService
    {
        private const int CacheTtlMinutes = 10;
        private const int MaxRawTextChars = 12000;

        private readonly IMemoryCache _cache;
        private readonly IPdfTextExtractor _pdfExtractor;
        private readonly TesseractOcrExtractor _tesseract;
        private readonly IAiSummaryProvider _ai;
        private readonly IAiVisionProvider _aiVision;
        private readonly ILogger<WizardExtractionService> _logger;
        private readonly IWebHostEnvironment _env;

        public WizardExtractionService(
            IMemoryCache cache,
            IPdfTextExtractor pdfExtractor,
            TesseractOcrExtractor tesseract,
            IAiSummaryProvider ai,
            IAiVisionProvider aiVision,
            ILogger<WizardExtractionService> logger,
            IWebHostEnvironment env)
        {
            _cache = cache;
            _pdfExtractor = pdfExtractor;
            _tesseract = tesseract;
            _ai = ai;
            _aiVision = aiVision;
            _logger = logger;
            _env = env;
        }

        public sealed record JobStatus(bool Done, WizardExtractionResult? Result, string? Error);

        // Yeni iş kuyruğa atar, jobId döner. Frontend GetStatus ile polling yapar.
        public string Start(string absoluteFilePath, string mimeType)
        {
            // H-5 hardening — defense-in-depth path validation.
            // Wizard endpoint (Faz 2) bu service'e contractFile'ın disk path'ini geçer.
            // Arbitrary path geçilirse exfil riski (AI provider'a base64 dosya yüklenir).
            var fullPath = Path.GetFullPath(absoluteFilePath);
            var allowedRoot = Path.GetFullPath(Path.Combine(_env.ContentRootPath, "App_Data"));
            if (!fullPath.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("WizardExtraction.Start path traversal blocked: {Path}", fullPath);
                throw new InvalidOperationException("Geçersiz dosya yolu.");
            }

            var jobId = Guid.NewGuid().ToString("N");
            _cache.Set("wizjob:" + jobId, new JobStatus(false, null, null),
                TimeSpan.FromMinutes(CacheTtlMinutes));

            // Fire-and-forget. Hata loglanır + cache'e generic mesaj yazılır.
            // NOT: AppDomain shutdown sırasında pending task abort olur — Plan 25.1 hardening
            // queue pattern'ine geçişi planlar (HIGH risk, ayrı plan).
            _ = Task.Run(async () =>
            {
                try
                {
                    var result = await ExecuteAsync(absoluteFilePath, mimeType, CancellationToken.None);
                    _cache.Set("wizjob:" + jobId, new JobStatus(true, result, null),
                        TimeSpan.FromMinutes(CacheTtlMinutes));
                }
                catch (InvalidOperationException ioex)
                {
                    // Beklenen domain hataları (PDF taranmış, format desteklenmiyor, AI fail) — mesaj kullanıcıya gösterilir.
                    _logger.LogWarning(ioex, "WizardExtraction job {JobId} domain hata", jobId);
                    _cache.Set("wizjob:" + jobId, new JobStatus(true, null, ioex.Message),
                        TimeSpan.FromMinutes(CacheTtlMinutes));
                }
                catch (Exception ex)
                {
                    // Beklenmedik hata — ex.Message GÖSTERME (security-principles.md kural #7).
                    _logger.LogError(ex, "WizardExtraction job {JobId} beklenmedik hata", jobId);
                    _cache.Set("wizjob:" + jobId, new JobStatus(true, null,
                        "Sözleşme analiz edilirken beklenmedik bir hata oluştu. Lütfen tekrar deneyin."),
                        TimeSpan.FromMinutes(CacheTtlMinutes));
                }
            });

            return jobId;
        }

        public JobStatus? GetStatus(string jobId)
        {
            return _cache.TryGetValue<JobStatus>("wizjob:" + jobId, out var status) ? status : null;
        }

        // Asıl iş: dosyaya bak → text mi vision mı? → AI çağır → JSON parse et.
        private async Task<WizardExtractionResult> ExecuteAsync(string filePath, string mimeType, CancellationToken ct)
        {
            string rawText = string.Empty;
            bool useVision = false;
            string? base64Image = null;

            // 1) PDF → PdfPig ile text extract dene
            if (string.Equals(mimeType, "application/pdf", StringComparison.OrdinalIgnoreCase))
            {
                rawText = await _pdfExtractor.ExtractAsync(filePath, ct);
                if (rawText.Length < 100)
                {
                    // Plan 33 BUGFIX-4 (2026-05-15): Taranmış PDF — Tesseract OCR fallback.
                    // Önceki kod exception fırlatıyordu ("JPEG/PNG olarak tarayın"). Mevcut
                    // TesseractOcrExtractor + Türkçe/İngilizce dil + per-page confidence
                    // pattern AiExtractionWorker'da zaten kullanılıyor; Wizard da aynı chain'i
                    // kullansın. PDF→image vision fallback şimdilik scope dışı (AiExtractionWorker
                    // Ocr.cs pattern uzun vadede port edilir, Plan 33 Faz 4 D2 altında).
                    _logger.LogInformation(
                        "WizardExtraction PDF text yetersiz ({Len} char), Tesseract OCR fallback deneniyor: {Path}",
                        rawText.Length, filePath);

                    try
                    {
                        var ocrText = _tesseract.ExtractFromPdf(filePath);
                        if (!string.IsNullOrWhiteSpace(ocrText) && ocrText.Length >= 100)
                        {
                            rawText = ocrText;
                            _logger.LogInformation(
                                "WizardExtraction OCR başarılı: {Len} char extract edildi", rawText.Length);
                        }
                        else
                        {
                            _logger.LogWarning(
                                "WizardExtraction OCR sonucu yetersiz ({Len} char)", ocrText?.Length ?? 0);
                            throw new InvalidOperationException(
                                "Bu PDF'ten metin çıkarılamadı (taranmış görüntü ve OCR yetersiz). " +
                                "Lütfen sözleşmeyi daha yüksek çözünürlükte tarayıp tekrar yükleyin.");
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        throw; // user-facing mesajı koru
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "WizardExtraction Tesseract OCR hata: {Path}", filePath);
                        throw new InvalidOperationException(
                            "PDF metin çıkarımı sırasında beklenmedik bir hata oluştu. " +
                            "Sistem yöneticinize bildirin.");
                    }
                }
            }
            // 2) Image → vision pipeline
            else if (mimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                useVision = true;
                var bytes = await File.ReadAllBytesAsync(filePath, ct);
                base64Image = Convert.ToBase64String(bytes);
            }
            // 3) Word vs. → text path henüz desteklenmiyor
            else
            {
                throw new InvalidOperationException(
                    "Bu dosya türü AI tarafından okunamıyor. PDF veya görsel (JPG/PNG) yükleyin.");
            }

            // Token guard: çok uzun sözleşmeleri kırp
            if (rawText.Length > MaxRawTextChars)
            {
                _logger.LogWarning("WizardExtraction raw text {Len} > {Max}, kırpıldı", rawText.Length, MaxRawTextChars);
                rawText = rawText[..MaxRawTextChars];
            }

            AiSummaryResult aiResult;
            if (useVision)
            {
                var visionReq = new AiVisionRequest(
                    SystemPrompt: ExtractionPrompts.Stage1SystemPrompt + "\n\n" + WizardJsonShape,
                    UserPrompt: "Yukarıdaki sözleşme görselini analiz et ve JSON formatında dön.",
                    Base64Images: new[] { base64Image! },
                    MimeType: mimeType,
                    RequireJson: true,
                    Purpose: "contract_wizard_vision");
                aiResult = await _aiVision.GenerateFromImagesAsync(visionReq, ct);
            }
            else
            {
                var textReq = new AiRequest(
                    SystemPrompt: ExtractionPrompts.Stage1SystemPrompt + "\n\n" + WizardJsonShape,
                    UserPrompt: ExtractionPrompts.BuildUserPrompt(rawText, contractTitle: null),
                    RequireJson: true,
                    Purpose: "contract_wizard_text");
                aiResult = await _ai.GenerateAsync(textReq, ct);
            }

            if (!aiResult.IsSuccess || string.IsNullOrWhiteSpace(aiResult.RawJson))
            {
                _logger.LogWarning("Wizard AI çağrısı başarısız. Hata: {Error}", aiResult.Error ?? "boş yanıt");
                throw new InvalidOperationException("AI analizi tamamlanamadı. Lütfen tekrar deneyin.");
            }

            return ParseResult(aiResult.RawJson!);
        }

        private const string WizardJsonShape = @"
**ÇIKTI ŞEMASI (zorunlu):**
{
  ""title"": ""string|null"",
  ""counterparty"": ""string|null"",
  ""category"": 0,                  // 0=Lease, 1=Service, 2=Supply, 3=Employment, 4=License, 5=Insurance, 6=Other
  ""startDate"": ""YYYY-MM-DD|null"",
  ""endDate"": ""YYYY-MM-DD|null"",
  ""notes"": ""string|null"",
  ""obligations"": [
    {
      ""title"": ""Aylık Kira"",
      ""amount"": 5000.00,
      ""currency"": ""TRY"",
      ""isRecurring"": true,
      ""recurrenceType"": 0,         // 0=Monthly, 1=Quarterly, 2=Yearly, 3=Custom
      ""dayOfMonth"": 5,
      ""monthOfYear"": null,
      ""dueDate"": null,             // tek seferlikse ""YYYY-MM-DD""
      ""notes"": null
    }
  ]
}
Tüm alanlar opsiyonel; emin olmadıklarını null bırak. Halüsinasyon yapma.
";

        private WizardExtractionResult ParseResult(string rawJson)
        {
            // AI bazen ```json ... ``` markdown bloğu ile sarar; ayıkla
            var json = rawJson.Trim();
            var startIdx = json.IndexOf('{');
            var endIdx = json.LastIndexOf('}');
            if (startIdx < 0 || endIdx <= startIdx)
            {
                _logger.LogWarning("Wizard AI yanıtında JSON bulunamadı. Raw={Raw}", Truncate(rawJson, 500));
                throw new InvalidOperationException(
                    "AI yanıtı geçerli JSON formatında değil. Sözleşme metni okunamamış olabilir, lütfen tekrar deneyin.");
            }
            json = json[startIdx..(endIdx + 1)];

            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(json);
            }
            catch (JsonException jex)
            {
                _logger.LogWarning(jex, "Wizard AI JSON parse hatası. Raw={Raw}", Truncate(rawJson, 500));
                throw new InvalidOperationException(
                    "AI yanıtı çözümlenemedi. Lütfen tekrar deneyin.");
            }

            using (doc)
            {
                var root = doc.RootElement;

                var obligations = new List<WizardObligationSuggestion>();
                if (root.TryGetProperty("obligations", out var oblArr) && oblArr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var o in oblArr.EnumerateArray())
                    {
                        obligations.Add(new WizardObligationSuggestion(
                            Title: GetStr(o, "title"),
                            Amount: GetDec(o, "amount"),
                            Currency: GetStr(o, "currency") ?? "TRY",
                            IsRecurring: GetBool(o, "isRecurring") ?? false,
                            RecurrenceType: GetInt(o, "recurrenceType"),
                            DayOfMonth: GetInt(o, "dayOfMonth"),
                            MonthOfYear: GetInt(o, "monthOfYear"),
                            DueDate: GetStr(o, "dueDate"),
                            Notes: GetStr(o, "notes")));
                    }
                }

                return new WizardExtractionResult(
                    Title: GetStr(root, "title"),
                    Counterparty: GetStr(root, "counterparty"),
                    Category: GetInt(root, "category"),
                    StartDate: GetStr(root, "startDate"),
                    EndDate: GetStr(root, "endDate"),
                    Notes: GetStr(root, "notes"),
                    Obligations: obligations);
            }
        }

        private static string Truncate(string s, int max) =>
            string.IsNullOrEmpty(s) || s.Length <= max ? s : s[..max] + "…";

        private static string? GetStr(JsonElement el, string prop) =>
            el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String
                ? v.GetString()
                : null;

        private static int? GetInt(JsonElement el, string prop) =>
            el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var i)
                ? i
                : null;

        private static decimal? GetDec(JsonElement el, string prop) =>
            el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetDecimal(out var d)
                ? d
                : null;

        private static bool? GetBool(JsonElement el, string prop) =>
            el.TryGetProperty(prop, out var v) && (v.ValueKind == JsonValueKind.True || v.ValueKind == JsonValueKind.False)
                ? v.GetBoolean()
                : null;
    }
}
