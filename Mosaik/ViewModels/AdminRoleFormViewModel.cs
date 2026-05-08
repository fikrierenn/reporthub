using Mosaik.Models;

namespace Mosaik.ViewModels
{
    public class AdminRoleFormViewModel : UserMessageViewModel
    {
        public Role Role { get; set; } = new Role();
    }
}
