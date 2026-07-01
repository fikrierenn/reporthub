using System.ComponentModel.DataAnnotations;

namespace Mosaik.Modules.Forms.Entities
{
    // Plan 41 §4.1 — Submit edilen veri.
    // Status: 0 Submitted | 1 InReview | 2 Completed | 3 Rejected
    public class FormSubmission
    {
        public int Id { get; set; }

        [Required]
        public int FormDefinitionId { get; set; }
        public FormDefinition? FormDefinition { get; set; }

        [Required]
        public int FirmaId { get; set; }

        // Plan 41 rev 2 (§4.6) — ZORUNLU: submission hangi şema versiyonuna göre
        // render/validate edildiyse o versiyon (geriye uyumluluk garantisi).
        [Required]
        public int FormVersionId { get; set; }
        public FormDefinitionVersion? FormVersion { get; set; }

        public int? SubmittedById { get; set; }                   // NULL anonim

        [MaxLength(200)]
        public string? SubmitterEmail { get; set; }

        [MaxLength(40)]
        public string? SubmitterPhone { get; set; }

        [MaxLength(45)]
        public string? SubmitterIp { get; set; }

        [MaxLength(500)]
        public string? SubmitterUserAgent { get; set; }

        public int? PublicTokenId { get; set; }                   // public link ile gelmişse

        public byte Status { get; set; }

        public int? LinkedProcessInstanceId { get; set; }         // Plan 42 ProcessInstance (soft ref)
        public int? WorkflowInstanceId { get; set; }              // Plan 36

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        public ICollection<FormSubmissionFieldValue> Values { get; set; } = new List<FormSubmissionFieldValue>();
        public ICollection<FormSubmissionFile> Files { get; set; } = new List<FormSubmissionFile>();   // Faz 4: ek + imza
    }
}
