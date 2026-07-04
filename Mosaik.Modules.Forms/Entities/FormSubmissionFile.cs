using System.ComponentModel.DataAnnotations;

namespace Mosaik.Modules.Forms.Entities
{
    // Plan 41 Faz 4 — form eki + imza görseli disk storage kaydı.
    // Forms modülü Documents'a (ana proje) compile bağlanamaz (ADR-002) → kendi izole
    // App_Data/forms/{firmaId}/ storage'ı (advisor 2026-07-01, conf 85). Disk yolu ContentRoot-göreli.
    public class FormSubmissionFile
    {
        public int Id { get; set; }

        [Required]
        public int FormSubmissionId { get; set; }
        public FormSubmission? FormSubmission { get; set; }

        [Required]
        public int FirmaId { get; set; }

        [Required, MaxLength(80)]
        public string FieldKey { get; set; } = string.Empty;      // hangi alandan geldi

        [Required, MaxLength(300)]
        public string FileName { get; set; } = string.Empty;      // orijinal ad (imza için "signature.png")

        [Required, MaxLength(400)]
        public string DiskPath { get; set; } = string.Empty;      // ContentRoot-göreli (App_Data/forms/{firma}/{guid}.ext)

        public long FileSize { get; set; }

        [MaxLength(120)]
        public string? MimeType { get; set; }

        // Plan 57 A2-#1 — form IsEncrypted ise disk içeriği DataProtection ile şifreli
        // (FileSize orijinal boyut; indirme decrypt-gate arkasında çözer).
        public bool IsEncrypted { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
