using System.Net;
using System.Text;
using System.Text.Json;
using Mosaik.Core.Ai;
using Mosaik.Services.Ai;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mosaik.Tests;

public class MultiProviderVisionProviderTests
{
    // ---- Fakes ----

    private sealed class FakeSettingsProvider(IReadOnlyList<AiConfig> configs) : IAiSettingsProvider
    {
        public Task<IReadOnlyList<AiConfig>> GetActiveOrderedAsync(CancellationToken ct = default)
            => Task.FromResult(configs);
        public Task<AiConfig?> GetActiveAsync(CancellationToken ct = default)
            => Task.FromResult(configs.Count > 0 ? configs[0] : (AiConfig?)null);
    }

    private sealed class FakeHttpClientFactory(params HttpResponseMessage[] responses) : IHttpClientFactory
    {
        private int _index;
        public HttpClient CreateClient(string name)
            => new HttpClient(new FakeHandler(responses[Math.Min(_index++, responses.Length - 1)]));
    }

    private sealed class FakeHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(response);
    }

    private static AiConfig MakeConfig(string provider, int id = 1) =>
        new AiConfig(Id: id, Provider: provider, ApiKey: "key", Model: "model",
            MaxTokens: 1024, Temperature: 0.3, BaseUrl: null);

    private static AiVisionRequest MakeRequest(string provider = "openai") =>
        new AiVisionRequest("sys", "user", new[] { "base64data" }, "image/jpeg");

    private static HttpResponseMessage OkJson(object body) =>
        new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };

    private static object OpenAiSuccessBody(string content = "extracted text") => new
    {
        choices = new[] { new { message = new { content } } },
        usage = new { prompt_tokens = 100, completion_tokens = 50 }
    };

    private static object GeminiSuccessBody(string content = "gemini text") => new
    {
        candidates = new[]
        {
            new { content = new { parts = new[] { new { text = content } } } }
        },
        usageMetadata = new { promptTokenCount = 80, candidatesTokenCount = 40 }
    };

    // ---- Guard clause tests ----

    [Fact]
    public async Task NoImages_ReturnsFail()
    {
        var sut = new MultiProviderVisionProvider(
            new FakeHttpClientFactory(),
            new FakeSettingsProvider(Array.Empty<AiConfig>()),
            NullLogger<MultiProviderVisionProvider>.Instance);

        var result = await sut.GenerateFromImagesAsync(
            new AiVisionRequest("sys", "user", Array.Empty<string>(), "image/jpeg"));

        Assert.False(result.IsSuccess);
        Assert.Contains("Görsel sağlanmadı", result.Error);
    }

    [Fact]
    public async Task NoVisionCapableConfigs_ReturnsFail()
    {
        var configs = new[] { MakeConfig("groq"), MakeConfig("ollama") };
        var sut = new MultiProviderVisionProvider(
            new FakeHttpClientFactory(),
            new FakeSettingsProvider(configs),
            NullLogger<MultiProviderVisionProvider>.Instance);

        var result = await sut.GenerateFromImagesAsync(MakeRequest());

        Assert.False(result.IsSuccess);
        Assert.Contains("Vision destekleyen", result.Error);
    }

    [Fact]
    public async Task GroqAndOllamaFiltered_OnlyVisionCapableAttempted()
    {
        // groq + openai configured; groq filtered → only openai tried → success
        var configs = new[] { MakeConfig("groq", 1), MakeConfig("openai", 2) };
        var factory = new FakeHttpClientFactory(OkJson(OpenAiSuccessBody("ok")));
        var sut = new MultiProviderVisionProvider(factory,
            new FakeSettingsProvider(configs),
            NullLogger<MultiProviderVisionProvider>.Instance);

        var result = await sut.GenerateFromImagesAsync(MakeRequest());

        Assert.True(result.IsSuccess);
        Assert.Equal("ok", result.RawJson);
    }

    // ---- OpenAI-compatible path ----

    [Fact]
    public async Task OpenAi_SuccessResponse_MapsTokensAndContent()
    {
        var configs = new[] { MakeConfig("openai") };
        var factory = new FakeHttpClientFactory(OkJson(OpenAiSuccessBody("contract data")));
        var sut = new MultiProviderVisionProvider(factory,
            new FakeSettingsProvider(configs),
            NullLogger<MultiProviderVisionProvider>.Instance);

        var result = await sut.GenerateFromImagesAsync(MakeRequest("openai"));

        Assert.True(result.IsSuccess);
        Assert.Equal("contract data", result.RawJson);
        Assert.Equal(100, result.InputTokens);
        Assert.Equal(50, result.OutputTokens);
        Assert.Equal("model", result.ModelUsed);
    }

    [Fact]
    public async Task OpenAi_HttpError_ReturnsFail()
    {
        var configs = new[] { MakeConfig("openai") };
        var factory = new FakeHttpClientFactory(
            new HttpResponseMessage(HttpStatusCode.TooManyRequests) { Content = new StringContent("rate limit") });
        var sut = new MultiProviderVisionProvider(factory,
            new FakeSettingsProvider(configs),
            NullLogger<MultiProviderVisionProvider>.Instance);

        var result = await sut.GenerateFromImagesAsync(MakeRequest());

        Assert.False(result.IsSuccess);
        Assert.Contains("429", result.Error);
    }

    [Fact]
    public async Task OpenAi_EmptyContent_ReturnsFail()
    {
        var configs = new[] { MakeConfig("openai") };
        var body = new { choices = new[] { new { message = new { content = "" } } } };
        var factory = new FakeHttpClientFactory(OkJson(body));
        var sut = new MultiProviderVisionProvider(factory,
            new FakeSettingsProvider(configs),
            NullLogger<MultiProviderVisionProvider>.Instance);

        var result = await sut.GenerateFromImagesAsync(MakeRequest());

        Assert.False(result.IsSuccess);
        Assert.Contains("content boş", result.Error);
    }

    // ---- Gemini path ----

    [Fact]
    public async Task Gemini_SuccessResponse_MapsTokensAndContent()
    {
        var configs = new[] { MakeConfig("gemini") };
        var factory = new FakeHttpClientFactory(OkJson(GeminiSuccessBody("gemini result")));
        var sut = new MultiProviderVisionProvider(factory,
            new FakeSettingsProvider(configs),
            NullLogger<MultiProviderVisionProvider>.Instance);

        var result = await sut.GenerateFromImagesAsync(MakeRequest("gemini"));

        Assert.True(result.IsSuccess);
        Assert.Equal("gemini result", result.RawJson);
        Assert.Equal(80, result.InputTokens);
        Assert.Equal(40, result.OutputTokens);
    }

    [Fact]
    public async Task Gemini_HttpError_ReturnsFail()
    {
        var configs = new[] { MakeConfig("gemini") };
        var factory = new FakeHttpClientFactory(
            new HttpResponseMessage(HttpStatusCode.Forbidden) { Content = new StringContent("api key invalid") });
        var sut = new MultiProviderVisionProvider(factory,
            new FakeSettingsProvider(configs),
            NullLogger<MultiProviderVisionProvider>.Instance);

        var result = await sut.GenerateFromImagesAsync(MakeRequest());

        Assert.False(result.IsSuccess);
        Assert.Contains("403", result.Error);
    }

    // ---- Fallback chain ----

    [Fact]
    public async Task Fallback_FirstFails_SecondSucceeds()
    {
        var configs = new[] { MakeConfig("openai", 1), MakeConfig("grok", 2) };
        var factory = new FakeHttpClientFactory(
            new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("err") },
            OkJson(OpenAiSuccessBody("grok answered")));
        var sut = new MultiProviderVisionProvider(factory,
            new FakeSettingsProvider(configs),
            NullLogger<MultiProviderVisionProvider>.Instance);

        var result = await sut.GenerateFromImagesAsync(MakeRequest());

        Assert.True(result.IsSuccess);
        Assert.Equal("grok answered", result.RawJson);
    }

    [Fact]
    public async Task Fallback_AllFail_ReturnsLastError()
    {
        var configs = new[] { MakeConfig("openai", 1), MakeConfig("grok", 2) };
        var factory = new FakeHttpClientFactory(
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) { Content = new StringContent("down") },
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) { Content = new StringContent("down") });
        var sut = new MultiProviderVisionProvider(factory,
            new FakeSettingsProvider(configs),
            NullLogger<MultiProviderVisionProvider>.Instance);

        var result = await sut.GenerateFromImagesAsync(MakeRequest());

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
    }

    // ---- Cancellation ----

    [Fact]
    public async Task Cancellation_Propagates()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var configs = new[] { MakeConfig("openai") };
        var sut = new MultiProviderVisionProvider(
            new FakeHttpClientFactory(OkJson(OpenAiSuccessBody())),
            new FakeSettingsProvider(configs),
            NullLogger<MultiProviderVisionProvider>.Instance);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => sut.GenerateFromImagesAsync(MakeRequest(), cts.Token));
    }
}
