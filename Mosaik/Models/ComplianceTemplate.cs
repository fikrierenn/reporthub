using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Mosaik.Core.Domain;

namespace Mosaik.Models
{
    // Hazır uyum şablonları. Seed verisi (migration 49). Admin import'u → Obligation'a dönüşür.
    public class ComplianceTemplate : BaseEntity<int>
    {
        [Required]
        [MaxLength(100)]
        public string PackageName { get; set; } = string.Empty;  // "vergi", "ik", "finans"

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public ObligationCategory Category { get; set; }

        public ObligationType Type { get; set; }

        public RecurrenceType Recurrence { get; set; } = RecurrenceType.Monthly;

        public int? DayOfMonth { get; set; }

        public int? MonthOfYear { get; set; }

        public int ReminderDays { get; set; } = 7;

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
