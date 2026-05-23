using Microsoft.Extensions.Logging.Abstractions;
using Mosaik.Core.AI.Embed;
using Mosaik.Services.Ai;
using Xunit.Abstractions;

namespace Mosaik.Tests;

// Plan 34.1 Faz 1 A-06 — E5Embedder gerçek ONNX inference smoke.
// Model + tokenizer dosyaları yoksa sessiz skip (LlamaSharpSmokeTest pattern).
// Mevcut: gerçek embedding üretimi + L2 normalize doğrulaması + cosine sanity.
public class E5EmbedderSmokeTest
{
    private readonly ITestOutputHelper _output;

    public E5EmbedderSmokeTest(ITestOutputHelper output) => _output = output;

    private static string ResolveContentRoot()
    {
        var baseDir = AppContext.BaseDirectory;
        return Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "Mosaik"));
    }

    private static bool ModelFilesPresent(string contentRoot)
    {
        var modelPath = Path.Combine(contentRoot, "App_Data", "models", "embed", "e5-base.onnx");
        var tokenizerPath = Path.Combine(contentRoot, "App_Data", "models", "tokenizer", "sentencepiece.bpe.model");
        return File.Exists(modelPath) && File.Exists(tokenizerPath);
    }

    [Fact]
    public async Task Embed_TurkishSentence_ReturnsNormalizedVector()
    {
        var contentRoot = ResolveContentRoot();
        if (!ModelFilesPresent(contentRoot))
        {
            _output.WriteLine($"E5 model/tokenizer yok, skip: {contentRoot}");
            return;
        }

        var embedder = new E5Embedder(NullLogger<E5Embedder>.Instance, contentRoot);
        Assert.True(embedder.IsReady, "Embedder hazır değil.");
        Assert.Equal(768, embedder.Dimensions);

        var emb = await embedder.EmbedAsync("Yıllık izin maksimum kaç gündür?", EmbedRole.Query);

        Assert.Equal(768, emb.Length);

        // L2 normalize kontrolü (norm ≈ 1.0).
        double norm = 0;
        foreach (var v in emb) norm += v * v;
        norm = Math.Sqrt(norm);
        _output.WriteLine($"L2 norm: {norm:F6}");
        Assert.InRange(norm, 0.99, 1.01);

        // Sıfır olmayan değer var (model ölü değil)
        int nonZero = emb.Count(v => Math.Abs(v) > 1e-6);
        Assert.True(nonZero > 100, $"Çok az aktif boyut: {nonZero}");

        embedder.Dispose();
    }

    [Fact]
    public async Task Embed_SimilarQueries_HighCosine()
    {
        var contentRoot = ResolveContentRoot();
        if (!ModelFilesPresent(contentRoot)) return;

        var embedder = new E5Embedder(NullLogger<E5Embedder>.Instance, contentRoot);
        Assert.True(embedder.IsReady);

        var a = await embedder.EmbedAsync("İzin nasıl alınır?", EmbedRole.Query);
        var b = await embedder.EmbedAsync("Yıllık izin nasıl talep edilir?", EmbedRole.Query);
        var c = await embedder.EmbedAsync("Java programlama dili", EmbedRole.Query);

        double simAB = Dot(a, b);
        double simAC = Dot(a, c);

        _output.WriteLine($"sim(izin1, izin2) = {simAB:F4}");
        _output.WriteLine($"sim(izin1, java)  = {simAC:F4}");

        Assert.True(simAB > simAC, "Semantik yakın çiftin cosine'i daha yüksek olmalı.");
        Assert.True(simAB > 0.7, $"İlgili sorular cosine düşük: {simAB:F4}");

        embedder.Dispose();
    }

    private static double Dot(float[] a, float[] b)
    {
        double s = 0;
        for (int i = 0; i < a.Length; i++) s += a[i] * b[i];
        return s;
    }
}
