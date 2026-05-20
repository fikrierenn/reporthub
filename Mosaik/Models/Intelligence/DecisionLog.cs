using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Mosaik.Models.Intelligence
{
    // Plan 38 — Decision Memory.
    // AuditLog ne olduğunu der, DecisionLog neden + alternatifler + beklenen/gerçek sonuç der.
    [Table("DecisionLogs")]
    public class DecisionLog
    {
        [Key]
        [BindNever]
        public int Id { get; set; }

        public int FirmaId { get; set; }

        [Required]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        public string? Rationale { get; set; }

        public int MadeBy { get; set; }

        public DateTime MadeAt { get; set; } = DateTime.UtcNow;

        public string? AlternativesJson { get; set; }

        public string? ExpectedOutcome { get; set; }

        public string? ActualOutcome { get; set; }

        public string? KpiImpactJson { get; set; }

        [MaxLength(50)]
        public string? RelatedEntityType { get; set; }

        public int? RelatedEntityId { get; set; }

        public long? RelatedAuditId { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Active";
    }
}
