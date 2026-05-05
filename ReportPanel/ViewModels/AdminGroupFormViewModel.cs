using ReportPanel.Models;

namespace ReportPanel.ViewModels
{
    public class AdminGroupFormViewModel
    {
        public ReportGroup Group { get; set; } = new ReportGroup();
        public string Message { get; set; } = "";
        public string MessageType { get; set; } = "";
    }
}
