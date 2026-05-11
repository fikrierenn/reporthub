using Mosaik.Core.Ai;
using Mosaik.Services.Ai;

namespace Mosaik.Tests;

public class FallbackLlmServiceTests
{
    // ---- Fakes ----

    private sealed class FakeAiSummaryProvider(AiSummaryResult response) : IAiSummaryProvider
    {
        public Task<AiSummaryResult> GenerateAsync(AiRequest request, CancellationToken ct = default)
            => Task.FromResult(response);
    }

    // ---- Tests ----

    [Fact]
    public async Task GenerateAsync_Success_MapsAllFields()
    {
        var inner = new FakeAiSummaryProvider(
            new AiSummaryResult(true, """{"ok":1}""", null, 100, 50, "gemini-2.0-flash"));
        var sut = new FallbackLlmService(inner);

        var result = await sut.GenerateAsync(new LlmRequest("sys", "user"));

        Assert.True(result.IsSuccess);
        Assert.Equal("""{"ok":1}""", result.Content);
        Assert.Null(result.Error);
        Assert.Equal(100, result.InputTokens);
        Assert.Equal(50, result.OutputTokens);
        Assert.Equal("gemini-2.0-flash", result.ModelUsed);
    }

    [Fact]
    public async Task GenerateAsync_Failure_MapsError()
    {
        var inner = new FakeAiSummaryProvider(
            new AiSummaryResult(false, null, "Tüm provider'lar başarısız.", 0, 0, null));
        var sut = new FallbackLlmService(inner);

        var result = await sut.GenerateAsync(new LlmRequest("sys", "user"));

        Assert.False(result.IsSuccess);
        Assert.Equal("Tüm provider'lar başarısız.", result.Error);
        Assert.Null(result.Content);
    }

    [Fact]
    public async Task GenerateAsync_PassesPurpose()
    {
        string? capturedPurpose = null;
        var inner = new CapturingFakeProvider(req => { capturedPurpose = req.Purpose; });
        var sut = new FallbackLlmService(inner);

        await sut.GenerateAsync(new LlmRequest("sys", "user", Purpose: "contract_extract"));

        Assert.Equal("contract_extract", capturedPurpose);
    }

    [Fact]
    public async Task GenerateAsync_PassesRequireJsonFalse()
    {
        bool? capturedRequireJson = null;
        var inner = new CapturingFakeProvider(req => { capturedRequireJson = req.RequireJson; });
        var sut = new FallbackLlmService(inner);

        await sut.GenerateAsync(new LlmRequest("sys", "user", RequireJson: false));

        Assert.Equal(false, capturedRequireJson);
    }

    [Fact]
    public async Task GenerateAsync_CancellationPropagates()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        CancellationToken? captured = null;
        var inner = new CapturingFakeProvider(_ => { }, ct => { captured = ct; });
        var sut = new FallbackLlmService(inner);

        await sut.GenerateAsync(new LlmRequest("sys", "user"), cts.Token);

        Assert.True(captured?.IsCancellationRequested);
    }

    // ---- Capture helper ----

    private sealed class CapturingFakeProvider(
        Action<AiRequest> onRequest,
        Action<CancellationToken>? onCt = null) : IAiSummaryProvider
    {
        public Task<AiSummaryResult> GenerateAsync(AiRequest request, CancellationToken ct = default)
        {
            onRequest(request);
            onCt?.Invoke(ct);
            return Task.FromResult(new AiSummaryResult(true, "{}", null));
        }
    }
}
