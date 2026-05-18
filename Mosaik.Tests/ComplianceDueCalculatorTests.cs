using Mosaik.Models;
using Mosaik.Services.Compliance;
using Xunit;

namespace Mosaik.Tests;

public class ComplianceDueCalculatorTests
{
    // Quarterly: Geçici Vergi pattern — dönem sonu + 2 ay'ın 17'si.

    [Fact]
    public void Quarterly_GeciciVergi_MayisGununden_Sonra_AgustosOlmali()
    {
        // 19 Mayıs 2026 → Q1 beyan (17 May) kaçtı → next = 17 Ağustos 2026 (Pazartesi)
        var t = new ComplianceTemplate
        {
            Recurrence = RecurrenceType.Quarterly,
            DayOfMonth = 17
        };
        var due = ComplianceDueCalculator.ComputeFirstDue(t, new DateOnly(2026, 5, 19));
        Assert.Equal(new DateOnly(2026, 8, 17), due);
    }

    [Fact]
    public void Quarterly_GeciciVergi_MayisOnce_MayisDonemi()
    {
        // 1 Mayıs 2026 → Q4 2025 kaçtı, Q1 2026 beyan henüz → 17 Mayıs 2026 (Pazar) → 18 (Pazartesi)
        var t = new ComplianceTemplate
        {
            Recurrence = RecurrenceType.Quarterly,
            DayOfMonth = 17
        };
        var due = ComplianceDueCalculator.ComputeFirstDue(t, new DateOnly(2026, 5, 1));
        Assert.Equal(new DateOnly(2026, 5, 18), due);
    }

    [Fact]
    public void Quarterly_OcakBaslangic_SubatDonemi()
    {
        // 15 Ocak 2026 → Q4 2025 beyan henüz → 17 Şubat 2026 (Salı)
        var t = new ComplianceTemplate
        {
            Recurrence = RecurrenceType.Quarterly,
            DayOfMonth = 17
        };
        var due = ComplianceDueCalculator.ComputeFirstDue(t, new DateOnly(2026, 1, 15));
        Assert.Equal(new DateOnly(2026, 2, 17), due);
    }

    [Fact]
    public void Quarterly_AralikIcin_ErtesiYilSubat()
    {
        // 1 Aralık 2026 → Q3 (17 Kasım) kaçtı → Q4 due Şubat 2027
        var t = new ComplianceTemplate
        {
            Recurrence = RecurrenceType.Quarterly,
            DayOfMonth = 17
        };
        var due = ComplianceDueCalculator.ComputeFirstDue(t, new DateOnly(2026, 12, 1));
        Assert.Equal(new DateOnly(2027, 2, 17), due);
    }

    // Monthly: bu ayın DayOfMonth'u; geçtiyse sonraki ay.

    [Fact]
    public void Monthly_GunGecmediyse_BuAy()
    {
        // KDV 28 — 19 Mayıs 2026 → 28 Mayıs (Perşembe)
        var t = new ComplianceTemplate { Recurrence = RecurrenceType.Monthly, DayOfMonth = 28 };
        var due = ComplianceDueCalculator.ComputeFirstDue(t, new DateOnly(2026, 5, 19));
        Assert.Equal(new DateOnly(2026, 5, 28), due);
    }

    [Fact]
    public void Monthly_GunGectiyse_SonrakiAy()
    {
        // KDV 28 — 29 Mayıs 2026 → 28 Haziran (Pazar) → 29 (Pazartesi)
        var t = new ComplianceTemplate { Recurrence = RecurrenceType.Monthly, DayOfMonth = 28 };
        var due = ComplianceDueCalculator.ComputeFirstDue(t, new DateOnly(2026, 5, 29));
        Assert.Equal(new DateOnly(2026, 6, 29), due);
    }

    // Yearly: (MonthOfYear, DayOfMonth) bu yıl; geçtiyse ertesi yıl.

    [Fact]
    public void Yearly_AyGecmediyse_BuYil()
    {
        // Kurumlar Vergisi 30 Nisan — 1 Mart 2026 → 30 Nisan 2026 (Perşembe)
        var t = new ComplianceTemplate
        {
            Recurrence = RecurrenceType.Yearly,
            MonthOfYear = 4,
            DayOfMonth = 30
        };
        var due = ComplianceDueCalculator.ComputeFirstDue(t, new DateOnly(2026, 3, 1));
        Assert.Equal(new DateOnly(2026, 4, 30), due);
    }

    [Fact]
    public void Yearly_AyGectiyse_ErtesiYil()
    {
        // Kurumlar Vergisi 30 Nisan — 19 Mayıs 2026 → 30 Nisan 2027 (Cuma)
        var t = new ComplianceTemplate
        {
            Recurrence = RecurrenceType.Yearly,
            MonthOfYear = 4,
            DayOfMonth = 30
        };
        var due = ComplianceDueCalculator.ComputeFirstDue(t, new DateOnly(2026, 5, 19));
        Assert.Equal(new DateOnly(2027, 4, 30), due);
    }

    [Fact]
    public void Yearly_MTV1_OcakSonu_HaftaSonu_Pazartesi()
    {
        // MTV 1. Taksit MonthOfYear=1 DayOfMonth=31 — 19 Mayıs 2026 → 31 Ocak 2027 (Pazar) → 1 Şubat 2027 (Pazartesi)
        var t = new ComplianceTemplate
        {
            Recurrence = RecurrenceType.Yearly,
            MonthOfYear = 1,
            DayOfMonth = 31
        };
        var due = ComplianceDueCalculator.ComputeFirstDue(t, new DateOnly(2026, 5, 19));
        Assert.Equal(new DateOnly(2027, 2, 1), due);
    }
}
