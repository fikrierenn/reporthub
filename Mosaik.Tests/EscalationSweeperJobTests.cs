using Mosaik.Services;
using Xunit;

namespace Mosaik.Tests
{
    // Plan 54 M4 — EscalationSweeperJob saf metrik + karşılaştırma logic testleri.
    public class EscalationSweeperJobTests
    {
        private static List<List<Dictionary<string, object>>> Rs(params Dictionary<string, object>[] rows)
            => new() { rows.ToList() };

        private static Dictionary<string, object> Row(string col, object val) => new() { [col] = val };

        // --- ComputeMetric ---

        [Fact]
        public void ComputeMetric_First_ReturnsFirstRowColumn()
        {
            var rs = Rs(Row("Ciro", 120), Row("Ciro", 999));
            Assert.Equal(120m, EscalationSweeperJob.ComputeMetric(rs, 0, "Ciro", "first"));
        }

        [Fact]
        public void ComputeMetric_Count_IgnoresColumn()
        {
            var rs = Rs(Row("X", 1), Row("X", 2), Row("X", 3));
            Assert.Equal(3m, EscalationSweeperJob.ComputeMetric(rs, 0, "yok", "count"));
        }

        [Fact]
        public void ComputeMetric_Sum_AddsColumnValues()
        {
            var rs = Rs(Row("Adet", 10), Row("Adet", 5), Row("Adet", 2));
            Assert.Equal(17m, EscalationSweeperJob.ComputeMetric(rs, 0, "Adet", "sum"));
        }

        [Fact]
        public void ComputeMetric_AvgMinMax()
        {
            var rs = Rs(Row("V", 10), Row("V", 20), Row("V", 30));
            Assert.Equal(20m, EscalationSweeperJob.ComputeMetric(rs, 0, "V", "avg"));
            Assert.Equal(10m, EscalationSweeperJob.ComputeMetric(rs, 0, "V", "min"));
            Assert.Equal(30m, EscalationSweeperJob.ComputeMetric(rs, 0, "V", "max"));
        }

        [Fact]
        public void ComputeMetric_OutOfRangeResultSet_ReturnsNull()
        {
            var rs = Rs(Row("V", 1));
            Assert.Null(EscalationSweeperJob.ComputeMetric(rs, 5, "V", "first"));
        }

        [Fact]
        public void ComputeMetric_MissingColumn_ReturnsNull()
        {
            var rs = Rs(Row("V", 1));
            Assert.Null(EscalationSweeperJob.ComputeMetric(rs, 0, "Baska", "first"));
        }

        [Fact]
        public void ComputeMetric_EmptyResultSet_FirstReturnsNull_CountReturnsZero()
        {
            var rs = new List<List<Dictionary<string, object>>> { new() };
            Assert.Null(EscalationSweeperJob.ComputeMetric(rs, 0, "V", "first"));
            Assert.Equal(0m, EscalationSweeperJob.ComputeMetric(rs, 0, "V", "count"));
        }

        [Fact]
        public void ComputeMetric_NonNumericValue_SkippedInAggregate()
        {
            var rs = Rs(Row("V", 10), Row("V", "abc"), Row("V", 20));
            Assert.Equal(30m, EscalationSweeperJob.ComputeMetric(rs, 0, "V", "sum"));
        }

        [Fact]
        public void ComputeMetric_DecimalStringInvariant_Parsed()
        {
            var rs = Rs(Row("V", "12.5"));
            Assert.Equal(12.5m, EscalationSweeperJob.ComputeMetric(rs, 0, "V", "first"));
        }

        // --- Compare ---

        [Theory]
        [InlineData(10, "gt", 5, true)]
        [InlineData(5, "gt", 5, false)]
        [InlineData(5, "gte", 5, true)]
        [InlineData(3, "lt", 5, true)]
        [InlineData(5, "lte", 5, true)]
        [InlineData(5, "eq", 5, true)]
        [InlineData(6, "eq", 5, false)]
        [InlineData(5, "bogus", 5, false)]
        public void Compare_Operators(decimal value, string op, decimal threshold, bool expected)
        {
            Assert.Equal(expected, EscalationSweeperJob.Compare(value, op, threshold));
        }
    }
}
