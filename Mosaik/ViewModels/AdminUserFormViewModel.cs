using Mosaik.Models;

namespace Mosaik.ViewModels
{
    public class AdminUserFormViewModel : UserMessageViewModel
    {
        public User User { get; set; } = new();
        public List<Role> AvailableRoles { get; set; } = new();
        public HashSet<int> SelectedRoleIds { get; set; } = new();
        public List<UserDataFilter> DataFilters { get; set; } = new();
        public List<DataSource> DataSources { get; set; } = new();
        // Plan 07 Faz 5: aktif FilterDefinition'lar — partial view her biri icin section render eder.
        public List<FilterDefinition> FilterDefinitions { get; set; } = new();
    }
}
