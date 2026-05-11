using System.Text;
using System.Text.Json;
using Mosaik.Core.Ai;

namespace Mosaik.Services
{
    // Plan 17 Faz F — IAiSummaryProvider host implementasyonu.
    //
    // OpenAI-compatible providers (groq/grok/openai/openrouter/ollama) tek handler
    // ile çalışır — sadece BaseUrl + Model + Auth header değişir. Gemini özel format.
    //
    // Modül override: AiRequest.OverrideModel/MaxTokens/Temperature → o çağrıya özel,
    // global config bozulmaz.
    public class AiSummaryProvider : IAiSummaryProvider
    {
        private readonly IAiSettingsProvider _settings;
        private readonly IHttpClientFactory _httpFactory;
        private readonly ILogger<AiSummaryProvider> _logger;

        public AiSummaryProvider(
            IAiSettingsProvider settings,
            IHttpClientFactory httpFactory,
            ILogger<AiSummaryProvider> logger)
        {
            _settings = settings;
            _httpFactory = httpFactory;
            _logger = logger;
        }

        public async Task<AiSummaryResult> GenerateAsync(AiRequest req, CancellationToken ct = default)
        {
            var configs = await _settings.GetActiveOrderedAsync(ct);
            if (configs.Count == 0)
                return new AiSummaryResult(false, null, "AI ayarları kapalı veya API key girilmemiş.");

            AiSummaryResult? lastFail = null;
            var attempted = new List<string>();

            foreach (var cfg in configs)
            {
                // Per-call override
                var effective = cfg with
                {
                    Model = req.OverrideModel ?? cfg.Model,
                    MaxTokens = req.OverrideMaxTokens ?? cfg.MaxTokens,
                    Temperature = req.OverrideTemperature ?? cfg.Temperature
                };

                AiSummaryResult result;
                try
                {
                    result = cfg.Provider.ToLowerInvariant() switch
                    {
                        "gemini" => await CallGeminiAsync(effective, req, ct),
                        "zai" => await CallZaiAsync(effective, req, ct),
                        _ => await CallOpenAiCompatibleAsync(effective, req, ct)
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "AI provider hata (provider={Provider}, model={Model}, purpose={Purpose})",
                        cfg.Provider, effective.Model, req.Purpose);
                    result = new AiSummaryResult(false, null, $"{cfg.Provider} exception: {ex.Message}");
                }

                if (result.IsSuccess)
                {
                    if (attempted.Count > 0)
                        _logger.LogInformation("AI fallback success on {Provider}/{Model} after {Failed} failure(s): {Attempted}",
                            cfg.Provider, effective.Model, attempted.Count, string.Join(", ", attempted));
                    return result;
                }

                lastFail = result;
                attempted.Add($"{cfg.Provider}/{effective.Model}");
                _logger.LogWarning("AI provider {Provider}/{Model} başarısız, fallback deneniyor: {Error}",
                    cfg.Provider, effective.Model, result.Error);
            }

            // Tüm provider'lar başarısız
            return lastFail ?? new AiSummaryResult(false, null,
                $"Tüm AI provider'lar başarısız ({attempted.Count} deneme): {string.Join("; ", attempted)}");
        }

        private async Task<AiSummaryResult> CallOpenAiCompatibleAsync(AiConfig cfg, AiRequest req, CancellationToken ct)
        {
            var http = _httpFactory.CreateClient("ai");
            http.Timeout = TimeSpan.FromSeconds(180);

            var baseUrl = (cfg.BaseUrl ?? DefaultBaseUrlFor(cfg.Provider)).TrimEnd('/');
            var url = baseUrl + "/chat/completions";

            object body = req.RequireJson
                ? new
                {
                    model = cfg.Model,
                    messages = new object[]
                    {
                        new { role = "system", content = req.SystemPrompt },
                        new { role = "user", content = req.UserPrompt }
                    },
                    response_format = new { type = "json_object" },
                    temperature = cfg.Temperature,
                    max_tokens = cfg.MaxTokens
                }
                : new
                {
                    model = cfg.Model,
                    messages = new object[]
                    {
                        new { role = "system", content = req.SystemPrompt },
                        new { role = "user", content = req.UserPrompt }
                    },
                    temperature = cfg.Temperature,
                    max_tokens = cfg.MaxTokens
                };

            using var msg = new HttpRequestMessage(HttpMethod.Post, url);
            msg.Headers.Add("Authorization", $"Bearer {cfg.ApiKey}");
            msg.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            using var resp = await http.SendAsync(msg, ct);
            var text = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                return new AiSummaryResult(false, null,
                    $"{cfg.Provider} HTTP {(int)resp.StatusCode}: {Truncate(text, 400)}");
            }

            string content;
            int inTok = 0, outTok = 0;
            try
            {
                using var doc = JsonDocument.Parse(text);
                content = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "";

                if (doc.RootElement.TryGetProperty("usage", out var u))
                {
                    if (u.TryGetProperty("prompt_tokens", out var pt)) inTok = pt.GetInt32();
                    if (u.TryGetProperty("completion_tokens", out var ot)) outTok = ot.GetInt32();
                }
            }
            catch (Exception ex)
            {
                return new AiSummaryResult(false, null,
                    $"{cfg.Provider} JSON parse/yapı hatası: {ex.Message}. Body: {Truncate(text, 300)}");
            }

            if (string.IsNullOrWhiteSpace(content))
                return new AiSummaryResult(false, null,
                    $"{cfg.Provider} content boş döndü (HTTP 200 ama içerik yok).");

            return new AiSummaryResult(true, content, null, inTok, outTok, cfg.Model);
        }

        // z.ai (Zhipu GLM) — docs.z.ai/api-reference/llm/chat-completion
        // OpenAI-compatible AMA bazı modeller response_format'a "Invalid API parameter" verebiliyor.
        // İki aşamalı: önce response_format ile dene, "Invalid" alınca response_format'sız tekrar dene
        // (system prompt'ta JSON istemi zaten var).
        private async Task<AiSummaryResult> CallZaiAsync(AiConfig cfg, AiRequest req, CancellationToken ct)
        {
            var first = await DoZaiAsync(cfg, req, includeJsonFormat: req.RequireJson, ct);
            if (first.IsSuccess) return first;

            // "Invalid API parameter" → response_format desteksiz olabilir, retry without
            if (req.RequireJson && (first.Error?.Contains("1210") ?? false))
            {
                _logger.LogWarning("z.ai response_format reddedildi, response_format'sız tekrar deniyor");
                return await DoZaiAsync(cfg, req, includeJsonFormat: false, ct);
            }
            return first;
        }

        private async Task<AiSummaryResult> DoZaiAsync(AiConfig cfg, AiRequest req, bool includeJsonFormat, CancellationToken ct)
        {
            var http = _httpFactory.CreateClient("ai");
            http.Timeout = TimeSpan.FromSeconds(180);

            var baseUrl = (cfg.BaseUrl ?? "https://api.z.ai/api/paas/v4").TrimEnd('/');
            var url = baseUrl + "/chat/completions";

            // z.ai temperature 0.0-1.0 (OpenAI gibi 0-2 değil) — sıkı clamp
            var zaiTemp = Math.Clamp(cfg.Temperature, 0.0, 1.0);

            var bodyDict = new Dictionary<string, object?>
            {
                ["model"] = cfg.Model,
                ["messages"] = new object[]
                {
                    new { role = "system", content = req.SystemPrompt },
                    new { role = "user", content = req.UserPrompt }
                },
                ["temperature"] = zaiTemp,
                // GLM-4.5+ "thinking" mode default'ta açık — reasoning_content'e yazıp
                // content'i boş bırakıyor + max_tokens'ı reasoning yiyor. Disable et.
                ["thinking"] = new { type = "disabled" },
                // Özet için min 2048 — system prompt + user prompt + answer rahat sığsın
                ["max_tokens"] = Math.Max(cfg.MaxTokens, 2048)
            };
            if (includeJsonFormat)
            {
                bodyDict["response_format"] = new { type = "json_object" };
            }

            var bodyJson = JsonSerializer.Serialize(bodyDict);
            _logger.LogInformation("z.ai REQUEST → {Url} body={Body}", url, Truncate(bodyJson, 600));

            using var msg = new HttpRequestMessage(HttpMethod.Post, url);
            msg.Headers.Add("Authorization", $"Bearer {cfg.ApiKey}");
            msg.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");

            using var resp = await http.SendAsync(msg, ct);
            var text = await resp.Content.ReadAsStringAsync(ct);

            _logger.LogInformation("z.ai RESPONSE ← {Status} body={Body}", (int)resp.StatusCode, Truncate(text, 800));

            if (!resp.IsSuccessStatusCode)
            {
                return new AiSummaryResult(false, null, $"z.ai HTTP {(int)resp.StatusCode}: {Truncate(text, 400)}");
            }

            using var doc = JsonDocument.Parse(text);
            var firstChoice = doc.RootElement.GetProperty("choices")[0];
            var message = firstChoice.GetProperty("message");
            var content = message.GetProperty("content").GetString() ?? "";
            var finishReason = firstChoice.TryGetProperty("finish_reason", out var fr) ? fr.GetString() : null;

            // content boşsa: reasoning_content'a düşmüş olabilir veya finish_reason=length
            if (string.IsNullOrWhiteSpace(content))
            {
                if (message.TryGetProperty("reasoning_content", out var rc))
                {
                    var reasoning = rc.GetString();
                    if (!string.IsNullOrWhiteSpace(reasoning))
                    {
                        _logger.LogWarning("z.ai content boş, reasoning_content'tan JSON ayıklanıyor (finish={Finish})", finishReason);
                        // reasoning_content'tan JSON bloğunu çek (markdown ```json...``` veya saf JSON)
                        var extracted = ExtractJsonBlock(reasoning);
                        if (!string.IsNullOrWhiteSpace(extracted))
                            content = extracted;
                    }
                }

                if (string.IsNullOrWhiteSpace(content))
                {
                    return new AiSummaryResult(false, null,
                        $"z.ai content boş döndü (finish_reason={finishReason}). max_tokens artırılmalı veya farklı model denenebilir.");
                }
            }

            int inTok = 0, outTok = 0;
            if (doc.RootElement.TryGetProperty("usage", out var u))
            {
                if (u.TryGetProperty("prompt_tokens", out var pt)) inTok = pt.GetInt32();
                if (u.TryGetProperty("completion_tokens", out var ot)) outTok = ot.GetInt32();
            }

            return new AiSummaryResult(true, content, null, inTok, outTok, cfg.Model);
        }

        // Markdown ```json ... ``` veya saf JSON bloğunu metinden ayıkla
        private static string? ExtractJsonBlock(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            // Markdown code fence
            var m = System.Text.RegularExpressions.Regex.Match(text, @"```(?:json)?\s*(\{[\s\S]*?\})\s*```");
            if (m.Success) return m.Groups[1].Value;
            // İlk { ile son } arası
            var first = text.IndexOf('{');
            var last = text.LastIndexOf('}');
            if (first >= 0 && last > first) return text.Substring(first, last - first + 1);
            return null;
        }

        private async Task<AiSummaryResult> CallGeminiAsync(AiConfig cfg, AiRequest req, CancellationToken ct)
        {
            var http = _httpFactory.CreateClient("ai");
            http.Timeout = TimeSpan.FromSeconds(180);

            var baseUrl = (cfg.BaseUrl ?? "https://generativelanguage.googleapis.com/v1beta").TrimEnd('/');
            var url = $"{baseUrl}/models/{cfg.Model}:generateContent?key={cfg.ApiKey}";

            object generationConfig = req.RequireJson
                ? new
                {
                    temperature = cfg.Temperature,
                    maxOutputTokens = cfg.MaxTokens,
                    response_mime_type = "application/json"
                }
                : new
                {
                    temperature = cfg.Temperature,
                    maxOutputTokens = cfg.MaxTokens
                };

            var body = new
            {
                contents = new object[]
                {
                    new { role = "user", parts = new object[] { new { text = req.SystemPrompt + "\n\n" + req.UserPrompt } } }
                },
                generationConfig
            };

            using var resp = await http.PostAsync(url,
                new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"), ct);
            var text = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                return new AiSummaryResult(false, null,
                    $"Gemini HTTP {(int)resp.StatusCode}: {Truncate(text, 400)}");
            }

            string content;
            int inTok = 0, outTok = 0;
            try
            {
                using var doc = JsonDocument.Parse(text);
                content = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString() ?? "";

                if (doc.RootElement.TryGetProperty("usageMetadata", out var u))
                {
                    if (u.TryGetProperty("promptTokenCount", out var pt)) inTok = pt.GetInt32();
                    if (u.TryGetProperty("candidatesTokenCount", out var ot)) outTok = ot.GetInt32();
                }
            }
            catch (Exception ex)
            {
                return new AiSummaryResult(false, null,
                    $"Gemini JSON parse/yapı hatası: {ex.Message}. Body: {Truncate(text, 300)}");
            }

            if (string.IsNullOrWhiteSpace(content))
                return new AiSummaryResult(false, null,
                    "Gemini content boş döndü (HTTP 200 ama içerik yok).");

            return new AiSummaryResult(true, content, null, inTok, outTok, cfg.Model);
        }

        private static string DefaultBaseUrlFor(string provider) => provider.ToLowerInvariant() switch
        {
            "groq" => "https://api.groq.com/openai/v1",
            "grok" => "https://api.x.ai/v1",
            "openai" => "https://api.openai.com/v1",
            "openrouter" => "https://openrouter.ai/api/v1",
            "zai" => "https://api.z.ai/api/paas/v4",
            "ollama" => "http://localhost:11434/v1",
            _ => "https://api.groq.com/openai/v1"
        };

        private static string Truncate(string s, int max) =>
            string.IsNullOrEmpty(s) ? "" : (s.Length <= max ? s : s.Substring(0, max) + "...");
    }
}
