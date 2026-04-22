using ReportPanel.Models;

namespace ReportPanel.ViewModels
{
    public class AdminUserFormViewModel
    {
        public User User { get; set; } = new();
        public string Message { get; set; } = "";
        public string MessageType { get; set; } = "";
        public List<Role> AvailableRoles { get; set; } = new();
        public HashSet<int> SelectedRoleIds { get; set; } = new();
        public List<UserDataFilter> DataFilters { get; set; } = new();
        public List<DataSource> DataSources { get; set; } = new();
    }
}
