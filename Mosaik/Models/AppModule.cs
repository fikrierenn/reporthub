using System.ComponentModel.DataAnnotations;

namespace Mosaik.Models
{
    public class AppModule
    {
        public int ModuleId { get; set; }

        [Required, MaxLength(50)]
        public string ModuleKey { get; set; } = "";

        [Required, MaxLength(100)]
        public string DisplayName { get; set; } = "";

        public bool IsEnabled { get; set; } = true;

        public int SortOrder { get; set; } = 0;
    }
}
