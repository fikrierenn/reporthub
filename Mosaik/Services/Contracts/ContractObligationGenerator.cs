using Mosaik.Models;
using Mosaik.ViewModels;

namespace Mosaik.Services.Contracts
{
    // Plan 25 — RecurringObligationInput'tan ContractRecurrence + N adet ContractObligation üretir.
    // Saf hesaplama (DB'ye yazmaz). Controller transaction içinde çağırır.
    public static class ContractObligationGenerator
    {
        // RecurrenceType başına maksimum dönem sayısı (10 yıl referansı).
        private static int GetMaxIterations(RecurrenceType type, int intervalValue) => type switch
        {
            RecurrenceType.Yearly   => 10,
            RecurrenceType.Quarterly => 40,
            RecurrenceType.Monthly  => 120,
            RecurrenceType.Custom   => intervalValue > 0 ? (120 / intervalValue + 10) : 120,
            _                       => 120
        };

        public sealed record GeneratedSet(
            ContractRecurrence Recurrence,
            IReadOnlyList<ContractObligation> Obligations);

        public static IReadOnlyList<GeneratedSet> Generate(
            int contractId,
            int firmaId,
            DateOnly contractStart,
            DateOnly contractEnd,
            IEnumerable<RecurringObligationInput> inputs,
            string? createdBy)
        {
            var now = DateTime.UtcNow;
            var result = new List<GeneratedSet>();

            foreach (var input in inputs)
            {
                var effectiveStart = input.StartDate ?? contractStart;
                var effectiveEnd = input.EndDate ?? contractEnd;

                if (effectiveStart == default || effectiveEnd == default)
                    throw new ArgumentException(
                        $"Periyodik yükümlülük '{input.Title}' için başlangıç/bitiş tarihi belirlenemedi.");

                if (effectiveEnd < effectiveStart)
                    throw new ArgumentException(
                        $"Periyodik yükümlülük '{input.Title}' bitişi başlangıçtan önce olamaz.");

                var recurrence = new ContractRecurrence
                {
                    RecurrenceType = input.RecurrenceType,
                    IntervalValue = NormalizeInterval(input.RecurrenceType, input.IntervalValue),
                    DayOfMonth = input.DayOfMonth,
                    MonthOfYear = input.RecurrenceType == RecurrenceType.Yearly ? input.MonthOfYear : null,
                    StartDate = effectiveStart,
                    EndDate = effectiveEnd,
                    CreatedAt = now,
                    CreatedBy = createdBy
                };

                var dueDates = ComputeDueDates(input, effectiveStart, effectiveEnd);

                var obligations = dueDates.Select(due => new ContractObligation
                {
                    FirmaId = firmaId,
                    ContractId = contractId,
                    Title = input.Title,
                    Category = input.Category,
                    Type = input.Type,
                    Amount = input.Amount,
                    Currency = string.IsNullOrWhiteSpace(input.Currency) ? "TRY" : input.Currency,
                    DueDate = due,
                    Status = ObligationStatus.Pending,
                    Source = ObligationSource.Manual,
                    Notes = input.Notes,
                    CreatedAt = now,
                    CreatedBy = createdBy
                }).ToList();

                result.Add(new GeneratedSet(recurrence, obligations));
            }

            return result;
        }

        // Public — test edilebilirlik için.
        public static IReadOnlyList<DateOnly> ComputeDueDates(
            RecurringObligationInput input,
            DateOnly effectiveStart,
            DateOnly effectiveEnd)
        {
            var dueDates = new List<DateOnly>();
            var dayOfMonth = Math.Clamp(input.DayOfMonth, 1, 28);

            // İlk vade tarihini hesapla.
            DateOnly current = input.RecurrenceType switch
            {
                RecurrenceType.Yearly => FirstYearlyDue(effectiveStart, input.MonthOfYear ?? effectiveStart.Month, dayOfMonth),
                _ => FirstMonthlyDue(effectiveStart, dayOfMonth)
            };

            int maxIter = GetMaxIterations(input.RecurrenceType, input.IntervalValue);
            int iterations = 0;
            while (current <= effectiveEnd)
            {
                if (++iterations > maxIter)
                {
                    string limitLabel = input.RecurrenceType switch
                    {
                        RecurrenceType.Yearly    => "10 yıl (10 dönem)",
                        RecurrenceType.Quarterly => "10 yıl (40 dönem)",
                        RecurrenceType.Monthly   => "10 yıl (120 dönem)",
                        RecurrenceType.Custom    => $"izin verilen maksimum ({maxIter} dönem)",
                        _                        => $"{maxIter} dönem"
                    };
                    throw new ArgumentException(
                        $"Yükümlülük '{input.Title}' için tarih aralığı çok uzun. " +
                        $"Maksimum: {limitLabel}. Bitiş tarihini kısaltın.");
                }

                if (current >= effectiveStart)
                    dueDates.Add(current);

                current = AdvanceDate(current, input.RecurrenceType, input.IntervalValue, dayOfMonth);
            }

            return dueDates;
        }

        // İlk aylık vade: effectiveStart'taki gün >= dayOfMonth ise sonraki ay; değilse aynı ay.
        private static DateOnly FirstMonthlyDue(DateOnly start, int dayOfMonth)
        {
            if (start.Day <= dayOfMonth)
                return SafeDate(start.Year, start.Month, dayOfMonth);
            // Sonraki aya geç
            var next = start.AddMonths(1);
            return SafeDate(next.Year, next.Month, dayOfMonth);
        }

        // İlk yıllık vade: effectiveStart'tan itibaren ilk geçerli (monthOfYear, dayOfMonth) tarihi.
        private static DateOnly FirstYearlyDue(DateOnly start, int monthOfYear, int dayOfMonth)
        {
            var candidate = SafeDate(start.Year, monthOfYear, dayOfMonth);
            return candidate >= start ? candidate : SafeDate(start.Year + 1, monthOfYear, dayOfMonth);
        }

        private static DateOnly AdvanceDate(DateOnly current, RecurrenceType type, int intervalValue, int dayOfMonth)
        {
            int monthsToAdd = type switch
            {
                RecurrenceType.Monthly => 1,
                RecurrenceType.Quarterly => 3,
                RecurrenceType.Yearly => 12,
                RecurrenceType.Custom => Math.Max(1, intervalValue),
                _ => 1
            };
            var next = current.AddMonths(monthsToAdd);
            return SafeDate(next.Year, next.Month, dayOfMonth);
        }

        // Şubat 31 → Şubat 28/29 cap + hafta sonu → Pazartesi.
        private static DateOnly SafeDate(int year, int month, int day)
        {
            var maxDay = DateTime.DaysInMonth(year, month);
            var date = new DateOnly(year, month, Math.Min(day, maxDay));
            return date.DayOfWeek switch
            {
                DayOfWeek.Saturday => date.AddDays(2),
                DayOfWeek.Sunday   => date.AddDays(1),
                _                  => date
            };
        }

        private static int NormalizeInterval(RecurrenceType type, int requested) =>
            type switch
            {
                RecurrenceType.Monthly => 1,
                RecurrenceType.Quarterly => 3,
                RecurrenceType.Yearly => 12,
                RecurrenceType.Custom => Math.Max(1, requested),
                _ => 1
            };
    }
}
