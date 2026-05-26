using System.ComponentModel.DataAnnotations;

namespace Mosaik.Modules.SOP.ViewModels
{
    // Plan 34 Faz C — Create/Edit form binding.
    public class SopFormViewModel
    {
        public int Id { get; set; }                                   // 0 = Create, >0 = Edit

        [Required(ErrorMessage = "Firma zorunlu.")]
        public int FirmaId { get; set; }

        [Required(ErrorMessage = "Başlık zorunlu."), MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [MaxLength(80)]
        public string? Category { get; set; }                         // sopCategory lookup Code

        [MaxLength(500)]
        public string? DepartmentIds { get; set; }                    // CSV

        public bool IsCompanyWide { get; set; }

        [Required, Range(1, int.MaxValue, ErrorMessage = "Sahip kullanıcı zorunlu.")]
        public int OwnerUserId { get; set; }

        [Range(1, 365, ErrorMessage = "Okuma süresi 1-365 gün arası.")]
        public int ReadDeadlineDays { get; set; } = 30;

        public bool RequiresIKApproval { get; set; } = true;
        public bool AiAdvisorEnabled { get; set; } = true;

        // Kurumsal başlık alanları (PRD template uyumlu)
        [MaxLength(50)]
        public string? DocumentNumber { get; set; }
        [MaxLength(50)]
        public string? RevisionNumber { get; set; }
        public DateTime? PublishDate { get; set; }
        public DateTime? RevisionDate { get; set; }
        public DateTime? EffectiveDate { get; set; }
        [MaxLength(200)]
        public string? PreparedBy { get; set; }
        [MaxLength(200)]
        public string? ApprovedBy { get; set; }
        [MaxLength(100)]
        public string? ReviewFrequency { get; set; }
        [MaxLength(100)]
        public string? Classification { get; set; }

        // Plan 44 — Erişim Kapsamı (chunk permission kaynak)
        [Range(0, 3, ErrorMessage = "Güvenlik seviyesi 0-3 arası.")]
        public byte SecurityLevel { get; set; } = 1;               // Internal default

        [MaxLength(500)]
        public string? AllowedRoleIds { get; set; }                 // CSV rol adı (boş = herkes)

        [MaxLength(500)]
        public string? AllowedDepartmentIds { get; set; }           // CSV dept ID (boş = herkes)

        [MaxLength(500)]
        public string? AllowedUserIds { get; set; }                 // CSV userId (boş = herkes)
    }
}
