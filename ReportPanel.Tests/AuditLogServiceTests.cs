using ReportPanel.Services;

namespace ReportPanel.Tests;

public class AuditLogServiceTests
{
    [Fact]
    public void ToJson_ShouldReturnEmptyStringForNull()
    {
        var result = AuditLogService.ToJson(null);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ToJson_ShouldSerializeObject()
    {
        var result = AuditLogService.ToJson(new { Name = "Report", Count = 2 });

        Assert.Contains("\"Name\":\"Report\"", result);
        Assert.Contains("\"Count\":2", result);
    }
}
