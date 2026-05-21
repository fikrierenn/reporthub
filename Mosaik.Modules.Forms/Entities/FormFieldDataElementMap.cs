using System.ComponentModel.DataAnnotations;

namespace Mosaik.Modules.Forms.Entities
{
    // Plan 41 §4.3 — KVKK Plan 40 entegrasyon.
    // Her field bir DataElement'e bağlı (yayında geçişi için zorunlu).
    // DataElementId Plan 40'tan gelecek FK — şimdilik soft reference (Plan 40 başlamadan).
    // UsageType: 0 collects | 1 derives
    public class FormFieldDataElementMap
    {
        public int Id { get; set; }

        [Required]
        public int FormFieldId { get; set; }
        public FormField? FormField { get; set; }

        [Required]
        public int DataElementId { get; set; }                    // Plan 40 KVKK DataElement (soft ref şimdilik)

        public byte UsageType { get; set; }                       // 0 collects 1 derives

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}
