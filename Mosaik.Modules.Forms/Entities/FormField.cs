using System.ComponentModel.DataAnnotations;

namespace Mosaik.Modules.Forms.Entities
{
    // Plan 41 §4.1 — Form field tanımı (12 tip).
    // FieldType: 0 text | 1 textarea | 2 number | 3 date | 4 datetime | 5 select |
    //            6 multiselect | 7 radio | 8 checkbox | 9 file | 10 signature |
    //            11 hidden | 12 section (UI header, not data)
    public class FormField
    {
        public int Id { get; set; }

        [Required]
        public int FormDefinitionId { get; set; }
        public FormDefinition? FormDefinition { get; set; }

        public int Order { get; set; }

        [Required, MaxLength(80)]
        public string FieldKey { get; set; } = string.Empty;      // "ad_soyad", "tc_kimlik"

        [Required, MaxLength(300)]
        public string Label { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? HelpText { get; set; }

        public byte FieldType { get; set; }

        public bool IsRequired { get; set; }

        public string? ValidationRules { get; set; }              // JSON: { minLength, maxLength, regex, custom }
        public string? Options { get; set; }                      // JSON: select/radio/checkbox seçenekler
        public string? ConditionalLogic { get; set; }             // G3 — structured JSON: { field, op(eq/ne), value }
        public string? DefaultValue { get; set; }

        [MaxLength(200)]
        public string? Placeholder { get; set; }

        public ICollection<FormFieldDataElementMap> DataElementMaps { get; set; } = new List<FormFieldDataElementMap>();
    }
}
