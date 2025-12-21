using System.ComponentModel.DataAnnotations;

namespace ReportPanel.ViewModels
{
    public class ProfileViewModel
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string FullName { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string Roles { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public DateTime? LastLoginAt { get; set; }

        public string? NewPassword { get; set; }

        public string? ConfirmPassword { get; set; }

        public string Message { get; set; } = string.Empty;
        public string MessageType { get; set; } = string.Empty;
    }
}
