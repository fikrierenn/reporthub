using System.ComponentModel.DataAnnotations;

namespace Mosaik.Modules.SOP.ViewModels
{
    // Plan 34 Faz C — Yeni versiyon (Quill block content).
    public class SopVersionFormViewModel
    {
        [Required]
        public int SopDocumentId { get; set; }

        [Required(ErrorMessage = "İçerik boş olamaz.")]
        public string ContentJson { get; set; } = string.Empty;
    }
}
