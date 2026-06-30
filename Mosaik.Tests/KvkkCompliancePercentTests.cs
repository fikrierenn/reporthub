using Mosaik.Modules.Kvkk.Services;
using Xunit;

namespace Mosaik.Tests
{
    // Plan 40 Faz 4 — reverse search uyumluluk skoru (saf helper).
    public class KvkkCompliancePercentTests
    {
        [Theory]
        [InlineData(0, 0, 0)]      // süreç yok
        [InlineData(10, 10, 100)]  // hepsi saklamalı
        [InlineData(10, 0, 0)]     // hiçbiri
        [InlineData(4, 1, 25)]
        [InlineData(3, 2, 67)]     // yuvarlama
        [InlineData(8, 6, 75)]
        public void CompliancePercent(int total, int withRetention, int expected)
        {
            Assert.Equal(expected, DataElementService.CompliancePercent(total, withRetention));
        }
    }
}
