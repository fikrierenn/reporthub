using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Mosaik.Core.Domain
{
    // Plan 20 — Organizasyon şeması düğümü.
    // Code = Zirve `Unvan` ile eşleşen string (case-insensitive trim match).
    // Tek node modeli: aynı Unvan farklı lokasyonlarda da tek pozisyon — incumbent çoklu olabilir.
    // Hiyerarşi self-ref Parent/Children. Cycle detection servis tarafında.
    public class OrgPosition : BaseEntity, IAuditable
    {
        [Key]
        [BindNever]
        public int Id { get; set; }

        // Zirve Unvan eşleşme anahtarı. Trim + case-insensitive uniqueness.
        [Required]
        [MaxLength(100)]
        public string Code { get; set; } = string.Empty;

        // Görsel etiket — Code'dan farklı olabilir (TR karakter, kısaltma genişletme).
        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        public int? ParentPositionId { get; set; }
        public OrgPosition? Parent { get; set; }

        public List<OrgPosition> Children { get; set; } = new();

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;

        [MaxLength(500)]
        public string? Description { get; set; }
    }
}
