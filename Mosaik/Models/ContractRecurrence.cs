using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Mosaik.Core.Domain;

namespace Mosaik.Models
{
    // Plan 25 — Yükümlülük tekrar kuralı.
    // DailyObligationCheckWorker her gün çalışır, DueDate geçmiş Pending'leri kontrol eder.
    // MonthlyRecurrenceWorker her ayın 1'inde bu kuraldan yeni yükümlülük üretir.
    public class ContractRecurrence : BaseEntity<int>
    {
        public RecurrenceType RecurrenceType { get; set; } = RecurrenceType.Monthly;
        public int IntervalValue { get; set; } = 1;     // Custom için: her N birim
        public int? DayOfMonth { get; set; }            // Aylık: ayın kaçında
        public int? MonthOfYear { get; set; }           // Yıllık: yılın kaçıncı ayında

        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public int? MaxOccurrences { get; set; }

        // Nav properties
        public ICollection<ContractObligation> Obligations { get; set; } = new List<ContractObligation>();
    }
}
