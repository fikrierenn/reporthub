using Mosaik.Models;

namespace Mosaik.ViewModels
{
    public class AdminFilterDefinitionFormViewModel : UserMessageViewModel
    {
        public FilterDefinition Definition { get; set; } = new();
        public List<DataSource> DataSources { get; set; } = new();
    }
}
