using System.ComponentModel.DataAnnotations;

namespace ReportPanel.Models
{
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
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        // Navigation property
        public virtual ICollection<ReportCatalog> Reports { get; set; } = new List<ReportCatalog>();
    }
}