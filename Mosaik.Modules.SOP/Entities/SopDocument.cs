using System.ComponentModel.DataAnnotations;

namespace Mosaik.Modules.SOP.Entities
{
    // Plan 34 §2.1 — SOP master record.
    // DepartmentIds: CSV başlangıçta ("1,4,12"); Plan 18B HR sync sonrası many-to-many'e taşınır.
    // IsCompanyWide: "Şirket Geneli" pseudo-departman seçimi (DepartmentIds yok sayılır).
    // ReadDeadlineDays: SOP bazında override edilebilir (default 30).
    // RequiresIKApproval: false ise 2-adımlı flow (Yazan → DepYön), true ise 3-adımlı.
    // AiAdvisorEnabled: false ise SOP detay sayfasında AI Danışman drawer butonu görünmez (Faz F).
    public class SopDocument
    {
        public int Id { get; set; }

        [Required]
        public int FirmaId { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [MaxLength(80)]
        public string? Category { get; set; }                       // "İK" | "Operasyon" | "IT" | "Finans" | "Kalite" | "Etik"

        [MaxLength(500)]
        public string? DepartmentIds { get; set; }                  // CSV: "1,4,12"; Plan 18B sonrası refactor

        public bool IsCompanyWide { get; set; }

        [Required]
        public int OwnerUserId { get; set; }

        public bool IsActive { get; set; } = true;

        public int ReadDeadlineDays { get; set; } = 30;
        public bool RequiresIKApproval { get; set; } = true;
        public bool AiAdvisorEnabled { get; set; } = true;

        // Plan 34.1: kurumsal SOP başlık alanları (PRD template uyumlu)
        [MaxLength(50)]
        public string? DocumentNumber { get; set; }                 // örn. "PRD-CRM-001"
        [MaxLength(50)]
        public string? RevisionNumber { get; set; }                 // örn. "Rev.0" / "v1.0"
        public DateTime? PublishDate { get; set; }                  // Yayın Tarihi
        public DateTime? RevisionDate { get; set; }                 // Son Revizyon Tarihi
        public DateTime? EffectiveDate { get; set; }                // Yürürlük Tarihi (master, SopVersion'dan ayrı)
        [MaxLength(200)]
        public string? PreparedBy { get; set; }                     // Hazırlayan (örn. "KVKK Uyum Sorumlusu (DPO)")
        [MaxLength(200)]
        public string? ApprovedBy { get; set; }                     // Onaylayan (örn. "Genel Müdür")
        [MaxLength(100)]
        public string? ReviewFrequency { get; set; }                // örn. "Yıllık (Mart)"
        [MaxLength(100)]
        public string? Classification { get; set; }                 // örn. "Şirket İçi - Kontrollü Dağıtım"

        // Plan 44 — Chunk-level permission kaynak (ChunkPermissionSyncJob bu kolonları chunk'lara yazar).
        // SecurityLevel: 0=Public, 1=Internal(default), 2=Confidential, 3=Restricted
        public byte SecurityLevel { get; set; } = 1;               // Internal default
        [MaxLength(500)]
        public string? AllowedRoleIds { get; set; }                 // CSV rol adı; null=herkes
        [MaxLength(500)]
        public string? AllowedDepartmentIds { get; set; }           // CSV dept ID; null=herkes
        [MaxLength(500)]
        public string? AllowedUserIds { get; set; }                 // CSV userId; null=herkes

        [Required]
        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<SopVersion> Versions { get; set; } = new List<SopVersion>();
    }
}
