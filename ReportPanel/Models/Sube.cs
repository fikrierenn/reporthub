using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ReportPanel.Models
{
    /// <summary>
    /// Plan 07 Faz 5b — canonical sube master. UserDataFilters 'sube' FilterKey'inde
    /// FilterValue olarak SubeId tutar; UserDataFilterInjector SP parametresine yazarken
    /// SubeMapping uzerinden DataSource'a ozgu ExternalCode'a cevirir.
    /// </summary>
    public class Sube
    {
        [Key]
        [BindNever]
        public int SubeId { get; set; }

        [Required]
        [MaxLength(100)]
        public string SubeAd { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public int DisplayOrder { get; set; }

        [BindNever]
        public DateTime CreatedAt { get; set; }

        [BindNever]
        public DateTime? UpdatedAt { get; set; }
    }

    public class SubeMapping
    {
        [Key]
        [BindNever]
        public int MappingId { get; set; }

        public int SubeId { get; set; }

        [Required]
        [MaxLength(50)]
        public string DataSourceKey { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string ExternalCode { get; set; } = string.Empty;

        [BindNever]
        public DateTime CreatedAt { get; set; }

        [ForeignKey("SubeId")]
        public virtual Sube? Sube { get; set; }

        [ForeignKey("DataSourceKey")]
        public virtual DataSource? DataSource { get; set; }
    }
}
