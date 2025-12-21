using ReportPanel.Models;

namespace ReportPanel.ViewModels
{
    public class AdminIndexViewModel
    {
        public string ActiveTab { get; set; } = "datasources";
        public string Message { get; set; } = "";
        public string MessageType { get; set; } = "";
        public string TemplateConnString { get; set; } = "";
        public List<DataSource> DataSources { get; set; } = new();
        public List<ReportCatalog> Reports { get; set; } = new();
        public List<User> Users { get; set; } = new();
    }
}
