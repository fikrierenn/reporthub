using Mosaik.Models;

namespace Mosaik.ViewModels
{
    public class AdminDataSourceFormViewModel
    {
        public DataSource DataSource { get; set; } = new();
        public string TemplateConnString { get; set; } = "";
    }
}
