using Mosaik.Models;

namespace Mosaik.ViewModels
{
    public class AdminRoleFormViewModel
    {
        public Role Role { get; set; } = new Role();
        public string Message { get; set; } = "";
        public string MessageType { get; set; } = "";
    }
}
