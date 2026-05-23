namespace Mosaik.Modules.SOP.Services
{
    // Plan 34.1 Faz 1 A-09 — PlainTextContent → 512-token chunks + 64 overlap.
    // Token tahmini ~4 char/token Türkçe için (XLMRoberta yaklaşık). Bu sayede
    // 512 token ≈ 2048 char, 64 overlap ≈ 256 char.
    //
    // Splitler kelime sınırında — chunk içeriği orta kelime kesilmez. Tokenizer
    // tam token sayımı yerine char approximation kullanılır (chunker perf önemli,
    // ±%10 sapma kabul). E5Embedder maks 512 token truncate'i savunma sağlar.
    public static class SopChunker
    {
        private const int ChunkSizeChars = 2048;                          // ≈ 512 token Türkçe
        private const int OverlapChars = 256;                              // ≈ 64 token overlap

        public static List<string> Split(string plainText)
        {
            if (string.IsNullOrWhiteSpace(plainText)) return new();

            var text = plainText.Trim();
            if (text.Length <= ChunkSizeChars) return new() { text };

            var chunks = new List<string>();
            int pos = 0;
            int step = ChunkSizeChars - OverlapChars;                      // ileri adım

            while (pos < text.Length)
            {
                int end = Math.Min(pos + ChunkSizeChars, text.Length);

                // Son chunk değilse kelime sınırında kes: end'den geri en yakın whitespace bul.
                if (end < text.Length)
                {
                    int boundary = FindWordBoundary(text, end);
                    if (boundary > pos + 100) end = boundary;              // çok küçük chunk üretmesin
                }

                chunks.Add(text.Substring(pos, end - pos).Trim());

                if (end >= text.Length) break;
                pos += step;
                if (pos >= end) pos = end;                                  // overlap çok büyükse koru
            }

            return chunks;
        }

        // end pozisyonundan geri en yakın whitespace ara (≤ 200 char geri).
        private static int FindWordBoundary(string text, int end)
        {
            int min = Math.Max(0, end - 200);
            for (int i = end - 1; i >= min; i--)
            {
                if (char.IsWhiteSpace(text[i])) return i + 1;
            }
            return end;                                                     // bulamadı, olduğu gibi kes
        }
    }
}
