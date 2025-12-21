using ReportPanel.Models;

namespace ReportPanel.ViewModels
{
    public class ReportRunViewModel
    {
        public ReportCatalog? SelectedReport { get; set; }
        public List<ReportParamField> ParamFields { get; set; } = new();
        public Dictionary<string, string> ParamValues { get; set; } = new();
        public List<Dictionary<string, object>> RunData { get; set; } = new();
        public bool RunSuccess { get; set; }
        public string RunError { get; set; } = "";
        public string RunMessage { get; set; } = "";
        public int RunRowCount { get; set; }
        public long RunDurationMs { get; set; }
        public string ViewMode { get; set; } = "";
        public string BodyClass { get; set; } = "";
        public string ResultSearch { get; set; } = "";
    }
}
