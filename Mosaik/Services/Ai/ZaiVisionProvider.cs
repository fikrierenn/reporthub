using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Mosaik.Core.Ai;

namespace Mosaik.Services.Ai
{
    // Plan 25 wizard — z.ai GLM-4V (vision). Mevcut text-only AiSummaryProvider'a paralel
    // ortogonal pipeline. zai config'i IAiSettingsProvider'dan okur (Provider="zai" olan ilk
    // kayıt). Yoksa graceful fail.
    //
    // GLM-4V multipart content array kullanır: system role image+text birlikte kabul edilmiyor.
    // SystemPrompt user içeriğine ilk text element olarak prepend edilir.
    public sealed class ZaiVisionProvider : IAiVisionProvider
    {
        private const string DefaultVisionModel = "glm-4.5v";

        private readonly IHttpClientFactory _httpFactory;
        private readonly IAiSettingsProvider _settings;
        private readonly ILogger<ZaiVisionProvider> _logger;

        public ZaiVisionProvider(
            IHttpClientFactory httpFactory,
            IAiSettingsProvider settings,
            ILogger<ZaiVisionProvider> logger)
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
            var zaiCfg = configs.FirstOrDefault(c => string.Equals(c.Provider, "zai", StringComparison.OrdinalIgnoreCase));
            if (zaiCfg is null)
            {
                _logger.LogWarning("ZaiVisionProvider: Aktif 'zai' sağlayıcı konfigürasyonu bulunamadı.");
                return new AiSummaryResult(false, null, "z.ai vision için aktif zai sağlayıcısı yok.");
            }

            var model = request.OverrideModel ?? DefaultVisionModel;
            var temperature = Math.Clamp(request.OverrideTemperature ?? zaiCfg.Temperature, 0.0, 1.0);
            var maxTokens = Math.Max(request.OverrideMaxTokens ?? zaiCfg.MaxTokens, 2048);

            // Vision için ayrı named client — Program.cs "ai-vision" 120s timeout.
            // "ai" client'ı runtime'da Timeout set etmek thread-unsafe (paylaşılan instance).
            var http = _httpFactory.CreateClient("ai-vision");

            var baseUrl = (zaiCfg.BaseUrl ?? "https://api.z.ai/api/paas/v4").TrimEnd('/');
            var url = baseUrl + "/chat/completions";

            // Multipart content: önce text (sistem direktifi + kullanıcı promptu birleşik),
            // sonra her görsel için image_url entry
            var contentParts = new List<object>
            {
                new { type = "text", text = request.SystemPrompt + "\n\n" + request.UserPrompt }
            };
            foreach (var b64 in request.Base64Images)
            {
                contentParts.Add(new
                {
                    type = "image_url",
                    image_url = new { url = $"data:{request.MimeType};base64,{b64}" }
                });
            }

            var bodyDict = new Dictionary<string, object?>
            {
                ["model"] = model,
                ["messages"] = new object[]
                {
                    new { role = "user", content = contentParts }
                },
                ["temperature"] = temperature,
                ["thinking"] = new { type = "disabled" },
                ["max_tokens"] = maxTokens
            };
            if (request.RequireJson)
                bodyDict["response_format"] = new { type = "json_object" };

            var bodyJson = JsonSerializer.Serialize(bodyDict);
            _logger.LogInformation("z.ai vision REQUEST → {Url} model={Model} images={Count}",
                url, model, request.Base64Images.Count);

            using var msg = new HttpRequestMessage(HttpMethod.Post, url);
            msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", zaiCfg.ApiKey);
            msg.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");

            try
            {
                using var resp = await http.SendAsync(msg, ct);
                var text = await resp.Content.ReadAsStringAsync(ct);

                _logger.LogInformation("z.ai vision RESPONSE ← {Status}", (int)resp.StatusCode);

                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogWarning("z.ai vision HTTP {Status}: {Body}", (int)resp.StatusCode, Truncate(text, 1000));
                    return new AiSummaryResult(false, null,
                        $"z.ai vision HTTP {(int)resp.StatusCode}: {Truncate(text, 300)}");
                }

                // Schema-aware parse — z.ai bazen 200 + HTML rate-limit veya {"error":...} dönebilir.
                JsonDocument doc;
                try
                {
                    doc = JsonDocument.Parse(text);
                }
                catch (JsonException)
                {
                    _logger.LogWarning("z.ai vision JSON parse fail. Raw={Raw}", Truncate(text, 1000));
                    return new AiSummaryResult(false, null,
                        "AI sağlayıcı beklenmedik yanıt verdi. Lütfen tekrar deneyin.");
                }

                using (doc)
                {
                    var root = doc.RootElement;

                    if (!root.TryGetProperty("choices", out var choices)
                        || choices.ValueKind != JsonValueKind.Array
                        || choices.GetArrayLength() == 0)
                    {
                        _logger.LogWarning("z.ai vision response 'choices' eksik veya boş. Raw={Raw}", Truncate(text, 1000));
                        return new AiSummaryResult(false, null,
                            "AI sağlayıcı eksik yanıt döndü (choices yok). Lütfen tekrar deneyin.");
                    }

                    var firstChoice = choices[0];
                    if (!firstChoice.TryGetProperty("message", out var message)
                        || !message.TryGetProperty("content", out var contentEl))
                    {
                        _logger.LogWarning("z.ai vision response message/content eksik. Raw={Raw}", Truncate(text, 1000));
                        return new AiSummaryResult(false, null,
                            "AI sağlayıcı eksik yanıt döndü (content yok). Lütfen tekrar deneyin.");
                    }

                    var content = contentEl.ValueKind == JsonValueKind.String ? contentEl.GetString() ?? "" : "";
                    var finishReason = firstChoice.TryGetProperty("finish_reason", out var fr) ? fr.GetString() : null;

                    if (string.IsNullOrWhiteSpace(content))
                        return new AiSummaryResult(false, null,
                            $"z.ai vision yanıtı boş (finish_reason={finishReason}). Lütfen tekrar deneyin.");

                    int inTok = 0, outTok = 0;
                    if (root.TryGetProperty("usage", out var u))
                    {
                        if (u.TryGetProperty("prompt_tokens", out var pt) && pt.TryGetInt32(out var p)) inTok = p;
                        if (u.TryGetProperty("completion_tokens", out var ot) && ot.TryGetInt32(out var o)) outTok = o;
                    }

                    return new AiSummaryResult(true, content, null, inTok, outTok, model);
                }
            }
            catch (TaskCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (TaskCanceledException tex)
            {
                // Timeout (CancellationToken'dan değil, HttpClient.Timeout)
                _logger.LogWarning(tex, "z.ai vision timeout");
                return new AiSummaryResult(false, null,
                    "AI sağlayıcı zaman aşımına uğradı. Sözleşme görseli çok büyük olabilir; daha küçük bir kopya deneyin.");
            }
            catch (Exception ex)
            {
                // Beklenmedik hata — ex.Message GÖSTERME (security-principles.md kural #7)
                _logger.LogError(ex, "z.ai vision çağrısı başarısız");
                return new AiSummaryResult(false, null,
                    "AI sağlayıcısına ulaşılamadı. Lütfen daha sonra tekrar deneyin.");
            }
        }

        private static string Truncate(string s, int max) =>
            string.IsNullOrEmpty(s) || s.Length <= max ? s : s[..max] + "…";
    }
}
