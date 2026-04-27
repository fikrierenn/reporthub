using ReportPanel.Models;

namespace ReportPanel.ViewModels
{
    public class DashboardViewModel
    {
        public string User { get; set; } = "";
        public string FullName { get; set; } = "";
        public string[] UserRoles { get; set; } = Array.Empty<string>();

        // KPI'lar
        public int ReportCount { get; set; }
        public int TodayRunCount { get; set; }
        public int MonthlyRunCount { get; set; }
        public int ActiveUserCount { get; set; }
        public DateTime? LastRunAt { get; set; }

        // Listeler
        public List<TopRunReportItem> TopRunReports { get; set; } = new();
        public List<ReportCatalog> FavoriteReports { get; set; } = new();
        public List<AuditLog> RecentLogs { get; set; } = new();

        // Sistem durumu (M-13: future hook)
        public string SystemStatus { get; set; } = "tüm sistemler çalışıyor";
        public string DataSourceStatus { get; set; } = "DerinSIS · 12ms";
    }

    public class TopRunReportItem
    {
        public string Title { get; set; } = "";
        public int Count { get; set; }
    }
}
