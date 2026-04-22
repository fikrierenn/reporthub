using ReportPanel.Models;

namespace ReportPanel.ViewModels
{
    public class AdminCategoryFormViewModel
    {
        public ReportCategory Category { get; set; } = new ReportCategory();
        public string Message { get; set; } = "";
        public string MessageType { get; set; } = "";
    }
}
