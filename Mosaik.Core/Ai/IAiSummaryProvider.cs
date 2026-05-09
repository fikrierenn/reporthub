namespace Mosaik.Core.Ai
{
    // Plan 17 Faz F (Plan 16.5 Faz C+D'ye evrilecek shared AI Core).
    // vNext modüller (Circular, Document, vb.) AI özet/analiz ihtiyacında
    // bu interface'i inject eder. Host (Mosaik) IAiSettingsProvider üzerinden
    // runtime config sağlar (Admin UI'dan yönetilir).
    public interface IAiSettingsProvider
    {
        // Aktif config'leri sırayla döndür: IsPrimary önce, sonra Priority asc.
        // Liste boşsa: hiç enabled config yok.
        Task<IReadOnlyList<AiConfig>> GetActiveOrderedAsync(CancellationToken ct = default);

        // Geriye uyumluluk: ilk config (= primary) ya da null
        Task<AiConfig?> GetActiveAsync(CancellationToken ct = default);
    }

    // AI metin üretici — single entry point. Modül kendi prompt'unu gönderir,
    // opsiyonel olarak global ayarları override edebilir (model/temperature/maxTokens).
    public interface IAiSummaryProvider
    {
        Task<AiSummaryResult> GenerateAsync(AiRequest request, CancellationToken ct = default);
    }

    // Active AI ayarları — DB'den okunur, runtime cache'lenir (1dk).
    // Provider değerleri: "groq", "grok", "openai", "openrouter", "gemini", "ollama"
    public sealed record AiConfig(
        string Provider,
        string ApiKey,
        string Model,
        int MaxTokens,
        double Temperature,
        string? BaseUrl);

    // Modülden gelen tek istek nesnesi.
    //  - SystemPrompt + UserPrompt: zorunlu
    //  - Override*: null ise AiConfig'in default'u kullanılır
    //  - RequireJson: provider 'response_format=json_object' bayrağı (varsayılan true)
    //  - Purpose: telemetri için ("circular_summary", "block_categorize" vb)
    public sealed record AiRequest(
        string SystemPrompt,
        string UserPrompt,
        string? OverrideModel = null,
        int? OverrideMaxTokens = null,
        double? OverrideTemperature = null,
        bool RequireJson = true,
        string? Purpose = null);

    // Sonuç. IsSuccess=false ise Error doludur.
    public sealed record AiSummaryResult(
        bool IsSuccess,
        string? RawJson,
        string? Error,
        int InputTokens = 0,
        int OutputTokens = 0,
        string? ModelUsed = null);

    // ---- Vision (görsel) AI ----
    // Plan 25 wizard — taranmış sözleşme görseli z.ai GLM-4V-plus'a yollanır.
    // Text path (IAiSummaryProvider) ile ortogonal: ayrı interface, ayrı request.
    public interface IAiVisionProvider
    {
        Task<AiSummaryResult> GenerateFromImagesAsync(AiVisionRequest request, CancellationToken ct = default);
    }

    // - SystemPrompt: vision modellerinde user içeriğine prepended olarak gider
    //   (GLM-4V system role'ünü image+text içerikle birlikte kabul etmiyor).
    // - Base64Images: data URL bileşenleri ("data:image/jpeg;base64,..." değil, sadece base64 payload)
    // - MimeType: "image/jpeg" | "image/png" | ...
    // - OverrideModel: null ise "glm-4v-plus" varsayılan
    public sealed record AiVisionRequest(
        string SystemPrompt,
        string UserPrompt,
        IReadOnlyList<string> Base64Images,
        string MimeType,
        string? OverrideModel = null,
        int? OverrideMaxTokens = null,
        double? OverrideTemperature = null,
        bool RequireJson = true,
        string? Purpose = null);
}
