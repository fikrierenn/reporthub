using System.ComponentModel.DataAnnotations;

namespace ReportPanel.Models
{
    public class AuditLog
    {
        [Key]
        public Guid AuditId { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string EventType { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? TargetType { get; set; }

        [MaxLength(200)]
        public string? TargetKey { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public string? OldValuesJson { get; set; }

        public string? NewValuesJson { get; set; }

        public bool IsSuccess { get; set; } = true;

        [MaxLength(1000)]
        public string? ErrorMessage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int? ReportId { get; set; }

        [MaxLength(50)]
        public string? DataSourceKey { get; set; }

        public string? ParamsJson { get; set; }

        public int? DurationMs { get; set; }

        public int? ResultRowCount { get; set; }

        [MaxLength(45)]
        public string? IpAddress { get; set; }

        [MaxLength(300)]
        public string? UserAgent { get; set; }
    }
}
