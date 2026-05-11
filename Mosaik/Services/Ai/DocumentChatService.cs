using Mosaik.Core.Ai;

namespace Mosaik.Services.Ai
{
    // Plan 27 Faz B-05 — Single-doc chat (RAG'siz MVP).
    // Token <30K ise PDF metnini doğrudan context'e koy + kullanıcı sorusunu z.ai'a sor.
    // Daha büyük doc'lar için Faz E (Kernel Memory + vector RAG) gelecek.
    public sealed class DocumentChatService
    {
        private const int MaxContextChars = 30000; // ~7.5K token, z.ai 128K context'inde rahat

        private const string SystemPrompt = """
            Sen bir kurumsal doküman asistanısın. Sadece verilen DOKÜMAN metnine dayanarak
            kullanıcı sorularını yanıtla. Dokümandaki bilgilere bağlı kal — uydurma yapma.

            KURALLAR:
            - Cevap dokümandaki gerçek bilgilere dayansın. Bilgi yoksa "Bu doküman bunu içermiyor"
              veya "Dokümandan tespit edemedim" de.
            - Yanıt Türkçe ve kısa olsun. 1-3 paragraf yeterli.
            - Mümkünse ilgili madde/sayfa numarası belirt (örn. "[Sayfa 3] göre...").
            - Hukuki tavsiye verme. Belge yorumcusu rolündesin.
            - Asla tahmin etme. Belirsizlik varsa söyle.
            """;

        private readonly IAiSummaryProvider _ai;
        private readonly ILogger<DocumentChatService> _logger;

        public DocumentChatService(IAiSummaryProvider ai, ILogger<DocumentChatService> logger)
        {
            _ai = ai;
            _logger = logger;
        }

        public async Task<DocumentChatResult> AskAsync(string documentText, string fileName, string question, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(documentText))
                return new DocumentChatResult(false, null, "Belge metni boş.", 0, 0);
            if (string.IsNullOrWhiteSpace(question))
                return new DocumentChatResult(false, null, "Soru boş.", 0, 0);

            var context = documentText.Length > MaxContextChars
                ? documentText[..MaxContextChars] + "\n[... metin kırpıldı, sorgu kapsamı sınırlı ...]"
                : documentText;

            var userPrompt = $"""
                === DOKÜMAN: {fileName} ===
                {context}
                === DOKÜMAN SONU ===

                SORU: {question}

                Yukarıdaki belgeye dayanarak cevap ver.
                """;

            var result = await _ai.GenerateAsync(new AiRequest(
                SystemPrompt: SystemPrompt,
                UserPrompt: userPrompt,
                RequireJson: false,
                Purpose: "document_chat_singledoc"
            ), ct);

            if (!result.IsSuccess || string.IsNullOrWhiteSpace(result.RawJson))
            {
                _logger.LogWarning("DocumentChat AI hata: {Error}", result.Error);
                return new DocumentChatResult(false, null, result.Error ?? "AI yanıt vermedi", 0, 0);
            }

            return new DocumentChatResult(true, result.RawJson, null, result.InputTokens, result.OutputTokens);
        }
    }

    public sealed record DocumentChatResult(bool IsSuccess, string? Answer, string? Error, int InputTokens, int OutputTokens);
}
