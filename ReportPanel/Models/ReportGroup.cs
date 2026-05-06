using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ReportPanel.Models
{
    // M-07: GroupId + CreatedAt [BindNever] — mass assignment koruması.
    // Plan 07 son rename: ReportCategory → ReportGroup (urunKategori ile çakışmasın).
    public class ReportGroup
    {
        [Key]
        [BindNever]
        public int GroupId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        [BindNever]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
