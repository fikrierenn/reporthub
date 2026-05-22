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
    }
}
