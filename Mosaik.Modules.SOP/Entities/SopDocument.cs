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

        [Required]
        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<SopVersion> Versions { get; set; } = new List<SopVersion>();
    }
}
