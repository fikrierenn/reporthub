using Mosaik.Modules.ProcessRuntime.Services;
using Xunit;

namespace Mosaik.Tests
{
    // Plan 42 REV 3 Faz 0 — insan-dostu vaka kodu üretici (saf).
    public class ProcessInstanceCodeTests
    {
        [Fact]
        public void Generate_FormatsDateAndPaddedId()
        {
            var code = ProcessInstanceCode.Generate(new DateTime(2026, 7, 5, 10, 0, 0, DateTimeKind.Utc), 42);
            Assert.Equal("PI-20260705-0042", code);
        }

        [Fact]
        public void Generate_LargeId_NoTruncation()
        {
            var code = ProcessInstanceCode.Generate(new DateTime(2026, 1, 2), 123456);
            Assert.Equal("PI-20260102-123456", code);
        }
    }
}
