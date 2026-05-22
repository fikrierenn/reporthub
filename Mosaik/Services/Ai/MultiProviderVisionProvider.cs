using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Mosaik.Core.Ai;

namespace Mosaik.Services.Ai
{
    // D-03 (2026-05-22): Multi-provider vision fallback zinciri.
    // IAiSettingsProvider'daki aktif config'leri sırayla dener: zai/openai/openrouter/grok → OpenAI-compatible, gemini → ayrı format.
    // groq/ollama vision desteklemez — atlanır. Hata gelirse fallback devam eder.
    // ZaiVisionProvider'ın yerini alır; aynı DI interface (IAiVisionProvider).
    public sealed class MultiProviderVisionProvider : IAiVisionProvider
    {
        private static readonly HashSet<string> VisionCapable =
            new(StringComparer.OrdinalIgnoreCase) { "zai", "gemini", "openai", "openrouter", "grok" };

        private readonly IHttpClientFactory _httpFactory;
        private readonly IAiSettingsProvider _settings;
        private readonly ILogger<MultiProviderVisionProvider> _logger;

        public MultiProviderVisionProvider(
            IHttpClientFactory httpFactory,
            IAiSettingsProvider settings,
            ILogger<MultiProviderVisionProvider> logger)
        {
            _httpFactory = httpFactory;
            _settings = settings;
            _logger = logger;
        }

        public async Task<AiSummaryResult> GenerateFromImagesAsync(AiVisionRequest request, CancellationToken ct = default)
        {
            if (request.Base64Images is null || request.Base64Images.Count == 0)
                return new AiSummaryResult(false, null, "Görsel sağlanmadı.");

            var configs = await _settings.GetActiveOrderedAsync(ct);
            var candidates = configs.Where(c => VisionCapable.Contains(c.Provider)).ToList();

            if (candidates.Count == 0)
                return new AiSummaryResult(false, null,
                    "Vision destekleyen aktif AI sağlayıcısı yok (zai/gemini/openai/openrouter/grok gerekli).");

            AiSummaryResult? lastFail = null;
            var attempted = new List<string>();

            foreach (var cfg in candidates)
            {
                ct.ThrowIfCancellationRequested();

                var model = request.OverrideModel ?? cfg.Model;
                AiSummaryResult result;
                try
                {
                    result = cfg.Provider.ToLowerInvariant() == "gemini"
                        ? await CallGeminiVisionAsync(cfg, model, request, ct)
                        : await CallOpenAiVisionAsync(cfg, model, request, ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Vision provider hata (provider={Provider}, model={Model})",
                        cfg.Provider, model);
                    result = new AiSummaryResult(false, null, $"{cfg.Provider} bağlantı hatası.");
                }

                if (result.IsSuccess)
                {
                    if (attempted.Count > 0)
                        _logger.LogInformation(
                            "Vision fallback success on {Provider}/{Model} after {N} failure(s): {Attempted}",
                            cfg.Provider, model, attempted.Count, string.Join(", ", attempted));
                    return result;
                }

                lastFail = result;
                attempted.Add($"{cfg.Provider}/{model}");
                _logger.LogWarning("Vision provider {Provider}/{Model} başarısız, fallback: {Error}",
                    cfg.Provider, model, result.Error);
            }

            return lastFail ?? new AiSummaryResult(false, null,
                $"Tüm vision provider'lar başarısız ({attempted.Count} deneme): {string.Join("; ", attempted)}");
        }

        // ---- OpenAI-compatible (zai / openai / openrouter / grok) ----
        // zai'da thinking mode hatası veya boş content gelirse fallback devam eder.
        private async Task<AiSummaryResult> CallOpenAiVisionAsync(
            AiConfig cfg, string model, AiVisionRequest request, CancellationToken ct)
        {
            var http = _httpFactory.CreateClient("ai-vision");
            var baseUrl = (cfg.BaseUrl ?? DefaultBaseUrlFor(cfg.Provider)).TrimEnd('/');

            var bodyDict = new Dictionary<string, object?>
            {
                ["model"]       = model,
                ["messages"]    = new object[] { new { role = "user", content = BuildOpenAiContentParts(request) } },
                ["temperature"] = request.OverrideTemperature ?? cfg.Temperature,
                ["max_tokens"]  = request.OverrideMaxTokens ?? cfg.MaxTokens
            };
            if (request.RequireJson) bodyDict["response_format"] = new { type = "json_object" };

            return await SendAndParseOpenAiStyleAsync(http, baseUrl + "/chat/completions",
                cfg.ApiKey, bodyDict, cfg.Provider, model, ct);
        }

        // ---- Gemini (inlineData format) ----
        private async Task<AiSummaryResult> CallGeminiVisionAsync(
            AiConfig cfg, string model, AiVisionRequest request, CancellationToken ct)
        {
            var http = _httpFactory.CreateClient("ai-vision");
            var baseUrl = (cfg.BaseUrl ?? "https://generativelanguage.googleapis.com/v1beta").TrimEnd('/');
            var url = $"{baseUrl}/models/{model}:generateContent?key={cfg.ApiKey}";

            var parts = new List<object>
            {
                new { text = request.SystemPrompt + "\n\n" + request.UserPrompt }
            };
            foreach (var b64 in request.Base64Images)
                parts.Add(new { inlineData = new { mimeType = request.MimeType, data = b64 } });

            object generationConfig = request.RequireJson
                ? new { temperature = request.OverrideTemperature ?? cfg.Temperature,
                        maxOutputTokens = request.OverrideMaxTokens ?? cfg.MaxTokens,
                        response_mime_type = "application/json" }
                : new { temperature = request.OverrideTemperature ?? cfg.Temperature,
                        maxOutputTokens = request.OverrideMaxTokens ?? cfg.MaxTokens };

            var body = new
            {
                contents = new object[] { new { role = "user", parts } },
                generationConfig
            };

            using var resp = await http.PostAsync(url,
                new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"), ct);
            var text = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Gemini vision HTTP error. Status={Status} Model={Model} Body={Body}",
                    (int)resp.StatusCode, model, Truncate(text, 400));
                return new AiSummaryResult(false, null,
                    $"Gemini vision sağlayıcı hatası (HTTP {(int)resp.StatusCode}).");
            }

            try
            {
                using var doc = JsonDocument.Parse(text);
                var content = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content").GetProperty("parts")[0]
                    .GetProperty("text").GetString() ?? "";

                int inTok = 0, outTok = 0;
                if (doc.RootElement.TryGetProperty("usageMetadata", out var u))
                {
                    if (u.TryGetProperty("promptTokenCount",     out var pt)) inTok  = pt.GetInt32();
                    if (u.TryGetProperty("candidatesTokenCount", out var ot)) outTok = ot.GetInt32();
                }

                return string.IsNullOrWhiteSpace(content)
                    ? new AiSummaryResult(false, null, "Gemini vision content boş döndü.")
                    : new AiSummaryResult(true, content, null, inTok, outTok, model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gemini vision JSON parse hatası. Body={Body}", Truncate(text, 300));
                return new AiSummaryResult(false, null, "Gemini vision yanıtı çözümlenemedi.");
            }
        }

        // ---- Helpers ----

        private static List<object> BuildOpenAiContentParts(AiVisionRequest request)
        {
            var parts = new List<object>
            {
                new { type = "text", text = request.SystemPrompt + "\n\n" + request.UserPrompt }
            };
            foreach (var b64 in request.Base64Images)
                parts.Add(new { type = "image_url",
                                image_url = new { url = $"data:{request.MimeType};base64,{b64}" } });
            return parts;
        }

        private async Task<AiSummaryResult> SendAndParseOpenAiStyleAsync(
            HttpClient http, string url, string apiKey,
            Dictionary<string, object?> bodyDict, string providerName, string model, CancellationToken ct)
        {
            using var msg = new HttpRequestMessage(HttpMethod.Post, url);
            msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            msg.Content = new StringContent(JsonSerializer.Serialize(bodyDict), Encoding.UTF8, "application/json");

            using var resp = await http.SendAsync(msg, ct);
            var text = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "{Provider} vision HTTP error. Status={Status} Model={Model} Body={Body}",
                    providerName, (int)resp.StatusCode, model, Truncate(text, 400));
                return new AiSummaryResult(false, null,
                    $"{providerName} vision sağlayıcı hatası (HTTP {(int)resp.StatusCode}).");
            }

            try
            {
                using var doc = JsonDocument.Parse(text);
                if (!doc.RootElement.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
                    return new AiSummaryResult(false, null, $"{providerName} vision: choices boş.");

                var content = choices[0].GetProperty("message").GetProperty("content").GetString() ?? "";

                int inTok = 0, outTok = 0;
                if (doc.RootElement.TryGetProperty("usage", out var u))
                {
                    if (u.TryGetProperty("prompt_tokens",     out var pt)) inTok  = pt.GetInt32();
                    if (u.TryGetProperty("completion_tokens", out var ot)) outTok = ot.GetInt32();
                }

                return string.IsNullOrWhiteSpace(content)
                    ? new AiSummaryResult(false, null, $"{providerName} vision content boş döndü.")
                    : new AiSummaryResult(true, content, null, inTok, outTok, model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Provider} vision JSON parse hatası. Body={Body}", providerName, Truncate(text, 300));
                return new AiSummaryResult(false, null, $"{providerName} vision yanıtı çözümlenemedi.");
            }
        }

        private static string DefaultBaseUrlFor(string provider) => provider.ToLowerInvariant() switch
        {
            "zai"        => "https://api.z.ai/api/paas/v4",
            "grok"       => "https://api.x.ai/v1",
            "openrouter" => "https://openrouter.ai/api/v1",
            _            => "https://api.openai.com/v1"
        };

        private static string Truncate(string s, int max) =>
            string.IsNullOrEmpty(s) || s.Length <= max ? s : s[..max] + "…";
    }
}
