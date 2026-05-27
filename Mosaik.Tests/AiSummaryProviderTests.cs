using System.Net;
using System.Text;
using System.Text.Json;
using Mosaik.Core.Ai;
using Mosaik.Core.AI.Local;
using Mosaik.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mosaik.Tests;

// D-04 (2026-05-22): AiSummaryProvider QA testleri
// Kapsam: fallback zinciri, budget enforcement, Gemini path, OpenAI path, cancellation.
// Not: s_dailyUsage static dict — test isolation için unique ID per test.
public class AiSummaryProviderTests
{
    // ---- ID counter (test isolation) ----
    private static int _idCounter = 10000;
    private static int NextId() => System.Threading.Interlocked.Increment(ref _idCounter);

    // ---- Fakes ----

    private sealed class FakeSettings(IReadOnlyList<AiConfig> configs) : IAiSettingsProvider
    {
        public Task<IReadOnlyList<AiConfig>> GetActiveOrderedAsync(CancellationToken ct = default)
            => Task.FromResult(configs);
        public Task<AiConfig?> GetActiveAsync(CancellationToken ct = default)
            => Task.FromResult(configs.Count > 0 ? configs[0] : (AiConfig?)null);
    }

    private sealed class FakeFactory(params HttpResponseMessage[] responses) : IHttpClientFactory
    {
        private int _i;
        public HttpClient CreateClient(string name)
            => new HttpClient(new FakeHandler(responses[Math.Min(_i++, responses.Length - 1)]));
    }

    private sealed class FakeHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct)
            => Task.FromResult(response);
    }

    // ---- Response builders ----

    private static HttpResponseMessage OpenAiOk(string content = "answer", int inTok = 100, int outTok = 50) =>
        new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(new
            {
                choices = new[] { new { message = new { content } } },
                usage = new { prompt_tokens = inTok, completion_tokens = outTok }
            }), Encoding.UTF8, "application/json")
        };

    private static HttpResponseMessage GeminiOk(string text = "gemini answer", int inTok = 80, int outTok = 40) =>
        new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(new
            {
                candidates = new[] { new { content = new { parts = new[] { new { text } } } } },
                usageMetadata = new { promptTokenCount = inTok, candidatesTokenCount = outTok }
            }), Encoding.UTF8, "application/json")
        };

    private static HttpResponseMessage HttpError(HttpStatusCode code = HttpStatusCode.InternalServerError) =>
        new HttpResponseMessage(code) { Content = new StringContent("error") };

    private static AiConfig Cfg(string provider = "openai", int? budget = null) =>
        new AiConfig(Id: NextId(), Provider: provider, ApiKey: "k",
            Model: "m", MaxTokens: 512, Temperature: 0.3, BaseUrl: null,
            DailyTokenBudget: budget);

    private static AiSummaryProvider Build(FakeSettings settings, FakeFactory factory, ILlmRunner? local = null) =>
        new AiSummaryProvider(settings, factory, NullLogger<AiSummaryProvider>.Instance, local);

    // Fake local LLM runner — sabit cevap döner, IsReady kontrol edilebilir.
    private sealed class FakeLocalRunner(bool ready, string answer, string? error = null) : ILlmRunner
    {
        public bool IsReady => ready;
        public Task<LlmRunResult> RunAsync(string systemPrompt, string userPrompt, LlmRunOptions options, CancellationToken ct = default)
            => Task.FromResult(error is null
                ? new LlmRunResult(true, answer, TokensIn: 42, TokensOut: 17)
                : new LlmRunResult(false, null, 0, 0, error));
    }

    private static AiRequest Req(string purpose = "test") =>
        new AiRequest("sys", "user", Purpose: purpose);

    // ---- Guard clause ----

    [Fact]
    public async Task NoConfigs_ReturnsFail()
    {
        var sut = Build(new FakeSettings([]), new FakeFactory());
        var r = await sut.GenerateAsync(Req());
        Assert.False(r.IsSuccess);
        Assert.Contains("API key", r.Error);
    }

    // ---- OpenAI-compatible path ----

    [Fact]
    public async Task OpenAi_Success_MapsAllFields()
    {
        var sut = Build(new FakeSettings([Cfg("openai")]), new FakeFactory(OpenAiOk("result text", 200, 80)));
        var r = await sut.GenerateAsync(Req());
        Assert.True(r.IsSuccess);
        Assert.Equal("result text", r.RawJson);
        Assert.Equal(200, r.InputTokens);
        Assert.Equal(80, r.OutputTokens);
        Assert.Equal("m", r.ModelUsed);
    }

    [Fact]
    public async Task OpenAi_HttpError_ReturnsFail()
    {
        var sut = Build(new FakeSettings([Cfg()]), new FakeFactory(HttpError(HttpStatusCode.BadGateway)));
        var r = await sut.GenerateAsync(Req());
        Assert.False(r.IsSuccess);
        Assert.Contains("502", r.Error);
    }

    [Fact]
    public async Task OpenAi_EmptyContent_ReturnsFail()
    {
        var resp = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"choices":[{"message":{"content":""}}]}""",
                Encoding.UTF8, "application/json")
        };
        var sut = Build(new FakeSettings([Cfg()]), new FakeFactory(resp));
        var r = await sut.GenerateAsync(Req());
        Assert.False(r.IsSuccess);
    }

    // ---- Gemini path ----

    [Fact]
    public async Task Gemini_Success_MapsAllFields()
    {
        var sut = Build(new FakeSettings([Cfg("gemini")]), new FakeFactory(GeminiOk("gemini text", 90, 45)));
        var r = await sut.GenerateAsync(Req());
        Assert.True(r.IsSuccess);
        Assert.Equal("gemini text", r.RawJson);
        Assert.Equal(90, r.InputTokens);
        Assert.Equal(45, r.OutputTokens);
    }

    [Fact]
    public async Task Gemini_HttpError_ReturnsFail()
    {
        var sut = Build(new FakeSettings([Cfg("gemini")]), new FakeFactory(HttpError(HttpStatusCode.Unauthorized)));
        var r = await sut.GenerateAsync(Req());
        Assert.False(r.IsSuccess);
        Assert.Contains("401", r.Error);
    }

    // ---- Fallback chain ----

    [Fact]
    public async Task Fallback_FirstFails_SecondSucceeds()
    {
        var configs = new[] { Cfg("openai"), Cfg("groq") };
        var sut = Build(new FakeSettings(configs),
            new FakeFactory(HttpError(), OpenAiOk("fallback answer")));
        var r = await sut.GenerateAsync(Req());
        Assert.True(r.IsSuccess);
        Assert.Equal("fallback answer", r.RawJson);
    }

    [Fact]
    public async Task Fallback_AllFail_ReturnsLastError()
    {
        var configs = new[] { Cfg("openai"), Cfg("groq") };
        var sut = Build(new FakeSettings(configs), new FakeFactory(HttpError(), HttpError()));
        var r = await sut.GenerateAsync(Req());
        Assert.False(r.IsSuccess);
        Assert.NotNull(r.Error);
    }

    [Fact]
    public async Task Fallback_NetworkException_ContinuesToNext()
    {
        var throwingHandler = new ThrowingHandler(new HttpRequestException("network down"));
        var factory = new FactoryWithHandlers(
            throwingHandler,
            new FakeHandler(OpenAiOk("network fallback")));
        var configs = new[] { Cfg("openai"), Cfg("groq") };
        var sut = new AiSummaryProvider(new FakeSettings(configs), factory, NullLogger<AiSummaryProvider>.Instance);
        var r = await sut.GenerateAsync(Req());
        Assert.True(r.IsSuccess);
        Assert.Equal("network fallback", r.RawJson);
    }

    // ---- Model / temperature override ----

    [Fact]
    public async Task OverrideModel_SentToProvider()
    {
        string? capturedBody = null;
        var handler = new CapturingHandler(OpenAiOk(), body => capturedBody = body);
        var factory = new FactoryWithHandlers(handler);
        var sut = new AiSummaryProvider(new FakeSettings([Cfg()]), factory, NullLogger<AiSummaryProvider>.Instance);

        await sut.GenerateAsync(new AiRequest("sys", "user", OverrideModel: "gpt-4o-override"));

        Assert.NotNull(capturedBody);
        Assert.Contains("gpt-4o-override", capturedBody);
    }

    // ---- Daily token budget (D-02-5) ----

    [Fact]
    public async Task Budget_FirstCallSucceeds_AccumulatesTokens_SecondCallSkips()
    {
        // Two configs: id1 (budget=10), id2 (unlimited)
        var id1 = NextId();
        var cfg1 = new AiConfig(Id: id1, Provider: "openai", ApiKey: "k", Model: "m",
            MaxTokens: 512, Temperature: 0.3, BaseUrl: null, DailyTokenBudget: 10);
        var cfg2 = new AiConfig(Id: NextId(), Provider: "groq", ApiKey: "k", Model: "m",
            MaxTokens: 512, Temperature: 0.3, BaseUrl: null, DailyTokenBudget: null);

        // First sut: only cfg1 configured → call fills budget (100 tokens > 10)
        var sut1 = new AiSummaryProvider(new FakeSettings([cfg1]),
            new FakeFactory(OpenAiOk("first", inTok: 60, outTok: 60)),
            NullLogger<AiSummaryProvider>.Instance);
        var r1 = await sut1.GenerateAsync(Req());
        Assert.True(r1.IsSuccess); // first call succeeds

        // Second sut: cfg1 + cfg2 — cfg1 budget exhausted (120 >= 10), falls back to cfg2
        var sut2 = new AiSummaryProvider(new FakeSettings([cfg1, cfg2]),
            new FakeFactory(OpenAiOk("second")),
            NullLogger<AiSummaryProvider>.Instance);
        var r2 = await sut2.GenerateAsync(Req());
        Assert.True(r2.IsSuccess);
        Assert.Equal("second", r2.RawJson);
    }

    [Fact]
    public async Task Budget_NullBudget_NeverSkipped()
    {
        // Provider with null budget always attempted regardless of prior usage
        var cfg = new AiConfig(Id: NextId(), Provider: "openai", ApiKey: "k", Model: "m",
            MaxTokens: 512, Temperature: 0.3, BaseUrl: null, DailyTokenBudget: null);
        var sut = Build(new FakeSettings([cfg]), new FakeFactory(OpenAiOk()));
        var r = await sut.GenerateAsync(Req());
        Assert.True(r.IsSuccess);
    }

    // ---- Local (yerleşik AI — LLamaSharp + Qwen) ----

    [Fact]
    public async Task Local_RunnerReady_ReturnsSuccess()
    {
        var runner = new FakeLocalRunner(ready: true, answer: "Yerleşik yanıt.");
        var sut = Build(new FakeSettings([Cfg("local")]), new FakeFactory(), runner);
        var r = await sut.GenerateAsync(new AiRequest("sys", "user", RequireJson: false));
        Assert.True(r.IsSuccess);
        Assert.Equal("Yerleşik yanıt.", r.RawJson);
        Assert.Equal(42, r.InputTokens);
        Assert.Equal(17, r.OutputTokens);
        Assert.Equal("qwen-2.5-3b-q4", r.ModelUsed);
    }

    [Fact]
    public async Task Local_RunnerNotReady_FallsBackToNextProvider()
    {
        var runner = new FakeLocalRunner(ready: false, answer: "");
        var configs = new[] { Cfg("local"), Cfg("openai") };
        var sut = Build(new FakeSettings(configs), new FakeFactory(OpenAiOk("cloud cevap")), runner);
        var r = await sut.GenerateAsync(Req());
        Assert.True(r.IsSuccess);
        Assert.Equal("cloud cevap", r.RawJson);
    }

    [Fact]
    public async Task Local_RequireJson_ExtractsJsonBlock()
    {
        var raw = "Açıklama metni... ```json\n{\"summary\":\"abc\"}\n``` ek metin";
        var runner = new FakeLocalRunner(ready: true, answer: raw);
        var sut = Build(new FakeSettings([Cfg("local")]), new FakeFactory(), runner);
        var r = await sut.GenerateAsync(new AiRequest("sys", "user", RequireJson: true));
        Assert.True(r.IsSuccess);
        Assert.Equal("{\"summary\":\"abc\"}", r.RawJson);
    }

    // ---- Cancellation ----

    [Fact]
    public async Task Cancellation_DuringHttpCall_Propagates()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        // Handler that respects cancellation (mimics real HttpClient behaviour)
        var handler = new CancelCheckHandler(OpenAiOk());
        var factory = new FactoryWithHandlers(handler);
        var sut = new AiSummaryProvider(new FakeSettings([Cfg()]), factory, NullLogger<AiSummaryProvider>.Instance);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => sut.GenerateAsync(Req(), cts.Token));
    }

    // ---- Helpers ----

    private sealed class CancelCheckHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(response);
        }
    }

    private sealed class ThrowingHandler(Exception ex) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct)
            => Task.FromException<HttpResponseMessage>(ex);
    }

    private sealed class CapturingHandler(HttpResponseMessage response, Action<string> onBody) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct)
        {
            onBody(await r.Content!.ReadAsStringAsync(ct));
            return response;
        }
    }

    private sealed class FactoryWithHandlers(params HttpMessageHandler[] handlers) : IHttpClientFactory
    {
        private int _i;
        public HttpClient CreateClient(string name)
            => new HttpClient(handlers[Math.Min(_i++, handlers.Length - 1)]);
    }
}
