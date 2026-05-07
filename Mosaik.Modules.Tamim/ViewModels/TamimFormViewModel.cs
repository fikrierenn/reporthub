using System.ComponentModel.DataAnnotations;
using Mosaik.Modules.Tamim.Models;

namespace Mosaik.Modules.Tamim.ViewModels
{
    // M-07: Entity referansı YASAK. Flat properties, mass assignment koruması.
    public class TamimFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Başlık zorunludur.")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "İçerik zorunludur.")]
        public string Body { get; set; } = string.Empty;

        public TamimStatus Status { get; set; } = TamimStatus.Draft;

        public DateTime? PublishDate { get; set; }

        public DateTime? ExpiresAt { get; set; }

        // Sadece okuma — service set eder
        public int CreatedById { get; set; }
        public string? CreatedByName { get; set; }
        public DateTime CreatedAt { get; set; }

        public int? ApprovedById { get; set; }
        public string? ApprovedByName { get; set; }

        public string Message { get; set; } = string.Empty;
        public string MessageType { get; set; } = string.Empty;
    }

    public class TamimListItemViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public TamimStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PublishDate { get; set; }
        public string? CreatedByName { get; set; }
    }
}
