using LLama;
using LLama.Common;
using Xunit.Abstractions;

namespace Mosaik.Tests;

// Plan 34.1 Faz 0 A-04 — LLamaSharp + Qwen 2.5 3B Q4 in-process inference smoke test.
// Model dosyası lokal yoksa test "skip" eder (sessizce pass). Kullanıcı model dosyasını
// App_Data/models/llm/qwen25-3b-instruct-q4_k_m.gguf konumuna manuel indirir
// (Mosaik/App_Data/models/README.md'deki talimat).
//
// Çalışma: dotnet test --filter "FullyQualifiedName~LlamaSharpSmoke"
public class LlamaSharpSmokeTest
{
    private readonly ITestOutputHelper _output;

    public LlamaSharpSmokeTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task Qwen25_3B_TurkishGreeting_ProducesOutput()
    {
        var modelPath = ResolveModelPath("qwen25-3b-instruct-q4_k_m.gguf");
        if (!File.Exists(modelPath))
        {
            _output.WriteLine($"Model dosyası bulunamadı, smoke test atlandı: {modelPath}");
            return;
        }

        _output.WriteLine($"Model: {modelPath}");
        _output.WriteLine($"Boyut: {new FileInfo(modelPath).Length / 1024 / 1024} MB");

        var parameters = new ModelParams(modelPath)
        {
            ContextSize = 512,
            GpuLayerCount = 0
        };

        using var weights = LLamaWeights.LoadFromFile(parameters);
        using var context = weights.CreateContext(parameters);
        var executor = new StatelessExecutor(weights, parameters);

        var inferenceParams = new InferenceParams
        {
            MaxTokens = 64,
            AntiPrompts = new List<string> { "<|im_end|>", "<|im_start|>" }
        };

        // Qwen 2.5 chat template (LlamaSharpRunner ile aynı format).
        const string prompt =
            "<|im_start|>system\nKısa cevap ver.<|im_end|>\n" +
            "<|im_start|>user\nMerhaba, bana kısaca kendini Türkçe tanıt.<|im_end|>\n" +
            "<|im_start|>assistant\n";
        var buffer = new System.Text.StringBuilder();
        await foreach (var token in executor.InferAsync(prompt, inferenceParams))
        {
            buffer.Append(token);
            if (buffer.Length > 200) break;
        }

        var output = buffer.ToString();
        _output.WriteLine($"Çıktı: {output}");

        Assert.False(string.IsNullOrWhiteSpace(output), "LLM cevap üretmedi.");
        Assert.True(output.Length >= 5, "Cevap çok kısa.");
    }

    private static string ResolveModelPath(string fileName)
    {
        // Bin offset: Mosaik.Tests/bin/Debug/net10.0/ → repo root → Mosaik/App_Data/models/llm
        var baseDir = AppContext.BaseDirectory;
        var repoRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", ".."));
        return Path.Combine(repoRoot, "Mosaik", "App_Data", "models", "llm", fileName);
    }
}
