using Mosaik.Modules.Kvkk.Services;
using Xunit;

namespace Mosaik.Tests
{
    // Plan 40 (M6) Faz 5 — serbest-metin saklama süresi → yaklaşık gün parse testleri.
    public class KvkkDurationParserTests
    {
        [Theory]
        [InlineData("60 gün", 60)]
        [InlineData("30-60 gün", 60)]
        [InlineData("2 yıl", 730)]
        [InlineData("1-3 yıl", 1095)]
        [InlineData("10 yıl", 3650)]
        [InlineData("Saklama süresi + 10 yıl", 3650)]
        [InlineData("6 ay", 180)]
        public void ParseMaxDays_ExtractsUpperBound(string text, int expected)
        {
            Assert.Equal(expected, KvkkDurationParser.ParseMaxDays(text));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("İYS geçerli olduğu sürece")]
        public void ParseMaxDays_UnknownText_ReturnsNull(string? text)
        {
            Assert.Null(KvkkDurationParser.ParseMaxDays(text));
        }
    }
}
