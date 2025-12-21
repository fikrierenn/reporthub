using ReportPanel.Models;

namespace ReportPanel.ViewModels
{
    public class ReportsIndexViewModel
    {
        public string[] UserRoles { get; set; } = Array.Empty<string>();
        public List<ReportCatalog> Reports { get; set; } = new();
    }
}
