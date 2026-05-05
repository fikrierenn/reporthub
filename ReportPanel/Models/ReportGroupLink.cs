using System.ComponentModel.DataAnnotations;

namespace ReportPanel.Models
{
    // Plan 07 son rename: ReportCategoryLink → ReportGroupLink.
    public class ReportGroupLink
    {
        [Required]
        public int ReportId { get; set; }

        [Required]
        public int GroupId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ReportCatalog? Report { get; set; }
        public ReportGroup? Group { get; set; }
    }
}
