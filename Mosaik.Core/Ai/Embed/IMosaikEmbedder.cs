namespace Mosaik.Core.AI.Embed
{
    // Plan 34.1 Faz 1 A-05 — Embedding abstraction. SOP RAG advisor + Documents
    // Plan 27 Faz E ortak kullanır. Impl Mosaik tarafında E5Embedder (ONNX).
    // E5 modelleri "query:" / "passage:" prefix kullanır — caller role belirtir.
    public interface IMosaikEmbedder
    {
        // Embedding boyutu (e5-base = 768). Vector store + cosine için gerekli.
        int Dimensions { get; }

        // Model hazır mı (ONNX dosyası yüklendi). False ise tüm Embed çağrıları
        // InvalidOperationException fırlatır. RAG advisor IsReady kontrol etmeli.
        bool IsReady { get; }

        // Tek metin için L2-normalize edilmiş embedding.
        Task<float[]> EmbedAsync(string text, EmbedRole role, CancellationToken ct = default);

        // Batch embed — N metin için N×Dimensions float matrix. Indexer için.
        Task<float[][]> EmbedBatchAsync(IReadOnlyList<string> texts, EmbedRole role, CancellationToken ct = default);
    }

    // E5 modelleri prefix gerektirir — query ve passage farklı embedding üretir.
    public enum EmbedRole
    {
        // Kullanıcı sorgusu — "query: ..." prefix.
        Query = 0,

        // SOP içerik chunk'ı — "passage: ..." prefix.
        Passage = 1
    }
}
