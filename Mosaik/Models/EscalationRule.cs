using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Mosaik.Models
{
    // Plan 54 M4 — Dashboard→Alert (Operational Intelligence, BI değil).
    // Bir raporun metriği (rs[ResultSet] üzerinde Aggregation(Column)) eşiği aştığında
    // EscalationSweeperJob (Hangfire, saatlik) INotificationService ile bildirim fire eder.
    // Değerlendirme GLOBAL — user-data-filter enjekte edilmez (sistem-geneli eşik).
    public class EscalationRule
    {
        [Key]
        [BindNever]
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        // Metrik kaynağı rapor (ReportCatalog). SP'si parametresiz olmalı — sweeper boş param geçer.
        public int ReportId { get; set; }

        // Hangi result set (0-based). Çok-RS dashboard SP'lerinde KPI satırı genelde rs0/rs1.
        public int ResultSet { get; set; }

        // Metrik kolonu (rs satırlarındaki kolon adı). Aggregation="count" ise kullanılmaz.
        [Required]
        [MaxLength(128)]
        public string Column { get; set; } = string.Empty;

        // first | count | sum | avg | min | max  (dashboard KPI agg sözlüğüyle uyumlu alt-küme)
        [Required]
        [MaxLength(10)]
        public string Aggregation { get; set; } = "first";

        // gt | gte | lt | lte | eq
        [Required]
        [MaxLength(4)]
        public string Operator { get; set; } = "gt";

        public decimal Threshold { get; set; }

        // Bildirim alıcıları — CSV userId listesi ("3,7,12").
        [Required]
        [MaxLength(500)]
        public string NotifyUserIds { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        // --- Çalıştırma durumu (sweeper günceller, form binding'e kapalı) ---

        [BindNever]
        public decimal? LastValue { get; set; }

        [BindNever]
        public DateTime? LastEvaluatedAt { get; set; }

        [BindNever]
        public DateTime? LastFiredAt { get; set; }

        [BindNever]
        [MaxLength(500)]
        public string? LastError { get; set; }

        [BindNever]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BindNever]
        public int CreatedBy { get; set; }

        [ForeignKey(nameof(ReportId))]
        public virtual ReportCatalog? Report { get; set; }
    }
}
