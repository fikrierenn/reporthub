using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ReportPanel.Models
{
    // M-07: CategoryId + CreatedAt [BindNever] — mass assignment koruması.
    public class ReportCategory
    {
        [Key]
        [BindNever]
        public int CategoryId { get; set; }

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
