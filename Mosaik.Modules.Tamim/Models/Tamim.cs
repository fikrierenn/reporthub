using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Mosaik.Core.Domain;

namespace Mosaik.Modules.Tamim.Models
{
    public enum TamimStatus
    {
        Draft = 0,        // Taslak — sadece editör görür
        Pending = 1,      // Onay bekliyor
        Approved = 2,     // Onaylandı, yayın bekliyor
        Published = 3,    // Yayında — herkes görür
        Rejected = 4,     // Reddedildi
        Archived = 5      // Arşivlendi
    }

    // Plan 17 Faz B — Tamim entity. BaseEntity inherit, [BindNever] PK + timestamp.
    // Cross-modül FK (CreatedById → Mosaik.Models.User): navigation property YOK,
    // sadece int FK. Service layer User lookup yapar.
    public class Tamim : BaseEntity
    {
        [Key]
        [BindNever]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;  // HTML / markdown / düz metin

        public TamimStatus Status { get; set; } = TamimStatus.Draft;

        // İşletme alanları
        public DateTime? PublishDate { get; set; }   // Yayın tarihi (null = manuel yayınlanır)
        public DateTime? ExpiresAt { get; set; }     // Geçerlilik sonu (null = süresiz)

        // İlişkiler (cross-modül FK — navigation YOK, sadece int)
        [BindNever]
        public int CreatedById { get; set; }         // Tamimi oluşturan User.UserId
        public int? ApprovedById { get; set; }       // Onaylayan User.UserId (null ise henüz onaylanmadı)
    }
}
