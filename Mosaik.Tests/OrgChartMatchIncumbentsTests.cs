using Mosaik.Core.Domain;
using Mosaik.Services;

namespace Mosaik.Tests;

// Plan 55 Faz 2 — OrgChartService.MatchIncumbents: Zirve incumbent → pozisyon eşleme.
// Match anahtarı ZirveMatchKey (yoksa Title), sonuç pozisyon Code'u ile anahtarlanır.
// Code org.json id suffix'i taşısa bile (Code ≠ Unvan) eşleşme sürer — regresyon koruması.
public class OrgChartMatchIncumbentsTests
{
    private static OrgPosition Pos(string code, string title, string? matchKey) =>
        new() { Code = code, Title = title, ZirveMatchKey = matchKey };

    private static OrgIncumbent Inc(string unvan, string personelno = "1") =>
        new() { Unvan = unvan, PersonelNo = personelno, AdSoyad = "Test" };

    [Fact]
    public void Match_CodeHasSuffix_MatchesViaZirveMatchKey_KeyedByCode()
    {
        // Code suffix'li (org.json id), ZirveMatchKey temiz ünvan. Zirve Unvan = temiz ünvan.
        var positions = new[] { Pos("GENEL MÜDÜR YARDIMCISI-N3", "Genel Müdür Yardımcısı", "Genel Müdür Yardımcısı") };
        var byCode = new Dictionary<string, List<OrgIncumbent>>();
        var unmatched = new List<OrgIncumbent>();

        OrgChartService.MatchIncumbents(positions, new[] { Inc("Genel Müdür Yardımcısı") }, byCode, unmatched);

        Assert.Empty(unmatched);
        Assert.True(byCode.ContainsKey("GENEL MÜDÜR YARDIMCISI-N3")); // Code ile anahtarlı, Unvan değil
        Assert.Single(byCode["GENEL MÜDÜR YARDIMCISI-N3"]);
    }

    [Fact]
    public void Match_DuplicateTitle_TwoPositionsSameKey_IncumbentAddedToBoth()
    {
        // Aynı ünvan iki pozisyon (iki kişi) → incumbent her ikisine düşer.
        var positions = new[]
        {
            Pos("İK UZMANI-N40", "İK Uzmanı", "İK Uzmanı"),
            Pos("İK UZMANI-N41", "İK Uzmanı", "İK Uzmanı")
        };
        var byCode = new Dictionary<string, List<OrgIncumbent>>();
        var unmatched = new List<OrgIncumbent>();

        OrgChartService.MatchIncumbents(positions, new[] { Inc("İK Uzmanı") }, byCode, unmatched);

        Assert.Empty(unmatched);
        Assert.Single(byCode["İK UZMANI-N40"]);
        Assert.Single(byCode["İK UZMANI-N41"]);
    }

    [Fact]
    public void Match_NullZirveMatchKey_FallsBackToTitle()
    {
        var positions = new[] { Pos("MUHASEBE MÜDÜRÜ", "Muhasebe Müdürü", null) }; // eski satır — matchkey NULL
        var byCode = new Dictionary<string, List<OrgIncumbent>>();
        var unmatched = new List<OrgIncumbent>();

        OrgChartService.MatchIncumbents(positions, new[] { Inc("Muhasebe Müdürü") }, byCode, unmatched);

        Assert.Empty(unmatched);
        Assert.Single(byCode["MUHASEBE MÜDÜRÜ"]);
    }

    [Fact]
    public void Match_NoPositionForUnvan_GoesToUnmatched()
    {
        var positions = new[] { Pos("SATIŞ DANIŞMANI", "Satış Danışmanı", "Satış Danışmanı") };
        var byCode = new Dictionary<string, List<OrgIncumbent>>();
        var unmatched = new List<OrgIncumbent>();

        OrgChartService.MatchIncumbents(positions, new[] { Inc("Bilinmeyen Ünvan") }, byCode, unmatched);

        Assert.Empty(byCode);
        Assert.Single(unmatched);
    }

    [Fact]
    public void Match_EmptyUnvan_GoesToUnmatched()
    {
        var positions = new[] { Pos("MÜDÜR", "Müdür", "Müdür") };
        var byCode = new Dictionary<string, List<OrgIncumbent>>();
        var unmatched = new List<OrgIncumbent>();

        OrgChartService.MatchIncumbents(positions, new[] { Inc("") }, byCode, unmatched);

        Assert.Empty(byCode);
        Assert.Single(unmatched);
    }
}
