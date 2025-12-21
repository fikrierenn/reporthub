using ReportPanel.Models;

namespace ReportPanel.ViewModels
{
    public class AdminReportFormViewModel
    {
        public ReportCatalog Report { get; set; } = new();
        public List<DataSource> DataSources { get; set; } = new();
        public string[] AvailableRoles { get; set; } = Array.Empty<string>();
        public HashSet<string> SelectedRoles { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
