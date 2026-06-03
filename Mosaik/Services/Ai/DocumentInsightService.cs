using System.Text.Json;
using Mosaik.Core.Ai;

namespace Mosaik.Services.Ai
{
    // Plan 27 Faz B (B-01 + B-03 birleşik):
    // Yüklenen doküman için tek z.ai çağrısıyla executive summary + tag classification yap.
    // Auto-tagging (Paperless-ngx pattern) + 1 paragraf yönetici özeti birlikte üretilir.
    // Kullanım: DocumentsController.Upload sonrası fire-and-forget background task.
    public sealed class DocumentInsightService
    {
        private const int MaxInputChars = 8000;  // ~2K token — cloud'da hızlı yanıt, Qwen 4096-ctx safe
        private const string SystemPrompt = """
            Sen bir kurumsal doküman analiz uzmanısın. Sana verilen metni inceleyip
            aşağıdaki JSON formatında yapılandırılmış özet üret:

            {
              "summary": "1 paragraflık yönetici özeti (max 3 cümle, max 300 karakter). Belge ne hakkında, kim için, hangi sonuca varıyor.",
              "documentType": "Sözleşme|Fatura|Tamim|Rapor|Sunum|Mektup|Form|Yönetmelik|Diğer",
              "year": 2026,
              "department": "Hukuk|Finans|İK|Operasyon|IT|Pazarlama|Satış|Yönetim|Bilinmiyor",
              "tags": ["max-5", "anahtar-kelime", "küçük-harf", "tire-ayrılmış"]
            }

            KURALLAR:
            - Sadece geçerli JSON döndür. Markdown bloğu yok.
            - summary 1 paragraf, max 3 cümle.
            - documentType listeden EN UYGUN olanı seç.
            - year: metinde geçen birincil tarih yılı. Bulamazsan null.
            - department: belgenin hangi birimi ilgilendirdiği. Belirsizse "Bilinmiyor".
            - tags: 3-5 adet, Türkçe küçük harf, tire-ayrılmış (örn: "kira-sözleşmesi", "ödeme-vadesi").
            - Hallüsinasyon yapma; metinde olmayan bilgiyi uydurma.
            """;

        private readonly IAiSummaryProvider _ai;
        private readonly ILogger<DocumentInsightService> _logger;

        public DocumentInsightService(IAiSummaryProvider ai, ILogger<DocumentInsightService> logger)
        {
            _ai = ai;
            _logger = logger;
        }

        public async Task<DocumentInsightResult?> AnalyzeAsync(string text, string? fileName, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("DocumentInsight: boş metin, atlanıyor.");
                return null;
            }

            // Çok uzun metni kırp — yönetici özeti için 16K char yeterli.
            var input = text.Length > MaxInputChars ? text[..MaxInputChars] + "\n[... metin kırpıldı ...]" : text;
            var userPrompt = $"Dosya adı: {fileName ?? "—"}\n\nMetin:\n---\n{input}\n---";

            var result = await _ai.GenerateAsync(new AiRequest(
                SystemPrompt: SystemPrompt,
                UserPrompt: userPrompt,
                RequireJson: true,
                Purpose: "document_insight"
            ), ct);

            if (!result.IsSuccess || string.IsNullOrWhiteSpace(result.RawJson))
            {
                _logger.LogWarning("DocumentInsight AI çağrısı başarısız: {Error}", result.Error ?? "boş yanıt");
                return null;
            }

            try
            {
                using var doc = JsonDocument.Parse(result.RawJson);
                var root = doc.RootElement;
                var summary = StrOrNull(root, "summary");
                if (summary != null && summary.Length > 2000) summary = summary[..2000];

                // tagsJson alanlarını kompakt JSON olarak topla (DB'de saklanacak)
                var tagsObj = new
                {
                    documentType = StrOrNull(root, "documentType"),
                    year = root.TryGetProperty("year", out var y) && y.ValueKind == JsonValueKind.Number && y.TryGetInt32(out var yi) ? (int?)yi : null,
                    department = StrOrNull(root, "department"),
                    tags = TagsArray(root)
                };
                var tagsJson = JsonSerializer.Serialize(tagsObj, new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

                return new DocumentInsightResult(summary, tagsJson, result.ModelUsed, result.InputTokens, result.OutputTokens);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "DocumentInsight JSON parse hatası. Raw={Raw}", result.RawJson?[..Math.Min(300, result.RawJson?.Length ?? 0)]);
                return null;
            }
        }

        private static string? StrOrNull(JsonElement el, string prop) =>
            el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(v.GetString())
                ? v.GetString() : null;

        private static List<string> TagsArray(JsonElement root)
        {
            var list = new List<string>();
            if (root.TryGetProperty("tags", out var ta) && ta.ValueKind == JsonValueKind.Array)
            {
                foreach (var t in ta.EnumerateArray())
                {
                    if (t.ValueKind == JsonValueKind.String)
                    {
                        var s = t.GetString();
                        if (!string.IsNullOrWhiteSpace(s)) list.Add(s);
                    }
                }
            }
            return list;
        }
    }

    public sealed record DocumentInsightResult(string? Summary, string? TagsJson, string? ModelUsed, int InputTokens, int OutputTokens);
}
