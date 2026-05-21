using System.ComponentModel.DataAnnotations;

namespace Mosaik.Modules.Forms.Entities
{
    // Plan 41 §4.6 — Sürüm versioning snapshot.
    // Yayına alındığında SchemaJson snapshot alır; submission'lar versiyon referansı saklar.
    public class FormDefinitionVersion
    {
        public int Id { get; set; }

        [Required]
        public int FormDefinitionId { get; set; }
        public FormDefinition? FormDefinition { get; set; }

        [Required]
        public int Version { get; set; }

        [Required]
        public string SchemaJson { get; set; } = string.Empty;

        public DateTime PublishedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public int PublishedBy { get; set; }
    }
}
