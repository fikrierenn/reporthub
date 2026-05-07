using System.ComponentModel.DataAnnotations;

namespace Mosaik.Modules.Circular.ViewModels
{
    // M-07: Entity referansı YASAK. Flat properties, mass assignment koruması.
    public class BlockFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Departman adı zorunlu.")]
        [MaxLength(100)]
        public string Department { get; set; } = string.Empty;

        [Required(ErrorMessage = "Subject zorunlu.")]
        [MaxLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Açıklama zorunlu.")]
        public string Content { get; set; } = string.Empty;

        [Required(ErrorMessage = "Block türü seçilmeli.")]
        [Range(1, int.MaxValue, ErrorMessage = "Block türü seçilmeli.")]
        public int BlockTypeId { get; set; }

        public bool IsUrgent { get; set; }

        // Salt okuma (service set eder)
        public string BlockNumber { get; set; } = string.Empty;
        public DateTime BlockDate { get; set; }
        public int? CircularId { get; set; }
        public string? TamimBaslik { get; set; }

        public string Message { get; set; } = string.Empty;
        public string MessageType { get; set; } = string.Empty;

        // Dropdown için
        public List<BlockTypeOption> BlockTypeOptions { get; set; } = new();
    }

    public class BlockTypeOption
    {
        public int Id { get; set; }
        public string Label { get; set; } = string.Empty;
    }
}
