using System.ComponentModel.DataAnnotations;

namespace ReportPanel.Models
{
    public class ReportFavorite
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public int ReportId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User? User { get; set; }
        public ReportCatalog? Report { get; set; }
    }
}
