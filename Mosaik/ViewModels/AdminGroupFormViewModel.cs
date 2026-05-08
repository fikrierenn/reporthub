using Mosaik.Models;

namespace Mosaik.ViewModels
{
    public class AdminGroupFormViewModel : UserMessageViewModel
    {
        public ReportGroup Group { get; set; } = new ReportGroup();
    }
}
