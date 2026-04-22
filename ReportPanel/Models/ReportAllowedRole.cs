using System.ComponentModel.DataAnnotations;

namespace ReportPanel.Models
{
    public class ReportAllowedRole
    {
        [Required]
        public int ReportId { get; set; }

        [Required]
        public int RoleId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ReportCatalog? Report { get; set; }
        public Role? Role { get; set; }
    }
}
