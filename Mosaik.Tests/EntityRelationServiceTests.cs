using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Mosaik.Core.Intelligence;
using Mosaik.Models;
using Mosaik.Services.Intelligence;

namespace Mosaik.Tests;

// Plan 38 — EntityRelationService unit tests.
public class EntityRelationServiceTests
{
    private static MosaikContext NewContext(string name)
    {
        var options = new DbContextOptionsBuilder<MosaikContext>()
            .UseInMemoryDatabase(databaseName: name + "_" + Guid.NewGuid())
            .Options;
        return new MosaikContext(options);
    }

    private static EntityRelationInput SampleInput(int srcId = 1, int tgtId = 2) => new(
        FirmaId: 1,
        SourceType: EntityType.User,
        SourceId: srcId,
        RelationType: RelationType.Manages,
        TargetType: EntityType.Department,
        TargetId: tgtId);

    [Fact]
    public async Task AddAsync_NewRelation_ReturnsOk()
    {
        await using var ctx = NewContext(nameof(AddAsync_NewRelation_ReturnsOk));
        var svc = new EntityRelationService(ctx, NullLogger<EntityRelationService>.Instance);

        var result = await svc.AddAsync(SampleInput());

        Assert.True(result.IsSuccess);
        Assert.True(result.Data > 0);
        Assert.Single(ctx.EntityRelations);
    }

    [Fact]
    public async Task AddAsync_DuplicateTuple_UpdatesExistingNotInsert()
    {
        await using var ctx = NewContext(nameof(AddAsync_DuplicateTuple_UpdatesExistingNotInsert));
        var svc = new EntityRelationService(ctx, NullLogger<EntityRelationService>.Instance);

        var first = await svc.AddAsync(SampleInput());
        var second = await svc.AddAsync(SampleInput() with { Weight = 0.75m });

        Assert.True(second.IsSuccess);
        Assert.Equal(first.Data, second.Data);
        Assert.Single(ctx.EntityRelations);
        Assert.Equal(0.75m, ctx.EntityRelations.Single().Weight);
    }

    [Fact]
    public async Task AddAsync_InvalidSourceType_ReturnsFailure()
    {
        await using var ctx = NewContext(nameof(AddAsync_InvalidSourceType_ReturnsFailure));
        var svc = new EntityRelationService(ctx, NullLogger<EntityRelationService>.Instance);

        var result = await svc.AddAsync(SampleInput() with { SourceType = "BogusType" });

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_SOURCE_TYPE", result.ErrorCode);
    }

    [Fact]
    public async Task AddAsync_InvalidRelationType_ReturnsFailure()
    {
        await using var ctx = NewContext(nameof(AddAsync_InvalidRelationType_ReturnsFailure));
        var svc = new EntityRelationService(ctx, NullLogger<EntityRelationService>.Instance);

        var result = await svc.AddAsync(SampleInput() with { RelationType = "loves" });

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_RELATION_TYPE", result.ErrorCode);
    }

    [Fact]
    public async Task GetBySourceAsync_FiltersByRelationType()
    {
        await using var ctx = NewContext(nameof(GetBySourceAsync_FiltersByRelationType));
        var svc = new EntityRelationService(ctx, NullLogger<EntityRelationService>.Instance);

        await svc.AddAsync(SampleInput());
        await svc.AddAsync(SampleInput(srcId: 1, tgtId: 3) with { RelationType = RelationType.MemberOf });

        var manages = await svc.GetBySourceAsync(EntityType.User, 1, RelationType.Manages);
        var all = await svc.GetBySourceAsync(EntityType.User, 1);

        Assert.Single(manages);
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task GetByTargetAsync_FindsIncomingRelations()
    {
        await using var ctx = NewContext(nameof(GetByTargetAsync_FindsIncomingRelations));
        var svc = new EntityRelationService(ctx, NullLogger<EntityRelationService>.Instance);

        await svc.AddAsync(SampleInput(srcId: 1, tgtId: 99));
        await svc.AddAsync(SampleInput(srcId: 2, tgtId: 99));

        var incoming = await svc.GetByTargetAsync(EntityType.Department, 99);

        Assert.Equal(2, incoming.Count);
    }

    [Fact]
    public async Task RemoveAsync_DeletesRelation()
    {
        await using var ctx = NewContext(nameof(RemoveAsync_DeletesRelation));
        var svc = new EntityRelationService(ctx, NullLogger<EntityRelationService>.Instance);

        var add = await svc.AddAsync(SampleInput());
        var remove = await svc.RemoveAsync(add.Data);

        Assert.True(remove.IsSuccess);
        Assert.Empty(ctx.EntityRelations);
    }
}
