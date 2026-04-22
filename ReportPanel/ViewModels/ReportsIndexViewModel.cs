using ReportPanel.Models;

namespace ReportPanel.ViewModels
{
    public class ReportsIndexViewModel
    {
        public string[] UserRoles { get; set; } = Array.Empty<string>();
        public List<ReportCatalog> Reports { get; set; } = new();
        public HashSet<int> FavoriteReportIds { get; set; } = new();
        public List<string> Categories { get; set; } = new();
        public string SearchTerm { get; set; } = "";
        public string SelectedCategory { get; set; } = "";
    }
}
