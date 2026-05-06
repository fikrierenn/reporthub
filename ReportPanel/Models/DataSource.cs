using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ReportPanel.Models
{
    // M-07: CreatedAt [BindNever] — mass assignment koruması. DataSourceKey hem Create
    // (user-input) hem Edit (route'tan) yolunda gerekli, BindNever YOK.
    public class DataSource
    {
        [Key]
        public string DataSourceKey { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string ConnString { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        [BindNever]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual ICollection<ReportCatalog> Reports { get; set; } = new List<ReportCatalog>();
    }
}
