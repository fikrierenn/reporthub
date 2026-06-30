using Mosaik.Modules.Kvkk.Services;
using Xunit;

namespace Mosaik.Tests
{
    // Plan 40 Faz 1 — xlsx import saf parser testleri (DB'siz, gerçek BKM v7 değer formatları).
    public class KvkkImportParsersTests
    {
        [Theory]
        [InlineData("Düşük (anonim/agregat veri)", 0)]
        [InlineData("Orta", 1)]
        [InlineData("Yüksek (finans+kimlik kombinasyonu)", 2)]
        [InlineData("Yüksek (özel nitelikli veri)", 2)]
        [InlineData(null, 1)]
        [InlineData("", 1)]
        public void ParseRiskLevel(string? input, byte expected)
        {
            Assert.Equal(expected, KvkkImportParsers.ParseRiskLevel(input));
        }

        [Theory]
        [InlineData("m.5/2/ç (TTK m.390 vd.)", "5/2-ç")]
        [InlineData("m.5/1 (Açık rıza)", "5/1")]
        [InlineData("m.6/3/a (Açık rıza)", "6/3-a")]
        [InlineData("m.5/2/a + m.6/3/g", "5/2-a")]   // ilk eşleşme
        [InlineData("m.5/2/f", "5/2-f")]
        [InlineData("Genel: m.5/2/f", "5/2-f")]
        [InlineData("Zorunlu çerez:", null)]
        [InlineData("", null)]
        public void ParseLegalBasisArticle(string input, string? expected)
        {
            Assert.Equal(expected, KvkkImportParsers.ParseLegalBasisArticle(input));
        }

        [Fact]
        public void SplitList_CommaSemicolon()
        {
            Assert.Equal(new[] { "Kimlik", "İletişim" }, KvkkImportParsers.SplitList("Kimlik, İletişim"));
            Assert.Equal(new[] { "A", "B", "C" }, KvkkImportParsers.SplitList("A; B, C"));
            Assert.Empty(KvkkImportParsers.SplitList(""));
        }

        [Theory]
        [InlineData("Yok", false)]
        [InlineData("", false)]
        [InlineData("Hayır", false)]
        [InlineData("ABD (Microsoft 365)", true)]
        [InlineData("AB/ABD (Bulut sağlayıcı)", true)]
        public void HasCrossBorder(string input, bool expected)
        {
            Assert.Equal(expected, KvkkImportParsers.HasCrossBorder(input));
        }

        [Theory]
        [InlineData("Mekanizma: Standart Sözleşme", (byte)1)]
        [InlineData("BCR bağlayıcı kurallar", (byte)2)]
        [InlineData("Taahhütname", (byte)3)]
        [InlineData("Tedarikçi ülkesi (sözleşme ifası)", (byte)5)]
        public void GuessMechanism(string input, byte expected)
        {
            Assert.Equal(expected, KvkkImportParsers.GuessMechanism(input));
        }

        [Fact]
        public void FirstCountry_StopsAtParenOrPipe()
        {
            Assert.Equal("AB/ABD", KvkkImportParsers.FirstCountry("AB/ABD (Microsoft 365) | Mekanizma: SCC"));
            Assert.Equal("ABD", KvkkImportParsers.FirstCountry("ABD (Google Analytics)"));
        }

        [Fact]
        public void TextContainsElement_DetectsByAlias()
        {
            Assert.True(DataElementMatcher.TextContainsElement("Ad, soyad, TC kimlik no, doğum tarihi", "Ad-Soyad", "ad,soyad,isim"));
            Assert.True(DataElementMatcher.TextContainsElement("IBAN, banka hesabı", "IBAN", "iban,hesap no"));
            Assert.False(DataElementMatcher.TextContainsElement("Yalnızca sıra numarası", "Parmak İzi", "parmak izi,fingerprint"));
        }
    }
}
