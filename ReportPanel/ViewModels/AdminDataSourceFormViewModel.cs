using ReportPanel.Models;

namespace ReportPanel.ViewModels
{
    public class AdminDataSourceFormViewModel
    {
        public DataSource DataSource { get; set; } = new();
        public string TemplateConnString { get; set; } = "";
    }
}
