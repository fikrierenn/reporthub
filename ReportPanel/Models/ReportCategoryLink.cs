using System.ComponentModel.DataAnnotations;

namespace ReportPanel.Models
{
    public class ReportCategoryLink
    {
        [Required]
        public int ReportId { get; set; }

        [Required]
        public int CategoryId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ReportCatalog? Report { get; set; }
        public ReportCategory? Category { get; set; }
    }
}
