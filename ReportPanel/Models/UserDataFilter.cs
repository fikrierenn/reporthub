using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReportPanel.Models
{
    public class UserDataFilter
    {
        [Key]
        public int FilterId { get; set; }

        public int UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string FilterKey { get; set; } = string.Empty; // sube, bolum, kategori...

        [Required]
        [MaxLength(100)]
        public string FilterValue { get; set; } = string.Empty; // FSM, KIRTASİYE...

        [MaxLength(50)]
        public string? DataSourceKey { get; set; } // null = tümü

        public int? ReportId { get; set; } // null = tüm raporlar

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [ForeignKey("DataSourceKey")]
        public virtual DataSource? DataSource { get; set; }

        [ForeignKey("ReportId")]
        public virtual ReportCatalog? Report { get; set; }
    }
}
