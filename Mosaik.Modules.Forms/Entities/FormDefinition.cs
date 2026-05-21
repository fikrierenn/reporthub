using System.ComponentModel.DataAnnotations;

namespace Mosaik.Modules.Forms.Entities
{
    // Plan 41 §4.1 — Form metadata.
    // Status: 0 Taslak | 1 Yayında | 2 Arşiv.
    // IsPublic: anonim/public link erişimi izinli mi.
    // IsAnonymous: submit edilirken kimlik gerekmiyor mu (ihbar formu).
    // IsEncrypted: field-level AES-256 (ihbar şifreli storage).
    public class FormDefinition
    {
        public int Id { get; set; }

        [Required]
        public int FirmaId { get; set; }

        [Required, MaxLength(120)]
        public string Slug { get; set; } = string.Empty;          // "dsar-basvuru", "ihbar-formu"

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [MaxLength(80)]
        public string? Category { get; set; }                     // "KVKK" | "İK" | "Müşteri" | "İç süreç"

        public bool IsPublic { get; set; }
        public bool IsAnonymous { get; set; }
        public bool IsEncrypted { get; set; }

        public byte Status { get; set; }                          // 0 Taslak 1 Yayında 2 Arşiv
        public int Version { get; set; } = 1;

        public int? LinkedProcessId { get; set; }                 // Plan 40 KVKK Process (sonradan FK)
        public int? TriggersWorkflowId { get; set; }              // Plan 36 trigger (sonradan FK)

        [Required]
        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<FormField> Fields { get; set; } = new List<FormField>();
        public ICollection<FormSubmission> Submissions { get; set; } = new List<FormSubmission>();
        public ICollection<PublicFormToken> PublicTokens { get; set; } = new List<PublicFormToken>();
        public ICollection<FormDefinitionVersion> Versions { get; set; } = new List<FormDefinitionVersion>();
    }
}
