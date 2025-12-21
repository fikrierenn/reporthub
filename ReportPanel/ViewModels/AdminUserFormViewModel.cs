using ReportPanel.Models;

namespace ReportPanel.ViewModels
{
    public class AdminUserFormViewModel
    {
        public User User { get; set; } = new();
        public string Message { get; set; } = "";
        public string MessageType { get; set; } = "";
        public string[] AvailableRoles { get; set; } = Array.Empty<string>();
    }
}
