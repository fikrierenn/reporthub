using Mosaik.Core.Ai;
using Mosaik.Services.Ai;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mosaik.Tests;

public class DocumentInsightServiceTests
{
    private sealed class FakeAi(bool success, string? rawJson, string? error = null) : IAiSummaryProvider
    {
        public AiRequest? LastRequest { get; private set; }

        public Task<AiSummaryResult> GenerateAsync(AiRequest request, CancellationToken ct = default)
        {
            LastRequest = request;
            return Task.FromResult(new AiSummaryResult(success, rawJson, error, 100, 50, "qwen-2.5-3b-q4"));
        }
    }

    private static DocumentInsightService Build(IAiSummaryProvider ai) =>
        new(ai, NullLogger<DocumentInsightService>.Instance);

    private const string ValidJson = """
        {"summary":"Test özet.","documentType":"Sözleşme","year":2025,"department":"Hukuk","tags":["kira","istanbul"]}
        """;

    [Fact]
    public async Task EmptyText_ReturnsNull()
    {
        var ai = new FakeAi(true, ValidJson);
        var sut = Build(ai);

        var r = await sut.AnalyzeAsync("", "test.pdf");

        Assert.Null(r);
        Assert.Null(ai.LastRequest); // AI hiç çağrılmadı
    }

    [Fact]
    public async Task LongText_TruncatedTo8000Chars()
    {
        var longText = new string('A', 20000);
        var ai = new FakeAi(true, ValidJson);
        var sut = Build(ai);

        await sut.AnalyzeAsync(longText, "doc.pdf");

        Assert.NotNull(ai.LastRequest);
        // UserPrompt'ta metin kısmı 8000 chars'a kırpılmış + "[... metin kırpıldı ...]" eklenmeli
        Assert.Contains("[... metin kırpıldı ...]", ai.LastRequest!.UserPrompt);
        // UserPrompt 8000 + overhead'den fazla uzun olmamalı (~9000 chars güvenli üst sınır)
        Assert.True(ai.LastRequest.UserPrompt.Length < 9000);
    }

    [Fact]
    public async Task ValidJsonResponse_ParsesAllFields()
    {
        var ai = new FakeAi(true, ValidJson);
        var sut = Build(ai);

        var r = await sut.AnalyzeAsync("Sözleşme metni.", "kira.pdf");

        Assert.NotNull(r);
        Assert.Equal("Test özet.", r!.Summary);
        Assert.Contains("Sözleşme", r.TagsJson);
        Assert.Contains("2025", r.TagsJson);
        Assert.Contains("Hukuk", r.TagsJson);
        Assert.Equal("qwen-2.5-3b-q4", r.ModelUsed);
    }

    [Fact]
    public async Task AiFails_ReturnsNull()
    {
        var ai = new FakeAi(false, null, "model hatası");
        var sut = Build(ai);

        var r = await sut.AnalyzeAsync("Metin.", "doc.pdf");

        Assert.Null(r);
    }

    [Fact]
    public async Task MalformedJson_ReturnsNull()
    {
        var ai = new FakeAi(true, "bu geçerli json değil {{{");
        var sut = Build(ai);

        var r = await sut.AnalyzeAsync("Metin.", "doc.pdf");

        Assert.Null(r);
    }

    [Fact]
    public async Task FileNamePassedToPrompt()
    {
        var ai = new FakeAi(true, ValidJson);
        var sut = Build(ai);

        await sut.AnalyzeAsync("Metin.", "sozlesme-2025.pdf");

        Assert.Contains("sozlesme-2025.pdf", ai.LastRequest!.UserPrompt);
    }

    [Fact]
    public async Task ShortText_NotTruncated_NoCutMessage()
    {
        var ai = new FakeAi(true, ValidJson);
        var sut = Build(ai);

        await sut.AnalyzeAsync("Kısa metin.", "doc.pdf");

        Assert.DoesNotContain("[... metin kırpıldı ...]", ai.LastRequest!.UserPrompt);
    }
}
