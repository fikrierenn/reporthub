using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReportPanel.Models
{
    public class ReportRunLog
    {
        [Key]
        public Guid RunId { get; set; } = Guid.NewGuid();
        
        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;
        
        public int ReportId { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string DataSourceKey { get; set; } = string.Empty;
        
        [Required]
        public string ParamsJson { get; set; } = string.Empty;
        
        public DateTime RunAt { get; set; } = DateTime.UtcNow;
        
        public int? DurationMs { get; set; }
        
        public int? ResultRowCount { get; set; }
        
        public bool IsSuccess { get; set; }
        
        [MaxLength(1000)]
        public string? ErrorMessage { get; set; }

        
        // Navigation property
        [ForeignKey("ReportId")]
        public virtual ReportCatalog? Report { get; set; }
    }
}
