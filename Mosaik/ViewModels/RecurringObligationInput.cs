using System.ComponentModel.DataAnnotations;
using Mosaik.Models;

namespace Mosaik.ViewModels
{
    // Plan 25 — sözleşme oluştururken kullanıcı 0+ tekrarlayan yükümlülük tanımlar.
    // Generator service bu input'tan ContractRecurrence + N adet ContractObligation üretir.
    public class RecurringObligationInput
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public ObligationType Type { get; set; } = ObligationType.Payment;
        public ObligationCategory Category { get; set; } = ObligationCategory.Finance;

        public decimal? Amount { get; set; }

        [MaxLength(3)]
        public string Currency { get; set; } = "TRY";

        public RecurrenceType RecurrenceType { get; set; } = RecurrenceType.Monthly;

        // Custom periyot için ay sayısı (Monthly=1, Quarterly=3, Yearly=12 zaten implicit).
        public int IntervalValue { get; set; } = 1;

        // Cap 28: 29-31 günleri Şubat'a düşünce gün sapması olur.
        [Range(1, 28)]
        public int DayOfMonth { get; set; } = 1;

        [Range(1, 12)]
        public int? MonthOfYear { get; set; }   // Yearly için

        public DateOnly? StartDate { get; set; }   // null → sözleşme StartDate'i
        public DateOnly? EndDate { get; set; }     // null → sözleşme EndDate'i

        [MaxLength(2000)]
        public string? Notes { get; set; }
    }
}
