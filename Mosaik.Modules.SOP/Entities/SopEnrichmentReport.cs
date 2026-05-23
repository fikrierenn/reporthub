using System.ComponentModel.DataAnnotations;

namespace Mosaik.Modules.SOP.Entities
{
    // Plan 34.1 Save-time AI Enrichment — SOP version save sonrası LLM otomatik review.
    // SopEnrichmentService.ReviewAsync üretir, Details sayfasında "AI İnceleme" gösterilir.
    // Skill catalog (gida-uyum, KVKK, vergi, doküman-yazım) inject edilerek tarama.
    public class SopEnrichmentReport
    {
        public int Id { get; set; }

        [Required]
        public int SopVersionId { get; set; }
        public SopVersion? SopVersion { get; set; }

        // JSON: { suggested_tags, suggested_category, missing_sections, anglo_jargon,
        //         kvkk_issues, retention_suggestions, legal_basis_gaps, severity, summary }
        [Required]
        public string ReportJson { get; set; } = string.Empty;

        // Lookup typeCode "sopEnrichmentStatus": 0 pending | 1 completed | 2 failed | 3 accepted
        public byte Status { get; set; }

        [MaxLength(500)]
        public string? FailureReason { get; set; }

        // İnject edilen skill ID'leri (audit + transparancy)
        [MaxLength(500)]
        public string? InjectedSkillIds { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public DateTime? AcceptedAt { get; set; }
        public int? AcceptedByUserId { get; set; }
    }
}
