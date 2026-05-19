using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mosaik.Models
{
    public class ModuleRoleAccess
    {
        public int Id { get; set; }

        public int ModuleId { get; set; }

        [ForeignKey(nameof(ModuleId))]
        public AppModule? Module { get; set; }

        [Required, MaxLength(50)]
        public string RoleName { get; set; } = string.Empty;
    }
}
