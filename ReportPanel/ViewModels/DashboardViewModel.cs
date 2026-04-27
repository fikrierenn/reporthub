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
        public bool HasUserFavorites { get; set; }
        public List<AuditLog> RecentLogs { get; set; } = new();
        public List<HourlyTrendItem> HourlyTrend { get; set; } = new(); // Son 24 saat, 24 entry (boş saatler = 0)

        // Sistem durumu (canlı)
        public string SystemStatus { get; set; } = "";
        public string SystemStatusKind { get; set; } = "ok"; // ok / warn / err
        public string DataSourceStatus { get; set; } = "";
    }

    public class TopRunReportItem
    {
        public string Title { get; set; } = "";
        public int Count { get; set; }
    }

    public class HourlyTrendItem
    {
        public DateTime Hour { get; set; } // Saat başlangıç UTC
        public int Count { get; set; }
    }
}
