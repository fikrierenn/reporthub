using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.Tokenizers;
using Mosaik.Core.AI.Embed;

namespace Mosaik.Services.Ai
{
    // Plan 34.1 Faz 1 A-06 — intfloat/multilingual-e5-base ONNX INT8 embedder.
    // Mosaik.Core.AI.Embed.IMosaikEmbedder impl. SOP RAG advisor + Documents Plan 27
    // Faz E ortak kullanır.
    //
    // Pipeline: text → "query:|passage: " prefix → SentencePiece tokenize →
    //   ONNX run → last_hidden_state → mean pool with attention mask → L2 normalize
    //
    // Model dosyaları (Mosaik/App_Data/models/):
    //   embed/e5-base-int8.onnx                    (~110 MB)
    //   tokenizer/sentencepiece.bpe.model          (XLMRoberta sentencepiece)
    //
    // Eksik dosya: IsReady=false döner, Embed çağrıları InvalidOperationException.
    public sealed class E5Embedder : IMosaikEmbedder, IDisposable
    {
        private const int E5Dimensions = 768;
        private const int MaxSeqLength = 512;

        private readonly ILogger<E5Embedder> _logger;
        private readonly InferenceSession? _session;
        private readonly SentencePieceTokenizer? _tokenizer;
        private readonly bool _ready;

        public int Dimensions => E5Dimensions;
        public bool IsReady => _ready;

        public E5Embedder(ILogger<E5Embedder> logger, string contentRoot)
        {
            _logger = logger;

            var modelPath = Path.Combine(contentRoot, "App_Data", "models", "embed", "e5-base.onnx");
            var tokenizerPath = Path.Combine(contentRoot, "App_Data", "models", "tokenizer", "sentencepiece.bpe.model");

            if (!File.Exists(modelPath) || !File.Exists(tokenizerPath))
            {
                _logger.LogWarning(
                    "E5Embedder: model veya tokenizer dosyası bulunamadı, embedder DEVRE DIŞI. Model={Model}, Tokenizer={Tokenizer}",
                    modelPath, tokenizerPath);
                _ready = false;
                return;
            }

            try
            {
                _session = new InferenceSession(modelPath);
                using var tokenizerStream = File.OpenRead(tokenizerPath);
                _tokenizer = SentencePieceTokenizer.Create(tokenizerStream, addBeginningOfSentence: true, addEndOfSentence: true);
                _ready = true;
                _logger.LogInformation("E5Embedder hazır: {Dims} dim, max seq {MaxSeq}", Dimensions, MaxSeqLength);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "E5Embedder yükleme hatası, embedder DEVRE DIŞI.");
                _ready = false;
            }
        }

        public async Task<float[]> EmbedAsync(string text, EmbedRole role, CancellationToken ct = default)
        {
            var batch = await EmbedBatchAsync(new[] { text }, role, ct);
            return batch[0];
        }

        public Task<float[][]> EmbedBatchAsync(IReadOnlyList<string> texts, EmbedRole role, CancellationToken ct = default)
        {
            if (!_ready) throw new InvalidOperationException("E5Embedder hazır değil — model/tokenizer dosyası eksik.");
            if (texts.Count == 0) return Task.FromResult(Array.Empty<float[]>());

            ct.ThrowIfCancellationRequested();

            var prefix = role == EmbedRole.Query ? "query: " : "passage: ";
            var result = new float[texts.Count][];

            for (int i = 0; i < texts.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                result[i] = EmbedSingle(prefix + (texts[i] ?? string.Empty));
            }

            return Task.FromResult(result);
        }

        private float[] EmbedSingle(string text)
        {
            var ids = _tokenizer!.EncodeToIds(text);
            if (ids.Count > MaxSeqLength) ids = ids.Take(MaxSeqLength).ToList();
            int seqLen = ids.Count;

            var inputIds = new long[seqLen];
            var attentionMask = new long[seqLen];
            for (int i = 0; i < seqLen; i++)
            {
                inputIds[i] = ids[i];
                attentionMask[i] = 1;
            }

            var inputIdsTensor = new DenseTensor<long>(inputIds, new[] { 1, seqLen });
            var attentionTensor = new DenseTensor<long>(attentionMask, new[] { 1, seqLen });

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input_ids", inputIdsTensor),
                NamedOnnxValue.CreateFromTensor("attention_mask", attentionTensor)
            };

            using var outputs = _session!.Run(inputs);
            var lastHidden = outputs.First().AsTensor<float>();             // [1, seq, 768]

            // Mean pooling — attention mask ile (e5 önerisi).
            var pooled = new float[E5Dimensions];
            int activeTokens = 0;
            for (int t = 0; t < seqLen; t++)
            {
                if (attentionMask[t] == 0) continue;
                activeTokens++;
                for (int d = 0; d < E5Dimensions; d++)
                {
                    pooled[d] += lastHidden[0, t, d];
                }
            }
            if (activeTokens == 0) activeTokens = 1;                         // div-by-zero guard
            for (int d = 0; d < E5Dimensions; d++) pooled[d] /= activeTokens;

            // L2 normalize (cosine = dot product için).
            double norm = 0;
            for (int d = 0; d < E5Dimensions; d++) norm += pooled[d] * pooled[d];
            norm = Math.Sqrt(norm);
            if (norm > 0)
            {
                float scale = (float)(1.0 / norm);
                for (int d = 0; d < E5Dimensions; d++) pooled[d] *= scale;
            }

            return pooled;
        }

        public void Dispose()
        {
            _session?.Dispose();
        }
    }
}
