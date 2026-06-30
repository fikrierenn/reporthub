using Mosaik.Modules.Kvkk.Services;
using Xunit;

namespace Mosaik.Tests
{
    // Plan 40 Faz 6 — VERBİS export formula-injection guard.
    public class KvkkVerbisSafeTests
    {
        [Theory]
        [InlineData("=cmd|'/c calc'!A1", "'=cmd|'/c calc'!A1")]
        [InlineData("+1+1", "'+1+1")]
        [InlineData("-2", "'-2")]
        [InlineData("@SUM(A1)", "'@SUM(A1)")]
        [InlineData("Bordro hesaplama", "Bordro hesaplama")]
        [InlineData("10 yıl", "10 yıl")]
        public void Safe_NeutralizesFormulaPrefix(string input, string expected)
        {
            Assert.Equal(expected, VerbisExporter.Safe(input));
        }

        [Fact]
        public void Safe_NullOrEmpty()
        {
            Assert.Equal("", VerbisExporter.Safe(null));
            Assert.Equal("", VerbisExporter.Safe(""));
        }
    }
}
