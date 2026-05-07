using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Mosaik.Core.Domain;

namespace Mosaik.Modules.Circular.Models
{
    // Plan 17 v2 (sadeleştirme 2026-05-08) — Genel Müdürlük çalışanlarının gün
    // içinde girdiği duyuru/karar/uyarı/hadise kaydı. 17:00 cron job o günkü
    // blokları toplayıp Circular zarfına bağlar (CircularId set).
    //
    // Durum bilgisi: CircularId IS NULL = bekliyor, IS NOT NULL = yayında.
    // Onay akışı YOK (basitlik). Editor onayı Plan 17.1'e ertelendi.
    [Table("DailyBlocks")]
    public class DailyBlock : BaseEntity
    {
        [Key]
        [BindNever]
        public int Id { get; set; }

        // BLK-YYYYMMDD-NNN otomatik (BlockService set eder)
        [Required]
        [MaxLength(50)]
        [BindNever]
        public string BlockNumber { get; set; } = string.Empty;

        [BindNever]
        public int CreatedById { get; set; }   // User.UserId cross-csproj FK

        [Required]
        [MaxLength(100)]
        public string Department { get; set; } = string.Empty;  // Plan 18B sonrası FK

        [Required]
        [MaxLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        public DateTime BlockDate { get; set; } = DateTime.UtcNow.Date;

        // DictionaryValue.Id (Mosaik.Core.Lookup, Code="blockType")
        public int BlockTypeId { get; set; }

        public bool IsUrgent { get; set; }

        // Circular'e bağlandı mı? NULL=bekliyor, NOT NULL=yayında.
        public int? CircularId { get; set; }

        // Soft-delete (yayınlanmamış blokları kaldır)
        public bool IsActive { get; set; } = true;
    }
}
