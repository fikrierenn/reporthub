using System.ComponentModel.DataAnnotations;

namespace Mosaik.Modules.SOP.Entities
{
    // Plan 34 §2.1 — SOP versiyon snapshot.
    // ContentJson: block-based (Quill setup'ı Faz C'de Circular'dan kopyalanır).
    // PlainTextContent: AI Danışman context cache (Faz F, ContentJson'dan derive, max 30K char).
    // Status: 0 Draft | 1 Pending | 2 Approved | 3 Archived (önceki versiyon yeni onaylandığında).
    // SupersededDate: yeni versiyon onaylandığında set, 3 ay sonra Archived'a geçirilir.
    public class SopVersion
    {
        public int Id { get; set; }

        [Required]
        public int SopDocumentId { get; set; }
        public SopDocument? SopDocument { get; set; }

        [Required]
        public int VersionNumber { get; set; }

        [Required]
        public string ContentJson { get; set; } = string.Empty;     // block-based content (Quill)

        public string? PlainTextContent { get; set; }               // AI context cache (Faz F)

        public DateTime? EffectiveDate { get; set; }                // onay tarihi
        public DateTime? SupersededDate { get; set; }               // yeni versiyon onaylanınca set

        // Lookup typeCode "sopVersionStatus": 0 Draft | 1 Pending | 2 Approved | 3 Archived
        public byte Status { get; set; }

        [Required]
        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<SopReadReceipt> ReadReceipts { get; set; } = new List<SopReadReceipt>();
        public ICollection<SopApprovalSubmission> ApprovalSubmissions { get; set; } = new List<SopApprovalSubmission>();
    }
}
