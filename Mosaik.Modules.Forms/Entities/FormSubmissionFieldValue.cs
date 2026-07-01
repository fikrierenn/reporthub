using System.ComponentModel.DataAnnotations;

namespace Mosaik.Modules.Forms.Entities
{
    // Plan 41 §4.1 — Alan-değer çifti.
    // IsEncrypted: ihbar formu AES-256 (Plan 41 Faz 5).
    public class FormSubmissionFieldValue
    {
        public int Id { get; set; }

        [Required]
        public int FormSubmissionId { get; set; }
        public FormSubmission? FormSubmission { get; set; }

        [Required]
        public int FormFieldId { get; set; }
        public FormField? FormField { get; set; }

        [Required, MaxLength(80)]
        public string FieldKey { get; set; } = string.Empty;      // denormalized for query

        public string? ValueText { get; set; }                    // text/textarea/select/radio/multiselect
        public decimal? ValueNumber { get; set; }
        public DateTime? ValueDate { get; set; }
        public bool? ValueBool { get; set; }

        // Faz 4: file + signature değeri → FormSubmissionFile (disk storage). base64 inline DEĞİL
        // (advisor 2026-07-01, conf 72 — NVARCHAR(MAX) şişmesi + render decode maliyeti).
        public int? ValueFileId { get; set; }
        public FormSubmissionFile? File { get; set; }

        public bool IsEncrypted { get; set; }
    }
}
