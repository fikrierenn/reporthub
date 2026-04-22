using ReportPanel.Models;

namespace ReportPanel.ViewModels
{
    public class AdminReportFormViewModel
    {
        public ReportCatalog Report { get; set; } = new();
        public List<DataSource> DataSources { get; set; } = new();
        public List<Role> AvailableRoles { get; set; } = new();
        public HashSet<int> SelectedRoleIds { get; set; } = new();
        public List<ReportCategory> AvailableCategories { get; set; } = new();
        public HashSet<int> SelectedCategoryIds { get; set; } = new();
        public string? Message { get; set; }
        public string? MessageType { get; set; }
    }
}
