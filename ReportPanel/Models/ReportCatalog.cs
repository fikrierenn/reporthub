using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ReportPanel.Models
{
    // M-07: ReportId + CreatedAt [BindNever] — mass assignment koruması.
    public class ReportCatalog
    {
        [Key]
        [BindNever]
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

        // ADR-009 · M-11 F-1.5 alt-commit 1: [Obsolete] faz. Tum raporlar dashboard.
        // Property ve DB kolonu hala yaziliyor (default "dashboard"); alt-commit 3 + Migration 19
        // tamamen drop eder. Kod tarafinda okuma/yazma yok, sadece EF migration uyumu icin.
        [Obsolete("ADR-009: ReportType ayrimi kaldirildi. Tum raporlar dashboard. Migration 19 kolonu drop edecek.")]
        [Required]
        [MaxLength(20)]
        public string ReportType { get; set; } = "dashboard";

        public string? DashboardConfigJson { get; set; }

        [Required]
        [MaxLength(200)]
        public string AllowedRoles { get; set; } = string.Empty;
        
        public bool IsActive { get; set; } = true;

        [BindNever]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        [ForeignKey("DataSourceKey")]
        public virtual DataSource? DataSource { get; set; }
        
        public virtual ICollection<ReportRunLog> RunLogs { get; set; } = new List<ReportRunLog>();

        public virtual ICollection<ReportAllowedRole> ReportAllowedRoles { get; set; } = new List<ReportAllowedRole>();

        public virtual ICollection<ReportGroupLink> ReportGroups { get; set; } = new List<ReportGroupLink>();
    }
}
