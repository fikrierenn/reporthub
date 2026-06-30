using Mosaik.Modules.Kvkk;
using Xunit;

namespace Mosaik.Tests
{
    // Plan 40 Faz 2 — byte→Türkçe etiket eşleme testleri.
    public class KvkkLabelsTests
    {
        [Theory]
        [InlineData((byte)0, "Düşük", "ok")]
        [InlineData((byte)1, "Orta", "warn")]
        [InlineData((byte)2, "Yüksek", "err")]
        [InlineData((byte)9, "—", "")]
        public void Risk(byte v, string label, string cls)
        {
            Assert.Equal(label, KvkkLabels.Risk(v));
            Assert.Equal(cls, KvkkLabels.RiskClass(v));
        }

        [Theory]
        [InlineData((byte)0, "Taslak")]
        [InlineData((byte)1, "Birim Onayı")]
        [InlineData((byte)2, "KVKK Onayı")]
        [InlineData((byte)3, "VERBİS Yayında")]
        [InlineData((byte)7, "—")]
        public void ReviewStatus(byte v, string expected)
        {
            Assert.Equal(expected, KvkkLabels.ReviewStatus(v));
        }

        [Theory]
        [InlineData((byte)0, "Toplar")]
        [InlineData((byte)2, "Aktarır")]
        public void UsageType(byte v, string expected)
        {
            Assert.Equal(expected, KvkkLabels.UsageType(v));
        }

        [Theory]
        [InlineData((byte)1, "Standart Sözleşme")]
        [InlineData((byte)5, "Sözleşme İfası")]
        [InlineData((byte)9, "—")]
        public void Mechanism(byte v, string expected)
        {
            Assert.Equal(expected, KvkkLabels.Mechanism(v));
        }
    }
}
