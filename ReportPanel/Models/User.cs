using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ReportPanel.Models
{
    // M-07: kritik alanlara [BindNever] — mass assignment koruması (defansif).
    // Form binding ile UserId/PasswordHash/timestamp set edilemez; service layer set eder.
    public class User
    {
        [Key]
        [BindNever]
        public int UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        [BindNever]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Email { get; set; }

        public bool IsAdUser { get; set; }

        public bool IsActive { get; set; } = true;

        [BindNever]
        public DateTime? LastLoginAt { get; set; }

        [BindNever]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BindNever]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
