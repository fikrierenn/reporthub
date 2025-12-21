using ReportPanel.Models;

namespace ReportPanel.ViewModels
{
    public class TestViewModel
    {
        public bool CanConnect { get; set; }
        public int DataSourceCount { get; set; }
        public int ReportCount { get; set; }
        public int LogCount { get; set; }
        public string? Error { get; set; }
        public List<DataSource> DataSources { get; set; } = new();
    }
}
