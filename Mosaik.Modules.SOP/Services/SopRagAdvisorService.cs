using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mosaik.Core.AI.Embed;
using Mosaik.Core.AI.Local;
using Mosaik.Core.AI.Rag;
using Mosaik.Core.AI.Skills;
using Mosaik.Core.Domain;
using Mosaik.Core.Logging;
using Mosaik.Modules.SOP.Entities;

namespace Mosaik.Modules.SOP.Services
{
    // Plan 34.1 Faz 2 A-14 + A-15 + A-17 — Cross-SOP RAG advisor.
    //
    // Pipeline (AskAsync):
    //   1) Hazır kontrolü (embedder + LLM IsReady) → graceful "AI hazır değil"
    //   2) Question embed (query prefix)
    //   3) Retriever top-K=4 + minScore=0.5 + firma scope
    //   4) Hit yok → "Bu soruya cevap verecek prosedür bulunamadı" + audit
    //   5) Context build: kaynak SOPlar + içerik chunk'ları
    //   6) LlmRunner.RunAsync(sıkı system prompt + context + soru)
    //   7) SopAiConversation persist (SourceSopVersionIds CSV)
    //   8) Audit sop_ai_question_asked
    //   9) Cevap + kaynak metadata döner (UI "Kaynaklar: [SOP-X]" listesi)
    public class SopRagAdvisorService
    {
        // Plan 34.1 §2 + ADR-022 §2.1 — sıkı system prompt.
        // Hallucination koruması (small 3B model RAG'sız çöker).
        private const string SystemPrompt =
            "Sen BKM'nin kurumsal prosedür danışmanısın. Yalnızca KAYNAKLAR bölümünde verilen " +
            "SOP içeriklerine dayanarak Türkçe yanıt ver.\n\n" +
            "KURALLAR:\n" +
            "- Cevap KAYNAKLAR'daki gerçek bilgiye bağlı kalsın. Bilgi yoksa 'Bu soruya cevap " +
            "verecek prosedür bulunamadı, lütfen prosedür yöneticisine danışın.' de.\n" +
            "- Asla uydurma yapma, hukuki tavsiye verme.\n" +
            "- Yanıt 1-3 paragraf, net ve sade Türkçe.\n" +
            "- Yorumlama açık olsun: 'Prosedüre göre ...' diye alıntı ver, çıkarımı ayrı belirt.\n" +
            "- Cevap sonunda KAYNAKLAR bölümünde hangi SOP'ları kullandığını belirt (zaten UI " +
            "tarafında ayrıca gösterilecek).";

        private const int MaxContextChars = 6000;                          // ~1500 token context guard

        private readonly DbContext _db;
        private readonly IMosaikEmbedder _embedder;
        private readonly ILlmRunner _llm;
        private readonly SopChunkRetriever _retriever;
        private readonly SopRateLimitGuard _rateLimit;
        private readonly ISkillCatalog _skills;
        private readonly IAuditLog _audit;
        private readonly ILogger<SopRagAdvisorService> _logger;

        public SopRagAdvisorService(
            DbContext db,
            IMosaikEmbedder embedder,
            ILlmRunner llm,
            SopChunkRetriever retriever,
            SopRateLimitGuard rateLimit,
            ISkillCatalog skills,
            IAuditLog audit,
            ILogger<SopRagAdvisorService> logger)
        {
            _db = db;
            _embedder = embedder;
            _llm = llm;
            _retriever = retriever;
            _rateLimit = rateLimit;
            _skills = skills;
            _audit = audit;
            _logger = logger;
        }

        public async Task<ServiceResult<SopAdvisorAnswer>> AskAsync(
            RagUserContext userCtx,
            bool isAdmin,
            string question,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(question))
                return ServiceResult<SopAdvisorAnswer>.Failure("Soru boş olamaz.");

            int userId = userCtx.UserId;
            int? firmaId = userCtx.FirmaId > 0 ? userCtx.FirmaId : null;

            // A-18 rate limit
            var quota = await _rateLimit.CheckAsync(userId, isAdmin, ct);
            if (!quota.CanAsk)
            {
                var resetMsg = quota.WindowResetAt.HasValue
                    ? $" {quota.WindowResetAt.Value.ToLocalTime():HH:mm}'da yeniden sorabilirsiniz."
                    : string.Empty;
                _logger.LogInformation(
                    "SopRagAdvisor: rate limit dolu UserId={UserId} Used={Used}",
                    userId, quota.UsedInWindow);
                return ServiceResult<SopAdvisorAnswer>.Failure(
                    $"Saatlik soru sınırına ulaştınız ({SopRateLimitGuard.HourlyLimit}/saat).{resetMsg}");
            }

            if (!_embedder.IsReady || !_llm.IsReady)
            {
                _logger.LogWarning(
                    "SopRagAdvisor: AI hazır değil (embedder={EmbReady}, llm={LlmReady})",
                    _embedder.IsReady, _llm.IsReady);
                return ServiceResult<SopAdvisorAnswer>.Failure(
                    "AI danışman şu an erişilemez. Lütfen sistem yöneticisine bildirin.");
            }

            // 1) Question → query embedding
            float[] queryEmb;
            try
            {
                queryEmb = await _embedder.EmbedAsync(question, EmbedRole.Query, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SopRagAdvisor: embedding hatası");
                return ServiceResult<SopAdvisorAnswer>.Failure("Soru analiz edilemedi.");
            }

            // 2) Retrieval — plan 44: RagUserContext ile permission-aware retrieval
            var hits = await _retriever.SearchAsync(queryEmb, userCtx, topK: 4, minScore: 0.5, ct);

            if (hits.Count == 0)
            {
                _logger.LogInformation(
                    "SopRagAdvisor: ilgili SOP bulunamadı UserId={UserId} Question={Q}",
                    userId, Trunc(question, 80));

                var noHitAnswer = new SopAdvisorAnswer(
                    Answer: "Bu soruya cevap verecek prosedür bulunamadı. Lütfen prosedür yöneticisine danışın veya yeni bir prosedür talebi açın.",
                    Sources: new List<SopAdvisorSource>(),
                    TokensIn: 0,
                    TokensOut: 0,
                    NoHits: true);

                await PersistAsync(userId, firmaId, question, noHitAnswer, ct);
                return ServiceResult<SopAdvisorAnswer>.Ok(noHitAnswer, "Cevap üretildi.");
            }

            // 3) Context build (top-K hit content joined)
            var contextStr = BuildContext(hits);

            // 4) Skill match — top-K hit'in SOP kategorisi + soru üzerinden uygun skill seç
            var topDoc = hits.FirstOrDefault();
            string? topCategory = null;
            if (topDoc != null)
            {
                var docMeta = await _db.Set<SopDocument>().AsNoTracking()
                    .Where(d => d.Id == topDoc.SopDocumentId)
                    .Select(d => new { d.Category })
                    .FirstOrDefaultAsync(ct);
                topCategory = docMeta?.Category;
            }
            // ContextSize 4096 → top-2 skill inject mümkün (~1800 token toplam).
            var skillManifests = await _skills.MatchAsync(new SkillMatchContext(
                Category: topCategory,
                Question: question), top: 2, ct);

            string augmentedSystemPrompt = SystemPrompt;
            if (skillManifests.Count > 0)
            {
                var sb = new StringBuilder(SystemPrompt);
                var injectedIds = new List<string>();
                foreach (var manifest in skillManifests)
                {
                    var skill = await _skills.GetAsync(manifest.Id, ct);
                    if (skill is null) continue;
                    // Her skill max 3000 char (2 skill × 3000 = 6000 char ≈ 1500 token).
                    var skillBody = skill.MarkdownContent;
                    if (skillBody.Length > 3000) skillBody = skillBody[..3000] + "\n[...kısaltıldı...]";
                    sb.Append("\n\n=== UZMANLIK BİLGİSİ (").Append(skill.Manifest.Name).Append(") ===\n")
                      .Append(skillBody)
                      .Append("\n=== UZMANLIK BİLGİSİ SONU ===");
                    injectedIds.Add(skill.Manifest.Id);
                }
                if (injectedIds.Count > 0)
                {
                    sb.Append("\n\nYukarıdaki uzmanlık bilgilerini referans olarak kullanabilirsin, ama KAYNAKLAR'da geçen SOP içeriği önceliklidir.");
                    augmentedSystemPrompt = sb.ToString();
                    _logger.LogInformation("SopRagAdvisor: {Count} skill inject edildi: {SkillIds}",
                        injectedIds.Count, string.Join(", ", injectedIds));
                }
            }

            // 5) LLM
            var userPrompt = $"=== KAYNAKLAR ===\n{contextStr}\n=== KAYNAKLAR SONU ===\n\nSORU: {question}\n\nKaynaklara dayanarak Türkçe cevap ver.";
            var llmResult = await _llm.RunAsync(augmentedSystemPrompt, userPrompt, new LlmRunOptions(MaxTokens: 512, Temperature: 0.3f), ct);

            if (!llmResult.IsSuccess || string.IsNullOrWhiteSpace(llmResult.Answer))
            {
                _logger.LogWarning(
                    "SopRagAdvisor: LLM başarısız UserId={UserId} Error={Error}",
                    userId, llmResult.Error);
                return ServiceResult<SopAdvisorAnswer>.Failure(
                    "AI cevap üretemedi. Tekrar deneyin veya prosedür yöneticisine danışın.");
            }

            // 5) Source dedup (aynı SOP'tan birden fazla chunk gelebilir, unique title)
            var sources = hits
                .GroupBy(h => h.SopDocumentId)
                .Select(g =>
                {
                    var first = g.OrderByDescending(x => x.Score).First();
                    return new SopAdvisorSource(
                        SopDocumentId: first.SopDocumentId,
                        SopVersionId: first.SopVersionId,
                        Title: first.SopTitle,
                        VersionNumber: first.VersionNumber,
                        Score: first.Score);
                })
                .OrderByDescending(s => s.Score)
                .Take(3)
                .ToList();

            var answer = new SopAdvisorAnswer(
                Answer: llmResult.Answer!,
                Sources: sources,
                TokensIn: llmResult.TokensIn,
                TokensOut: llmResult.TokensOut,
                NoHits: false);

            // 6) Persist + audit
            await PersistAsync(userId, firmaId, question, answer, ct);

            return ServiceResult<SopAdvisorAnswer>.Ok(answer, "Cevap üretildi.");
        }

        private async Task PersistAsync(int userId, int? firmaId, string question, SopAdvisorAnswer answer, CancellationToken ct)
        {
            try
            {
                var conversation = new SopAiConversation
                {
                    UserId = userId,
                    FirmaId = firmaId,
                    Question = question.Trim(),
                    Answer = answer.Answer,
                    SourceSopVersionIds = answer.Sources.Count == 0
                        ? null
                        : string.Join(",", answer.Sources.Select(s => s.SopVersionId)),
                    TokensIn = answer.TokensIn,
                    TokensOut = answer.TokensOut,
                    UserFeedback = 0,                                            // None (lookup sopAiFeedback)
                    CreatedAt = DateTime.UtcNow
                };
                _db.Set<SopAiConversation>().Add(conversation);
                await _db.SaveChangesAsync(ct);

                await _audit.LogAsync(
                    eventType: "sop_ai_question_asked",
                    targetType: "sop_ai_conversation",
                    targetKey: conversation.Id.ToString(),
                    description: $"User {userId} sordu: {Trunc(question, 100)}. {answer.Sources.Count} kaynak. NoHits={answer.NoHits}.");
            }
            catch (Exception ex)
            {
                // Persist hatası kullanıcıya geri dönmüş cevabı bozmaz — best-effort.
                _logger.LogWarning(ex,
                    "SopRagAdvisor: conversation persist hatası UserId={UserId}",
                    userId);
            }
        }

        private static string BuildContext(IReadOnlyList<SopChunkHit> hits)
        {
            var sb = new StringBuilder();
            int charsUsed = 0;
            for (int i = 0; i < hits.Count; i++)
            {
                var hit = hits[i];
                var header = $"\n[SOP {i + 1}] {hit.SopTitle} (v{hit.VersionNumber})\n";
                var content = hit.Content;
                int budget = MaxContextChars - charsUsed - header.Length;
                if (budget <= 100) break;
                if (content.Length > budget) content = content[..budget] + "...";
                sb.Append(header).Append(content).Append('\n');
                charsUsed += header.Length + content.Length + 1;
            }
            return sb.ToString();
        }

        private static string Trunc(string s, int maxLen) =>
            s.Length <= maxLen ? s : s[..maxLen] + "...";

        // Plan 34.1 Faz 5 A-25 — Thumbs-up/down feedback.
        // Sadece conversation sahibi feedback verebilir (UserId match).
        // Idempotent: aynı conversation tekrar feedback verirse en son set kazanır + FeedbackAt yenilenir.
        public async Task<ServiceResult> SubmitFeedbackAsync(int conversationId, int userId, byte feedback, string? note, CancellationToken ct = default)
        {
            // Lookup sopAiFeedback: 1 ThumbsUp | 2 ThumbsDown geçerli (0 None default'tan farklı set edilmesin).
            if (feedback != 1 && feedback != 2)
                return ServiceResult.Failure("Geçersiz feedback.");

            var conversation = await _db.Set<SopAiConversation>()
                .FirstOrDefaultAsync(c => c.Id == conversationId, ct);
            if (conversation is null)
                return ServiceResult.Failure("Conversation bulunamadı.");
            if (conversation.UserId != userId)
                return ServiceResult.Failure("Bu cevaba feedback veremezsiniz.");

            conversation.UserFeedback = feedback;
            conversation.FeedbackNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
            conversation.FeedbackAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            await _audit.LogAsync(
                eventType: feedback == 1 ? "sop_ai_feedback_up" : "sop_ai_feedback_down",
                targetType: "sop_ai_conversation",
                targetKey: conversationId.ToString(),
                description: $"User {userId} feedback={feedback}. Note: {Trunc(note ?? string.Empty, 50)}");

            return ServiceResult.Ok("Geri bildirim kaydedildi.");
        }
    }

    public sealed record SopAdvisorAnswer(
        string Answer,
        List<SopAdvisorSource> Sources,
        int TokensIn,
        int TokensOut,
        bool NoHits);

    public sealed record SopAdvisorSource(
        int SopDocumentId,
        int SopVersionId,
        string Title,
        int VersionNumber,
        double Score);
}
