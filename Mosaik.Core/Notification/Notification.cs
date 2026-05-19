using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Mosaik.Core.Domain;

namespace Mosaik.Core.Notification
{
    // Plan 17 Faz H — cross-modül bildirim. Tamim, HR sync, Approval, Görev hepsi kullanır.
    // EntityType + EntityId pattern: tıklayınca TargetUrl'e gider.
    public class Notification : BaseEntity, IAuditable
    {
        [Key]
        [BindNever]
        public int Id { get; set; }

        // Hedef kullanıcı (Mosaik.User.UserId)
        public int UserId { get; set; }

        // İlişkili entity (örn. "Circular" + CircularId, "ApprovalRequest" + RequestId)
        [Required]
        [MaxLength(50)]
        public string EntityType { get; set; } = string.Empty;

        public int? EntityId { get; set; }

        // Lookup `BildirimTuru` value code (örn. "CircularPublished", "ApprovalPending")
        [MaxLength(50)]
        public string? NotificationType { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Message { get; set; }

        // Tıklanınca açılacak relative URL (örn. "/Circular/Circular/Details/42")
        [MaxLength(500)]
        public string? TargetUrl { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime? ReadAt { get; set; }

        // N-1 Hibrit idempotency — caller-defined dedup key (örn. "obligation_reminder:42:20260519").
        // Filtered unique index: NULL ise eşsizlik kontrolü dışında tutulur.
        [MaxLength(256)]
        public string? ExternalKey { get; set; }
    }
}
