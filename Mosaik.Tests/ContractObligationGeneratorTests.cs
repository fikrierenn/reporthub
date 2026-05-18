using Mosaik.Models;
using Mosaik.Services.Contracts;
using Mosaik.ViewModels;
using Xunit;

namespace Mosaik.Tests;

public class ContractObligationGeneratorTests
{
    [Fact]
    public void Monthly_12Months_Produces12Obligations()
    {
        var input = new RecurringObligationInput
        {
            Title = "Aylık Kira",
            Type = ObligationType.Payment,
            Amount = 10000,
            Currency = "TRY",
            RecurrenceType = RecurrenceType.Monthly,
            DayOfMonth = 5,
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 12, 31)
        };

        var dueDates = ContractObligationGenerator.ComputeDueDates(
            input, input.StartDate!.Value, input.EndDate!.Value);

        Assert.Equal(12, dueDates.Count);
        // 5 Ocak 2026 = Pazartesi (kayma yok), 5 Aralık 2026 = Cumartesi → 7 Aralık (Pazartesi)
        Assert.Equal(new DateOnly(2026, 1, 5), dueDates[0]);
        Assert.Equal(new DateOnly(2026, 12, 7), dueDates[11]);
    }

    [Fact]
    public void Quarterly_OneYear_Produces4Obligations()
    {
        var input = new RecurringObligationInput
        {
            Title = "Çeyreklik KDV",
            RecurrenceType = RecurrenceType.Quarterly,
            DayOfMonth = 26,
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 12, 31)
        };

        var dueDates = ContractObligationGenerator.ComputeDueDates(
            input, input.StartDate!.Value, input.EndDate!.Value);

        Assert.Equal(4, dueDates.Count);
        // 26 Oca/Nis/Tem/Eki 2026 hafta sonu kontrolü: 26 Nisan = Pazar → 27 Nisan (Pazartesi)
        Assert.Equal(new DateOnly(2026, 1, 26), dueDates[0]);
        Assert.Equal(new DateOnly(2026, 4, 27), dueDates[1]);
        Assert.Equal(new DateOnly(2026, 7, 27), dueDates[2]);
        Assert.Equal(new DateOnly(2026, 10, 26), dueDates[3]);
    }

    [Fact]
    public void Yearly_TwoYears_Produces2Obligations()
    {
        var input = new RecurringObligationInput
        {
            Title = "Yıllık Sigorta",
            RecurrenceType = RecurrenceType.Yearly,
            MonthOfYear = 6,
            DayOfMonth = 15,
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2027, 12, 31)
        };

        var dueDates = ContractObligationGenerator.ComputeDueDates(
            input, input.StartDate!.Value, input.EndDate!.Value);

        Assert.Equal(2, dueDates.Count);
        Assert.Equal(new DateOnly(2026, 6, 15), dueDates[0]);
        Assert.Equal(new DateOnly(2027, 6, 15), dueDates[1]);
    }

    [Fact]
    public void DayOfMonth_PastInStartMonth_FirstDueIsNextMonth()
    {
        // Sözleşme 10 Ocak'ta başlıyor, vade ayın 5'i → ilk vade 5 Şubat olmalı.
        var input = new RecurringObligationInput
        {
            Title = "Aylık",
            RecurrenceType = RecurrenceType.Monthly,
            DayOfMonth = 5,
            StartDate = new DateOnly(2026, 1, 10),
            EndDate = new DateOnly(2026, 4, 30)
        };

        var dueDates = ContractObligationGenerator.ComputeDueDates(
            input, input.StartDate!.Value, input.EndDate!.Value);

        Assert.Equal(3, dueDates.Count);
        // 5 Şubat 2026 = Perşembe; 5 Nisan 2026 = Pazar → 6 Nisan (Pazartesi)
        Assert.Equal(new DateOnly(2026, 2, 5), dueDates[0]);
        Assert.Equal(new DateOnly(2026, 4, 6), dueDates[2]);
    }

    [Fact]
    public void Generate_ReturnsRecurrenceWithObligations()
    {
        var inputs = new List<RecurringObligationInput>
        {
            new()
            {
                Title = "Aylık Kira",
                Amount = 5000,
                RecurrenceType = RecurrenceType.Monthly,
                DayOfMonth = 1,
                StartDate = new DateOnly(2026, 1, 1),
                EndDate = new DateOnly(2026, 6, 30)
            }
        };

        var sets = ContractObligationGenerator.Generate(
            contractId: 99, firmaId: 1,
            contractStart: new DateOnly(2026, 1, 1),
            contractEnd: new DateOnly(2026, 12, 31),
            inputs: inputs,
            createdBy: "test");

        Assert.Single(sets);
        Assert.Equal(6, sets[0].Obligations.Count);
        Assert.Equal(99, sets[0].Obligations[0].ContractId);
        Assert.Equal(1, sets[0].Obligations[0].FirmaId);
        Assert.Equal("Aylık Kira", sets[0].Obligations[0].Title);
        Assert.Equal(ObligationStatus.Pending, sets[0].Obligations[0].Status);
    }

    [Fact]
    public void Generate_TooLongRange_Throws()
    {
        var inputs = new List<RecurringObligationInput>
        {
            new()
            {
                Title = "Çok uzun aylık",
                RecurrenceType = RecurrenceType.Monthly,
                DayOfMonth = 1,
                StartDate = new DateOnly(2020, 1, 1),
                EndDate = new DateOnly(2040, 12, 31)   // 21 yıl × 12 ay = 252 > 120
            }
        };

        Assert.Throws<ArgumentException>(() =>
            ContractObligationGenerator.Generate(
                contractId: 1, firmaId: 1,
                contractStart: new DateOnly(2020, 1, 1),
                contractEnd: new DateOnly(2040, 12, 31),
                inputs: inputs,
                createdBy: "test"));
    }
}
