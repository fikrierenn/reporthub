using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Mosaik.Models.Intelligence
{
    // Plan 38 — Living Org Map polymorphic ilişki tablosu.
    // SourceType/SourceId → RelationType → TargetType/TargetId (FK yok, validation servis katmanında).
    [Table("EntityRelations")]
    public class EntityRelation
    {
        [Key]
        [BindNever]
        public int Id { get; set; }

        public int FirmaId { get; set; }

        [Required]
        [MaxLength(50)]
        public string SourceType { get; set; } = string.Empty;

        public int SourceId { get; set; }

        [Required]
        [MaxLength(50)]
        public string RelationType { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string TargetType { get; set; } = string.Empty;

        public int TargetId { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? Weight { get; set; }

        public DateTime ValidFrom { get; set; } = DateTime.UtcNow;

        public DateTime? ValidTo { get; set; }

        [MaxLength(50)]
        public string? SourceSystem { get; set; }

        [BindNever]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BindNever]
        public int? CreatedBy { get; set; }
    }
}
