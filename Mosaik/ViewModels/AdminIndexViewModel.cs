using Mosaik.Models;

namespace Mosaik.ViewModels
{
    public class AdminIndexViewModel : UserMessageViewModel
    {
        public string ActiveTab { get; set; } = "datasources";
        public string TemplateConnString { get; set; } = "";
        public List<DataSource> DataSources { get; set; } = new();
        public List<ReportCatalog> Reports { get; set; } = new();
        public List<User> Users { get; set; } = new();
        // M-03 Faz B: UserId -> ["admin","ik",...] (UserRole junction'dan dolduruldu). View icin.
        public Dictionary<int, List<string>> UserRoleNames { get; set; } = new();
        public List<Role> Roles { get; set; } = new();
        public List<ReportGroup> Groups { get; set; } = new();
        // Plan 07 Faz 6 — Admin "Filtreler" tab listesi.
        public List<FilterDefinition> FilterDefinitions { get; set; } = new();

        // Plan 23 — Dashboard widget verileri. tab=overview varsayılan (yeni).
        public int ActiveUserCount { get; set; }
        public int ActiveModuleCount { get; set; }
        public int TotalModuleCount { get; set; }
        public int TodayAuditCount { get; set; }
        public int FailedLoginCount24h { get; set; }
        public List<AuditLog> RecentAudits { get; set; } = new();
    }
}
