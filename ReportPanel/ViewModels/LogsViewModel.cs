using ReportPanel.Models;

namespace ReportPanel.ViewModels
{
    public class LogsViewModel
    {
        public List<AuditLog> Logs { get; set; } = new();
        public List<string> EventTypes { get; set; } = new();
        public List<string> Usernames { get; set; } = new();
        public List<string> TargetTypes { get; set; } = new();
        public bool IsAdmin { get; set; }
        public string LogSearch { get; set; } = "";
        public string LogStart { get; set; } = "";
        public string LogEnd { get; set; } = "";
        public string SelectedEventType { get; set; } = "";
        public string SelectedUser { get; set; } = "";
        public string SelectedTargetType { get; set; } = "";
        public string SelectedSuccess { get; set; } = "";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }
}
