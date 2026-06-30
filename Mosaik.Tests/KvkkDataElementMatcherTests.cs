using Mosaik.Core.Intelligence;
using Mosaik.Modules.Kvkk.Services;
using Xunit;

namespace Mosaik.Tests
{
    // Plan 54 M6 / Plan 40 Faz 0 — EntityRelations çift-yazma whitelist sözleşmesi.
    // KvkkProcessService bu sabitlerle yazar; whitelist eksikse EntityRelation sessizce düşerdi.
    public class KvkkEntityRelationContractTests
    {
        [Fact]
        public void EntityType_Whitelist_AcceptsKvkkTypes()
        {
            Assert.True(EntityType.IsValid(KvkkProcessService.ProcessEntityType));      // "KvkkProcess"
            Assert.True(EntityType.IsValid(KvkkProcessService.DataElementEntityType));  // "DataElement"
        }

        [Fact]
        public void RelationType_Whitelist_AcceptsProcesses()
        {
            Assert.True(RelationType.IsValid(KvkkProcessService.ProcessesRelation));     // "processes"
        }
    }

    // Plan 54 M6 / Plan 40 Faz 0 — reverse-search saf eşleştirme testleri (DB'siz).
    public class KvkkDataElementMatcherTests
    {
        [Fact]
        public void Matches_DisplayName_Substring()
        {
            Assert.True(DataElementMatcher.Matches("soyad", "person.fullname", "Ad-Soyad", "ad,isim"));
        }

        [Fact]
        public void Matches_Alias()
        {
            Assert.True(DataElementMatcher.Matches("isim", "person.fullname", "Ad-Soyad", "ad,soyad,isim,name"));
        }

        [Fact]
        public void Matches_ElementCode()
        {
            Assert.True(DataElementMatcher.Matches("fullname", "person.fullname", "Ad-Soyad", null));
        }

        [Fact]
        public void Matches_CaseInsensitive()
        {
            Assert.True(DataElementMatcher.Matches("IBAN", "finance.iban", "IBAN", "iban,hesap no"));
        }

        [Fact]
        public void Matches_TurkishDottedI_Normalized()
        {
            // Sorgu "ıban" (noktasız ı) → "iban" alias eşleşmeli.
            Assert.True(DataElementMatcher.Matches("ıban", "finance.iban", "IBAN", "iban"));
        }

        [Fact]
        public void Matches_EmptyQuery_False()
        {
            Assert.False(DataElementMatcher.Matches("", "person.fullname", "Ad-Soyad", "ad,soyad"));
            Assert.False(DataElementMatcher.Matches("   ", "person.fullname", "Ad-Soyad", "ad,soyad"));
        }

        [Fact]
        public void Matches_NoMatch_False()
        {
            Assert.False(DataElementMatcher.Matches("plaka", "person.fullname", "Ad-Soyad", "ad,soyad,isim"));
        }

        [Fact]
        public void Matches_MultiWord_TokenAnd()
        {
            // "ad soyad" (boşluklu) → her iki token da alias'larda → eşleşir.
            Assert.True(DataElementMatcher.Matches("ad soyad", "person.fullname", "Ad-Soyad", "ad,soyad,isim,name"));
            // "tc kimlik" → displayName "TC Kimlik No" her iki token'ı içerir.
            Assert.True(DataElementMatcher.Matches("tc kimlik", "person.tckn", "TC Kimlik No", "tc,tckn,kimlik no"));
        }

        [Fact]
        public void Matches_MultiWord_AllTokensRequired()
        {
            // "ad plaka" → "ad" eşleşir ama "plaka" yok → token-AND başarısız.
            Assert.False(DataElementMatcher.Matches("ad plaka", "person.fullname", "Ad-Soyad", "ad,soyad,isim"));
        }

        [Fact]
        public void Normalize_LowercasesAndFoldsI()
        {
            Assert.Equal("istanbul", DataElementMatcher.Normalize("İstanbul"));
            Assert.Equal("iban", DataElementMatcher.Normalize("ıBAN"));
        }
    }
}
