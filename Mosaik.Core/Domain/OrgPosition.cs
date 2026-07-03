using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Mosaik.Core.Domain
{
    // Plan 20 — Organizasyon şeması düğümü. (Plan 55: holder + ZirveMatchKey eklendi.)
    // Zirve incumbent eşleşme anahtarı = ZirveMatchKey (yoksa Title); Code artık org.json id
    // suffix'i taşıyabilir, o yüzden match Code'a DEĞİL ZirveMatchKey'e yapılır.
    // Aynı ünvan birden çok pozisyon/kişi olabilir — incumbent çoklu düşer.
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

        // Plan 55 — org.json'dan gelen resmi holder kişi (pozisyonun dondurulmuş sahibi).
        // Zirve canlı-incumbent'tan ayrı: bu org şemasının kayıtlı sahibi, o anlık PDKS gerçeği.
        [MaxLength(150)]
        public string? HolderName { get; set; }

        [MaxLength(50)]
        public string? HolderPersonelno { get; set; }

        // Zirve Unvan eşleşme anahtarı (temiz ünvan). Code org.json id suffix'i alabildiği için
        // incumbent match Code yerine buna bakar. NULL ise servis Title'a fallback yapar.
        [MaxLength(150)]
        public string? ZirveMatchKey { get; set; }
    }
}
