using Mosaik.Models;

namespace Mosaik.Services.Compliance
{
    // ComplianceTemplate → ilk vade tarihi. Saf hesaplama (DB/clock dependency yok).
    //
    // Türk vergi takvimi referansı:
    //   - Quarterly (Geçici Vergi vb.): dönem sonu + 2 ay'ın DayOfMonth'u
    //     Q4(Eki-Ara)→Şubat, Q1(Oca-Mar)→Mayıs, Q2(Nis-Haz)→Ağustos, Q3(Tem-Eyl)→Kasım
    //   - Monthly: bu ayın DayOfMonth'u; geçtiyse sonraki ay
    //   - Yearly: (MonthOfYear, DayOfMonth) bu yıl; geçtiyse ertesi yıl
    //
    // Hafta sonu → Pazartesi kayması ContractObligationGenerator.SafeDate ile aynı.
    public static class ComplianceDueCalculator
    {
        // Quarterly compliance due ayları (Türk vergi takvimi sırası).
        private static readonly int[] QuarterlyDueMonths = [2, 5, 8, 11];

        public static DateOnly ComputeFirstDue(ComplianceTemplate t, DateOnly today)
        {
            var due = t.Recurrence switch
            {
                RecurrenceType.Monthly => NextMonthly(today, t.DayOfMonth ?? 1),
                RecurrenceType.Quarterly => NextQuarterly(today, t.DayOfMonth ?? 17),
                RecurrenceType.Yearly => NextYearly(today, t.MonthOfYear ?? 1, t.DayOfMonth ?? 1),
                RecurrenceType.Custom => NextMonthly(today, t.DayOfMonth ?? 1),
                _ => today.AddMonths(1)
            };
            return ShiftWeekend(due);
        }

        private static DateOnly NextMonthly(DateOnly today, int dayOfMonth)
        {
            var candidate = SafeDate(today.Year, today.Month, dayOfMonth);
            if (candidate > today) return candidate;
            var next = today.AddMonths(1);
            return SafeDate(next.Year, next.Month, dayOfMonth);
        }

        private static DateOnly NextQuarterly(DateOnly today, int dayOfMonth)
        {
            // Bugünden sonraki ilk Q-due tarihi: önce bu yıl tüm Q-due ayları, sonra ertesi yıl.
            foreach (var year in new[] { today.Year, today.Year + 1 })
            {
                foreach (var month in QuarterlyDueMonths)
                {
                    var candidate = SafeDate(year, month, dayOfMonth);
                    if (candidate > today) return candidate;
                }
            }
            // Kuramsal fallback (asla buraya düşmemeli)
            return SafeDate(today.Year + 2, QuarterlyDueMonths[0], dayOfMonth);
        }

        private static DateOnly NextYearly(DateOnly today, int monthOfYear, int dayOfMonth)
        {
            var candidate = SafeDate(today.Year, monthOfYear, dayOfMonth);
            return candidate > today
                ? candidate
                : SafeDate(today.Year + 1, monthOfYear, dayOfMonth);
        }

        private static DateOnly SafeDate(int year, int month, int day)
        {
            var maxDay = DateTime.DaysInMonth(year, month);
            return new DateOnly(year, month, Math.Min(day, maxDay));
        }

        private static DateOnly ShiftWeekend(DateOnly date) => date.DayOfWeek switch
        {
            DayOfWeek.Saturday => date.AddDays(2),
            DayOfWeek.Sunday => date.AddDays(1),
            _ => date
        };
    }
}
