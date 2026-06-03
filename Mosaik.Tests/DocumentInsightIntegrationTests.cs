using Mosaik.Core.Ai;
using Mosaik.Core.AI.Local;
using Mosaik.Services;
using Mosaik.Services.Ai;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;

namespace Mosaik.Tests;

// Gerçek LlamaSharp + Qwen 2.5 3B Q4 modeli gerektirir.
// Manuel çalıştırma: dotnet test --filter "Category=LocalIntegration"
// Bağımlılık: Mosaik/App_Data/models/llm/qwen25-3b-instruct-q4_k_m.gguf
[Trait("Category", "LocalIntegration")]
public class DocumentInsightIntegrationTests
{
    private const string ModelRoot = @"D:\Dev\reporthub\Mosaik";

    private const string SampleContract = """
        KİRA SÖZLEŞMESİ

        Bu sözleşme, 01.03.2025 tarihinde İSTANBUL'da aşağıdaki taraflar arasında akdedilmiştir.

        KİRAYA VEREN: Ahmet Yılmaz (TC: 12345678901)
        KİRACİ: XYZ Teknoloji A.Ş. (Vergi No: 1234567890)

        KİRALANAN: İstanbul, Şişli ilçesi, Büyükdere Caddesi No:100/A adresinde bulunan
        200 m² ofis alanı.

        SÜRE: 1 yıl (01.03.2025 - 01.03.2026)
        AYLIK KİRA: 25.000 TL + KDV
        DEPOZITO: 75.000 TL (3 aylık kira bedeli)

        TARAFLARIN YÜKÜMLÜLÜKLERİ:
        1. Kiracı, kira bedelini her ayın 1. günü ödemekle yükümlüdür.
        2. Kiracı, kiralanan yeri amacına uygun olarak kullanacaktır.
        3. Kiraya veren, kiralananda gerekli onarımları yapmakla yükümlüdür.
        4. Sözleşme süresi sonunda kiracı, taşınmazı teslim edecektir.

        Bu sözleşme 2 nüsha olarak imzalanmış ve taraflarca kabul edilmiştir.
        """;

    [Fact]
    public async Task Qwen_DocumentInsight_ProducesValidJson()
    {
        var runner = new LlamaSharpRunner(NullLogger<LlamaSharpRunner>.Instance, ModelRoot);
        if (!runner.IsReady)
        {
            // Model dosyası yoksa test geçilsin (CI ortamı)
            return;
        }

        var settings = new FakeLocalSettings();
        var factory = new FakeHttpFactory();
        var provider = new AiSummaryProvider(settings, factory, NullLogger<AiSummaryProvider>.Instance, runner);
        var sut = new DocumentInsightService(provider, NullLogger<DocumentInsightService>.Instance);

        var result = await sut.AnalyzeAsync(SampleContract, "kira-sozlesmesi-2025.pdf");

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result!.Summary), "Summary boş");
        Assert.False(string.IsNullOrWhiteSpace(result.TagsJson), "TagsJson boş");

        // JSON parse edilebilir mi?
        using var doc = JsonDocument.Parse(result.TagsJson!);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("documentType", out var dt), "documentType yok");
        Assert.Equal("Sözleşme", dt.GetString());

        Assert.True(root.TryGetProperty("year", out var yr), "year yok");
        Assert.Equal(2025, yr.GetInt32());

        Assert.True(root.TryGetProperty("tags", out var tags), "tags yok");
        Assert.True(tags.GetArrayLength() >= 2, "tags en az 2 adet olmalı");

        // Model adı
        Assert.Equal("qwen-2.5-3b-q4", result.ModelUsed);
        // Token kullanımı raporlandı mı
        Assert.True(result.InputTokens > 0);
        Assert.True(result.OutputTokens > 0);
    }

    // ---- Fakes ----

    private sealed class FakeLocalSettings : IAiSettingsProvider
    {
        private static readonly AiConfig LocalCfg = new(
            Id: 99, Provider: "local", ApiKey: "n/a", Model: "qwen-2.5-3b-q4",
            MaxTokens: 512, Temperature: 0.3, BaseUrl: null,
            DailyTokenBudget: null);

        public Task<IReadOnlyList<AiConfig>> GetActiveOrderedAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<AiConfig>>([LocalCfg]);

        public Task<AiConfig?> GetActiveAsync(CancellationToken ct = default)
            => Task.FromResult<AiConfig?>(LocalCfg);
    }

    private sealed class FakeHttpFactory : System.Net.Http.IHttpClientFactory
    {
        public System.Net.Http.HttpClient CreateClient(string name) => new();
    }
}
