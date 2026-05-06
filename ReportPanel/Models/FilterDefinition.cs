using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ReportPanel.Models
{
    public class FilterDefinition
    {
        [Key]
        [BindNever]
        public int FilterDefinitionId { get; set; }

        [Required]
        [MaxLength(50)]
        public string FilterKey { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Label { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Scope { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? DataSourceKey { get; set; }

        public string? OptionsQuery { get; set; }

        public bool IsActive { get; set; } = true;

        public int DisplayOrder { get; set; }

        [BindNever]
        public DateTime CreatedAt { get; set; }

        [BindNever]
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey("DataSourceKey")]
        public virtual DataSource? DataSource { get; set; }

        public const string ScopeSpInjection = "spInjection";
        public const string ScopeReportAccess = "reportAccess";
        public const string ValueAll = "*";
    }
}
