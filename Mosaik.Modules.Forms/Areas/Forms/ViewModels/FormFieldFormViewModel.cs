using System.ComponentModel.DataAnnotations;

namespace Mosaik.Modules.Forms.Areas.Forms.ViewModels
{
    // Plan 41 Faz 2 — FormField Add/Edit (liste-tabanlı builder, drag-drop yok).
    public class FormFieldFormViewModel
    {
        public int Id { get; set; }
        public int FormDefinitionId { get; set; }

        [Required, MaxLength(80)]
        public string FieldKey { get; set; } = string.Empty;

        [Required, MaxLength(300)]
        public string Label { get; set; } = string.Empty;

        public string? HelpText { get; set; }
        public byte FieldType { get; set; }
        public bool IsRequired { get; set; }

        // Options: satır satır girilir, controller "a\nb\nc" → JSON dizisine çevirir.
        public string? OptionsRaw { get; set; }

        public int? MinLength { get; set; }
        public int? MaxLength { get; set; }
        public string? Regex { get; set; }

        public string? DefaultValue { get; set; }
        public string? Placeholder { get; set; }

        public List<(string Value, string Text)> FieldTypeOptions { get; set; } = [];
    }
}
