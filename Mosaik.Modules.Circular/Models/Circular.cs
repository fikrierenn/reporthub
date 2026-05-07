using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Mosaik.Core.Domain;

namespace Mosaik.Modules.Circular.Models
{
    // Plan 17 v2 (sadeleştirme 2026-05-08) — Circular ZARFI.
    // Body içermez, içerik = DailyBlock.CircularId FK üzerinden bağlı blocks.
    // 17:00 cron job tarafından otomatik oluşturulur.
    [Table("Circulars")]
    public class Circular : BaseEntity
    {
        [Key]
        [BindNever]
        public int Id { get; set; }

        // TAM-YYYYMMDD (cron job set eder, günde bir circular)
        [Required]
        [MaxLength(50)]
        [BindNever]
        public string CircularNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;  // "08 Mayıs 2026 Günlük Tamim"

        public DateTime CircularDate { get; set; }   // Hangi günün tamimi (tarih bazı)

        public DateTime PublishedAt { get; set; }   // Cron yayın anı

        // Plan 17 Faz F — AI özet sonucu (JSON: {"summary":[..], "topics":[..]} vb)
        [BindNever]
        public string? AiSummaryJson { get; set; }

        [BindNever]
        public DateTime? AiSummaryAt { get; set; }
    }
}
