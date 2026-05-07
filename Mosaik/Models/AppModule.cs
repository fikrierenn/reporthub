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

        // Plan 16.6 — Modular Monolith. Mosaik.Modules.* assembly adı.
        // ModuleLoader bu assembly'yi yükler, IMosaikModule impl'i bulur.
        // Core modüller (rapor/dashboard/brand) için NULL.
        [MaxLength(200)]
        public string? AssemblyName { get; set; }

        // 'core' (mevcut: rapor, dashboard, brand) | 'extension' (Plan 17+ vNext)
        [Required, MaxLength(20)]
        public string ModuleType { get; set; } = "core";
    }
}
