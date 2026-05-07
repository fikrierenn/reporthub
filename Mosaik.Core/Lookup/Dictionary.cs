using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Mosaik.Core.Domain;

namespace Mosaik.Core.Lookup
{
    // Operax DictionaryType + YonetIQ Lookup birleşim:
    // Group + Value soft enum + IsActive soft-delete + DisplayOrder.
    // Hard-code string yerine: departman/pozisyon/sube/durum/öncelik tek tabloda.
    public class DictionaryType : BaseEntity
    {
        [Key]
        [BindNever]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public List<DictionaryValue> Values { get; set; } = new();
    }

    public class DictionaryValue : BaseEntity
    {
        [Key]
        [BindNever]
        public int Id { get; set; }

        public int TypeId { get; set; }
        public DictionaryType? Type { get; set; }

        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Label { get; set; } = string.Empty;

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
