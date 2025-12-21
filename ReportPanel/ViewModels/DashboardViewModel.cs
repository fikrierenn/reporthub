using ReportPanel.Models;

namespace ReportPanel.ViewModels
{
    public class DashboardViewModel
    {
        public string User { get; set; } = "";
        public string[] UserRoles { get; set; } = Array.Empty<string>();
        public int ReportCount { get; set; }
        public int MonthlyRunCount { get; set; }
        public DateTime? LastRunAt { get; set; }
        public List<ReportCatalog> RecentReports { get; set; } = new();
        public List<AuditLog> RecentLogs { get; set; } = new();
    }
}
