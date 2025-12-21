using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReportPanel.Models
{
    public class ReportCatalog
    {
        [Key]
        public int ReportId { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string? Description { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string DataSourceKey { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(200)]
        public string ProcName { get; set; } = string.Empty;
        
        [Required]
        public string ParamSchemaJson { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(200)]
        public string AllowedRoles { get; set; } = string.Empty;
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        // Navigation property
        [ForeignKey("DataSourceKey")]
        public virtual DataSource? DataSource { get; set; }
        
        public virtual ICollection<ReportRunLog> RunLogs { get; set; } = new List<ReportRunLog>();
    }
}