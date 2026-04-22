using System.ComponentModel.DataAnnotations;

namespace ReportPanel.Models
{
    public class UserRole
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public int RoleId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User? User { get; set; }
        public Role? Role { get; set; }
    }
}
