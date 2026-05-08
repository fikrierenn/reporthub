using Mosaik.Models;

namespace Mosaik.ViewModels
{
    public class AdminReportFormViewModel : UserMessageViewModel
    {
        public ReportCatalog Report { get; set; } = new();
        public List<DataSource> DataSources { get; set; } = new();
        public List<Role> AvailableRoles { get; set; } = new();
        public HashSet<int> SelectedRoleIds { get; set; } = new();
        public List<ReportGroup> AvailableGroups { get; set; } = new();
        public HashSet<int> SelectedGroupIds { get; set; } = new();
    }
}
