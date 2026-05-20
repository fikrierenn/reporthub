using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Mosaik.Core.Intelligence;
using Mosaik.Models;
using Mosaik.Services.Intelligence;

namespace Mosaik.Tests;

// Plan 38 — DecisionLogService unit tests.
public class DecisionLogServiceTests
{
    private static MosaikContext NewContext(string name)
    {
        var options = new DbContextOptionsBuilder<MosaikContext>()
            .UseInMemoryDatabase(databaseName: name + "_" + Guid.NewGuid())
            .Options;
        return new MosaikContext(options);
    }

    private static DecisionLogEntry SampleEntry() => new(
        FirmaId: 1,
        Title: "Sözleşme onaylandı",
        MadeBy: 42,
        Rationale: "Fiyat şartları uygun.",
        RelatedEntityType: EntityType.Contract,
        RelatedEntityId: 7);

    [Fact]
    public async Task LogAsync_ValidEntry_ReturnsOk()
    {
        await using var ctx = NewContext(nameof(LogAsync_ValidEntry_ReturnsOk));
        var svc = new DecisionLogService(ctx, NullLogger<DecisionLogService>.Instance);

        var result = await svc.LogAsync(SampleEntry());

        Assert.True(result.IsSuccess);
        Assert.True(result.Data > 0);
        Assert.Single(ctx.DecisionLogs);
    }

    [Fact]
    public async Task LogAsync_EmptyTitle_ReturnsFailure()
    {
        await using var ctx = NewContext(nameof(LogAsync_EmptyTitle_ReturnsFailure));
        var svc = new DecisionLogService(ctx, NullLogger<DecisionLogService>.Instance);

        var result = await svc.LogAsync(SampleEntry() with { Title = "  " });

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_TITLE", result.ErrorCode);
    }

    [Fact]
    public async Task LogAsync_InvalidRelatedEntityType_ReturnsFailure()
    {
        await using var ctx = NewContext(nameof(LogAsync_InvalidRelatedEntityType_ReturnsFailure));
        var svc = new DecisionLogService(ctx, NullLogger<DecisionLogService>.Instance);

        var result = await svc.LogAsync(SampleEntry() with { RelatedEntityType = "Banana" });

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_ENTITY_TYPE", result.ErrorCode);
    }

    [Fact]
    public async Task GetByEntityAsync_OrdersByMadeAtDesc()
    {
        await using var ctx = NewContext(nameof(GetByEntityAsync_OrdersByMadeAtDesc));
        var svc = new DecisionLogService(ctx, NullLogger<DecisionLogService>.Instance);

        await svc.LogAsync(SampleEntry() with { Title = "Eski", MadeAt = DateTime.UtcNow.AddDays(-2) });
        await svc.LogAsync(SampleEntry() with { Title = "Yeni", MadeAt = DateTime.UtcNow });

        var list = await svc.GetByEntityAsync(EntityType.Contract, 7);

        Assert.Equal(2, list.Count);
        Assert.Equal("Yeni", list[0].Title);
        Assert.Equal("Eski", list[1].Title);
    }
}
