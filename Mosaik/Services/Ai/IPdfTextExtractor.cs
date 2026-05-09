namespace Mosaik.Services.Ai
{
    // Plan 25 Faz D — PDF metin çıkarma abstraction.
    // AiExtractionWorker bu interface üzerinden metni alır; provider değiştirilebilir.
    public interface IPdfTextExtractor
    {
        // filePath: wwwroot-relative veya absolute path.
        // Dönen string: ham metin (boşsa PDF okunamadı).
        Task<string> ExtractAsync(string filePath, CancellationToken ct = default);
    }
}
