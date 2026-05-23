using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mosaik.Core.AI.Local;
using Mosaik.Core.AI.Skills;
using Mosaik.Core.Logging;
using Mosaik.Modules.SOP.Entities;

namespace Mosaik.Modules.SOP.Services
{
    // Plan 34.1 Save-time AI Enrichment.
    // SOP version save sonrası Hangfire enqueue ile çağrılır.
    // Skill catalog (gida-uyum, KVKK, vergi, doküman-yazım) inject ederek LLM'i
    // SOP içeriğini tarar + uyarır + öner.
    // Çıktı: JSON report → SopEnrichmentReports → Details "AI İnceleme" section.
    public class SopEnrichmentService
    {
        private const string SystemPrompt =
            "Sen kurumsal SOP review uzmanısın. Verilen prosedürü UZMANLIK BİLGİSİ'ndeki standartlara göre tara.\n" +
            "Çıktı SADECE şu JSON formatında olmalı (markdown/açıklama yok):\n" +
            "{\n" +
            "  \"suggested_tags\": [\"...\"],\n" +
            "  \"suggested_category\": \"İK|Operasyon|Finans|Kalite|Etik|IT\",\n" +
            "  \"missing_sections\": [{\"section\":\"...\",\"severity\":\"red|orange|yellow\",\"note\":\"...\"}],\n" +
            "  \"anglo_jargon\": [{\"term\":\"RACI\",\"suggestion\":\"Sorumluluk Matrisi\"}],\n" +
            "  \"kvkk_issues\": [{\"issue\":\"...\",\"severity\":\"...\"}],\n" +
            "  \"retention_suggestions\": [{\"doc_type\":\"...\",\"suggested\":\"...\",\"basis\":\"...\"}],\n" +
            "  \"legal_basis_gaps\": [\"...\"],\n" +
            "  \"severity\": \"red|orange|yellow|green\",\n" +
            "  \"overall_score\": 1-10,\n" +
            "  \"summary\": \"1-3 cümle özet\"\n" +
            "}\n\n" +
            "KURALLAR:\n" +
            "- Sadece prosedürdeki gerçek eksikliği işaretle, uydurma yapma.\n" +
            "- Tag/category önerisi prosedür içeriğine bağlı kalsın.\n" +
            "- Anglo-jargon (RACI, MSDS, KPI, brifing, checklist, KKI vb.) tespit et.\n" +
            "- KVKK kişisel veri kloz eksikliği işaretle.\n" +
            "- Saklama süresi yasal dayanak ile öner (KVKK, 4857, VUK, 6331).\n" +
            "- Yanıt Türkçe.";

        private readonly DbContext _db;
        private readonly ILlmRunner _llm;
        private readonly ISkillCatalog _skills;
        private readonly IAuditLog _audit;
        private readonly ILogger<SopEnrichmentService> _logger;

        public SopEnrichmentService(
            DbContext db,
            ILlmRunner llm,
            ISkillCatalog skills,
            IAuditLog audit,
            ILogger<SopEnrichmentService> logger)
        {
            _db = db;
            _llm = llm;
            _skills = skills;
            _audit = audit;
            _logger = logger;
        }

        public async Task<int> ReviewAsync(int versionId, CancellationToken ct = default)
        {
            // Yeni report oluştur, status=Pending.
            var report = new SopEnrichmentReport
            {
                SopVersionId = versionId,
                ReportJson = "{}",
                Status = 0,                                                     // Pending (lookup sopEnrichmentStatus)
                CreatedAt = DateTime.UtcNow
            };
            _db.Set<SopEnrichmentReport>().Add(report);
            await _db.SaveChangesAsync(ct);

            try
            {
                var version = await _db.Set<SopVersion>()
                    .Include(v => v.SopDocument)
                    .FirstOrDefaultAsync(v => v.Id == versionId, ct);
                if (version?.SopDocument is null)
                {
                    return await FailAsync(report, "Versiyon veya document bulunamadı.", ct);
                }

                if (!_llm.IsReady)
                {
                    return await FailAsync(report, "LLM hazır değil — model dosyası eksik.", ct);
                }

                var content = version.PlainTextContent;
                if (string.IsNullOrWhiteSpace(content))
                {
                    return await FailAsync(report, "PlainTextContent boş.", ct);
                }
                if (content.Length > 3000) content = content[..3000] + "\n[...kırpıldı...]";

                // Skill match — Category + content keyword
                var skillManifests = await _skills.MatchAsync(new SkillMatchContext(
                    Category: version.SopDocument.Category,
                    Question: version.SopDocument.Title + " " + content[..Math.Min(500, content.Length)]),
                    top: 2, ct);

                // Skill inject
                var sb = new StringBuilder(SystemPrompt);
                var injectedIds = new List<string>();
                foreach (var manifest in skillManifests)
                {
                    var skill = await _skills.GetAsync(manifest.Id, ct);
                    if (skill is null) continue;
                    var body = skill.MarkdownContent;
                    if (body.Length > 3000) body = body[..3000] + "\n[...kısaltıldı...]";
                    sb.Append("\n\n=== UZMANLIK BİLGİSİ (").Append(skill.Manifest.Name).Append(") ===\n")
                      .Append(body)
                      .Append("\n=== UZMANLIK BİLGİSİ SONU ===");
                    injectedIds.Add(skill.Manifest.Id);
                }

                var userPrompt = $"=== PROSEDÜR ===\nBaşlık: {version.SopDocument.Title}\nKategori: {version.SopDocument.Category ?? "—"}\n\n{content}\n=== PROSEDÜR SONU ===\n\nYukarıdaki prosedürü tara + uyar + öner. JSON formatında dön.";

                var llmResult = await _llm.RunAsync(sb.ToString(), userPrompt,
                    new LlmRunOptions(MaxTokens: 1024, Temperature: 0.2f), ct);

                if (!llmResult.IsSuccess || string.IsNullOrWhiteSpace(llmResult.Answer))
                {
                    return await FailAsync(report, "LLM cevap üretmedi: " + (llmResult.Error ?? "unknown"), ct);
                }

                // JSON extract (LLM bazen markdown code fence ekler)
                var json = ExtractJson(llmResult.Answer);
                if (string.IsNullOrEmpty(json))
                {
                    return await FailAsync(report, "LLM çıktısı geçerli JSON değil.", ct);
                }

                // Validate parse
                try
                {
                    JsonDocument.Parse(json);
                }
                catch (JsonException ex)
                {
                    return await FailAsync(report, "JSON parse hatası: " + ex.Message, ct);
                }

                report.ReportJson = json;
                report.Status = 1;                                              // Completed
                report.CompletedAt = DateTime.UtcNow;
                report.InjectedSkillIds = string.Join(",", injectedIds);
                await _db.SaveChangesAsync(ct);

                await _audit.LogAsync(
                    eventType: "sop_enrichment_completed",
                    targetType: "sop_version",
                    targetKey: versionId.ToString(),
                    description: $"AI review tamamlandı. Inject: {string.Join(", ", injectedIds)}");

                return report.Id;
            }
            catch (Exception ex)
            {
                return await FailAsync(report, "Beklenmeyen hata: " + ex.GetType().Name + " " + ex.Message, ct);
            }
        }

        // Önerileri kabul et: suggested_category (whitelist) SopDocument'e yansır,
        // report Status=3 (Accepted) + AcceptedAt/By + audit. Tag desteği master entity'de henüz yok.
        public async Task<(bool ok, string message)> AcceptAsync(int reportId, int userId, CancellationToken ct = default)
        {
            var report = await _db.Set<SopEnrichmentReport>()
                .Include(r => r.SopVersion)
                    .ThenInclude(v => v!.SopDocument)
                .FirstOrDefaultAsync(r => r.Id == reportId, ct);
            if (report is null) return (false, "Rapor bulunamadı.");
            if (report.Status != 1) return (false, "Sadece Tamamlanmış raporlar kabul edilebilir.");
            if (report.SopVersion?.SopDocument is null) return (false, "İlişkili SOP bulunamadı.");

            string? appliedCategory = null;
            try
            {
                using var jdoc = JsonDocument.Parse(report.ReportJson);
                if (jdoc.RootElement.TryGetProperty("suggested_category", out var cat)
                    && cat.ValueKind == JsonValueKind.String)
                {
                    var c = cat.GetString();
                    // Whitelist — system prompt'ta listelenenler.
                    var whitelist = new[] { "İK", "Operasyon", "Finans", "Kalite", "Etik", "IT" };
                    if (!string.IsNullOrWhiteSpace(c) && whitelist.Contains(c))
                    {
                        report.SopVersion.SopDocument.Category = c;
                        report.SopVersion.SopDocument.UpdatedAt = DateTime.UtcNow;
                        appliedCategory = c;
                    }
                }
            }
            catch (JsonException)
            {
                // JSON malformed — accept yine de devam (sadece kategori atlanır).
            }

            report.Status = 3;                                                  // Accepted
            report.AcceptedAt = DateTime.UtcNow;
            report.AcceptedByUserId = userId;
            await _db.SaveChangesAsync(ct);

            await _audit.LogAsync(
                eventType: "sop_enrichment_accepted",
                targetType: "sop_version",
                targetKey: report.SopVersionId.ToString(),
                description: appliedCategory is null
                    ? "AI önerileri kabul edildi (kategori uygulanmadı)."
                    : $"AI önerileri kabul edildi. Kategori → {appliedCategory}.");

            return (true, appliedCategory is null
                ? "Öneriler kabul edildi (kategori değişmedi)."
                : $"Öneriler kabul edildi. Kategori \"{appliedCategory}\" olarak güncellendi.");
        }

        private async Task<int> FailAsync(SopEnrichmentReport report, string reason, CancellationToken ct)
        {
            report.Status = 2;                                                  // Failed
            report.FailureReason = reason.Length > 500 ? reason[..500] : reason;
            report.CompletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            _logger.LogWarning("SopEnrichment failed VersionId={VersionId}: {Reason}",
                report.SopVersionId, reason);

            await _audit.LogAsync(
                eventType: "sop_enrichment_failed",
                targetType: "sop_version",
                targetKey: report.SopVersionId.ToString(),
                description: reason,
                isSuccess: false);

            return report.Id;
        }

        // LLM bazen ```json ... ``` ekler, bazen düz JSON döner. İlk { ile son } arası kes.
        private static string? ExtractJson(string raw)
        {
            int start = raw.IndexOf('{');
            int end = raw.LastIndexOf('}');
            if (start < 0 || end <= start) return null;
            return raw[start..(end + 1)];
        }
    }
}
