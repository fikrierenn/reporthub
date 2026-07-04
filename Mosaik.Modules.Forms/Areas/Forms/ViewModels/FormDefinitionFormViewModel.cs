using System.ComponentModel.DataAnnotations;

namespace Mosaik.Modules.Forms.Areas.Forms.ViewModels
{
    // Plan 41 Faz 2 — FormDefinition Create/Edit (metadata).
    public class FormDefinitionFormViewModel
    {
        public int Id { get; set; }

        [Required, MaxLength(120)]
        public string Slug { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [MaxLength(80)]
        public string? Category { get; set; }

        public bool IsPublic { get; set; }
        public bool IsAnonymous { get; set; }
        public bool IsEncrypted { get; set; }

        // Plan 57 B3 — submit'te tetiklenecek onay şablonu (opsiyonel; null = tetikleme yok).
        public int? TriggersWorkflowId { get; set; }
    }
}
