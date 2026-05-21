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

        public string? ValueText { get; set; }                    // text/textarea/select/radio/multiselect/signature(base64)
        public decimal? ValueNumber { get; set; }
        public DateTime? ValueDate { get; set; }
        public bool? ValueBool { get; set; }
        public int? ValueFileId { get; set; }                     // Documents FK (soft ref)

        public bool IsEncrypted { get; set; }
    }
}
