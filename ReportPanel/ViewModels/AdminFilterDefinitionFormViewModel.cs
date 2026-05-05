using ReportPanel.Models;

namespace ReportPanel.ViewModels
{
    public class AdminFilterDefinitionFormViewModel
    {
        public FilterDefinition Definition { get; set; } = new();
        public List<DataSource> DataSources { get; set; } = new();
        public string Message { get; set; } = "";
        public string MessageType { get; set; } = "";
    }
}
