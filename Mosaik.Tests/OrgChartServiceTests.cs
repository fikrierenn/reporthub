using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Mosaik.Models;
using Mosaik.Services;

namespace Mosaik.Tests;

// Plan 20 Faz A — OrgChartService CRUD + cycle detect.
public class OrgChartServiceTests
{
    private static MosaikContext NewContext(string name)
    {
        var options = new DbContextOptionsBuilder<MosaikContext>()
            .UseInMemoryDatabase(databaseName: name + "_" + Guid.NewGuid())
            .Options;
        return new MosaikContext(options);
    }

    private static OrgChartService NewService(MosaikContext ctx) =>
        new(ctx, new MemoryCache(new MemoryCacheOptions()), NullLogger<OrgChartService>.Instance);

    [Fact]
    public async Task CreateAsync_RootPosition_Succeeds()
    {
        await using var ctx = NewContext(nameof(CreateAsync_RootPosition_Succeeds));
        var svc = NewService(ctx);

        var result = await svc.CreateAsync("CEO", "Genel Müdür", null, 0, null, "admin");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Null(result.Data!.ParentPositionId);
        Assert.Equal("CEO", result.Data.Code);
    }

    [Fact]
    public async Task CreateAsync_ChildPosition_BindsToParent()
    {
        await using var ctx = NewContext(nameof(CreateAsync_ChildPosition_BindsToParent));
        var svc = NewService(ctx);

        var ceo = await svc.CreateAsync("CEO", "Genel Müdür", null, 0, null, "admin");
        var cfo = await svc.CreateAsync("CFO", "Mali İşler Direktörü", ceo.Data!.Id, 1, null, "admin");

        Assert.True(cfo.IsSuccess);
        Assert.Equal(ceo.Data.Id, cfo.Data!.ParentPositionId);
    }

    [Fact]
    public async Task CreateAsync_DuplicateCode_Fails()
    {
        await using var ctx = NewContext(nameof(CreateAsync_DuplicateCode_Fails));
        var svc = NewService(ctx);

        await svc.CreateAsync("CEO", "Genel Müdür", null, 0, null, "admin");
        var dup = await svc.CreateAsync("CEO", "Başka Title", null, 0, null, "admin");

        Assert.False(dup.IsSuccess);
        Assert.Contains("zaten mevcut", dup.Message);
    }

    [Fact]
    public async Task CreateAsync_EmptyCodeOrTitle_Fails()
    {
        await using var ctx = NewContext(nameof(CreateAsync_EmptyCodeOrTitle_Fails));
        var svc = NewService(ctx);

        var emptyCode = await svc.CreateAsync("", "Title", null, 0, null, "admin");
        var emptyTitle = await svc.CreateAsync("CODE", "  ", null, 0, null, "admin");

        Assert.False(emptyCode.IsSuccess);
        Assert.False(emptyTitle.IsSuccess);
    }

    [Fact]
    public async Task CreateAsync_UnknownParent_Fails()
    {
        await using var ctx = NewContext(nameof(CreateAsync_UnknownParent_Fails));
        var svc = NewService(ctx);

        var result = await svc.CreateAsync("CFO", "Mali", parentPositionId: 9999, 0, null, "admin");

        Assert.False(result.IsSuccess);
        Assert.Contains("bulunamadı", result.Message);
    }

    [Fact]
    public async Task UpdateAsync_SelfReference_Fails()
    {
        await using var ctx = NewContext(nameof(UpdateAsync_SelfReference_Fails));
        var svc = NewService(ctx);
        var ceo = (await svc.CreateAsync("CEO", "Genel Müdür", null, 0, null, "admin")).Data!;

        var result = await svc.UpdateAsync(ceo.Id, "Genel Müdür", parentPositionId: ceo.Id, 0, true, null, "admin");

        Assert.False(result.IsSuccess);
        Assert.Contains("kendisinin altına", result.Message);
    }

    [Fact]
    public async Task UpdateAsync_DescendantAsParent_Fails_Cycle()
    {
        await using var ctx = NewContext(nameof(UpdateAsync_DescendantAsParent_Fails_Cycle));
        var svc = NewService(ctx);
        var ceo = (await svc.CreateAsync("CEO", "Genel Müdür", null, 0, null, "admin")).Data!;
        var cfo = (await svc.CreateAsync("CFO", "Mali", ceo.Id, 1, null, "admin")).Data!;
        var acc = (await svc.CreateAsync("ACC", "Muhasebe", cfo.Id, 2, null, "admin")).Data!;

        // CEO'yu Muhasebe'nin altına bağla -> döngü.
        var result = await svc.UpdateAsync(ceo.Id, "Genel Müdür", parentPositionId: acc.Id, 0, true, null, "admin");

        Assert.False(result.IsSuccess);
        Assert.Contains("döngü", result.Message);
    }

    [Fact]
    public async Task UpdateAsync_ValidReparent_Succeeds()
    {
        await using var ctx = NewContext(nameof(UpdateAsync_ValidReparent_Succeeds));
        var svc = NewService(ctx);
        var ceo = (await svc.CreateAsync("CEO", "Genel Müdür", null, 0, null, "admin")).Data!;
        var cfo = (await svc.CreateAsync("CFO", "Mali", null, 1, null, "admin")).Data!;

        var result = await svc.UpdateAsync(cfo.Id, "Mali İşler Direktörü", parentPositionId: ceo.Id, 1, true, null, "admin");

        Assert.True(result.IsSuccess);
        var reloaded = await svc.GetByIdAsync(cfo.Id);
        Assert.Equal(ceo.Id, reloaded!.ParentPositionId);
        Assert.Equal("Mali İşler Direktörü", reloaded.Title);
    }

    [Fact]
    public async Task DeleteAsync_PositionWithChildren_Fails()
    {
        await using var ctx = NewContext(nameof(DeleteAsync_PositionWithChildren_Fails));
        var svc = NewService(ctx);
        var ceo = (await svc.CreateAsync("CEO", "Genel Müdür", null, 0, null, "admin")).Data!;
        await svc.CreateAsync("CFO", "Mali", ceo.Id, 1, null, "admin");

        var result = await svc.DeleteAsync(ceo.Id, "admin");

        Assert.False(result.IsSuccess);
        Assert.Contains("önce onları taşıyın", result.Message);
    }

    [Fact]
    public async Task DeleteAsync_LeafPosition_Succeeds()
    {
        await using var ctx = NewContext(nameof(DeleteAsync_LeafPosition_Succeeds));
        var svc = NewService(ctx);
        var ceo = (await svc.CreateAsync("CEO", "Genel Müdür", null, 0, null, "admin")).Data!;

        var result = await svc.DeleteAsync(ceo.Id, "admin");

        Assert.True(result.IsSuccess);
        Assert.Null(await svc.GetByIdAsync(ceo.Id));
    }

    [Fact]
    public async Task GetAllAsync_FiltersInactiveByDefault()
    {
        await using var ctx = NewContext(nameof(GetAllAsync_FiltersInactiveByDefault));
        var svc = NewService(ctx);
        var ceo = (await svc.CreateAsync("CEO", "Genel Müdür", null, 0, null, "admin")).Data!;
        await svc.UpdateAsync(ceo.Id, "Genel Müdür", null, 0, isActive: false, null, "admin");

        var active = await svc.GetAllAsync(includeInactive: false);
        var withInactive = await svc.GetAllAsync(includeInactive: true);

        Assert.Empty(active);
        Assert.Single(withInactive);
    }

    [Fact]
    public async Task GetByCodeAsync_TrimsAndFinds()
    {
        await using var ctx = NewContext(nameof(GetByCodeAsync_TrimsAndFinds));
        var svc = NewService(ctx);
        await svc.CreateAsync("MGR", "Müdür", null, 0, null, "admin");

        var byTrim = await svc.GetByCodeAsync("  MGR  ");

        Assert.NotNull(byTrim);
        Assert.Equal("MGR", byTrim!.Code);
    }

    // ============================================================
    // Faz B — Reorder + Import testleri
    // ============================================================

    [Fact]
    public async Task ReorderAsync_ChangesParentAndDisplayOrder()
    {
        await using var ctx = NewContext(nameof(ReorderAsync_ChangesParentAndDisplayOrder));
        var svc = NewService(ctx);
        var ceo = (await svc.CreateAsync("CEO", "Genel Müdür", null, 0, null, "admin")).Data!;
        var cfo = (await svc.CreateAsync("CFO", "Mali", null, 1, null, "admin")).Data!;
        var acc = (await svc.CreateAsync("ACC", "Muhasebe", ceo.Id, 1, null, "admin")).Data!;

        // CFO'yu CEO altına taşı, CFO ilk, ACC ikinci olacak şekilde sırala
        var result = await svc.ReorderAsync(cfo.Id, ceo.Id, new[] { cfo.Id, acc.Id }, "admin");

        Assert.True(result.IsSuccess);
        var reloadedCfo = await svc.GetByIdAsync(cfo.Id);
        var reloadedAcc = await svc.GetByIdAsync(acc.Id);
        Assert.Equal(ceo.Id, reloadedCfo!.ParentPositionId);
        Assert.Equal(1, reloadedCfo.DisplayOrder);
        Assert.Equal(2, reloadedAcc!.DisplayOrder);
    }

    [Fact]
    public async Task ReorderAsync_CycleAttempt_Fails()
    {
        await using var ctx = NewContext(nameof(ReorderAsync_CycleAttempt_Fails));
        var svc = NewService(ctx);
        var ceo = (await svc.CreateAsync("CEO", "Genel Müdür", null, 0, null, "admin")).Data!;
        var cfo = (await svc.CreateAsync("CFO", "Mali", ceo.Id, 1, null, "admin")).Data!;

        // CEO'yu CFO altına taşımak döngü oluşturur
        var result = await svc.ReorderAsync(ceo.Id, cfo.Id, new[] { ceo.Id }, "admin");

        Assert.False(result.IsSuccess);
        Assert.Contains("döngü", result.Message);
    }

    [Fact]
    public async Task ReorderAsync_ToRoot_SetsParentNull()
    {
        await using var ctx = NewContext(nameof(ReorderAsync_ToRoot_SetsParentNull));
        var svc = NewService(ctx);
        var ceo = (await svc.CreateAsync("CEO", "Genel Müdür", null, 0, null, "admin")).Data!;
        var cfo = (await svc.CreateAsync("CFO", "Mali", ceo.Id, 1, null, "admin")).Data!;

        var result = await svc.ReorderAsync(cfo.Id, null, new[] { ceo.Id, cfo.Id }, "admin");

        Assert.True(result.IsSuccess);
        var reloaded = await svc.GetByIdAsync(cfo.Id);
        Assert.Null(reloaded!.ParentPositionId);
    }

    [Fact]
    public async Task ImportFromZirveCodeAsync_NewCode_Succeeds()
    {
        await using var ctx = NewContext(nameof(ImportFromZirveCodeAsync_NewCode_Succeeds));
        var svc = NewService(ctx);

        var result = await svc.ImportFromZirveCodeAsync("SATIŞ DANIŞMANI", "admin");

        Assert.True(result.IsSuccess);
        Assert.Equal("SATIŞ DANIŞMANI", result.Data!.Code);
        Assert.Null(result.Data.ParentPositionId);
        // Title Case Türkçe — "Satış Danışmanı" beklenir
        Assert.Equal("Satış Danışmanı", result.Data.Title);
    }

    [Fact]
    public async Task ImportFromZirveCodeAsync_DuplicateCode_Fails()
    {
        await using var ctx = NewContext(nameof(ImportFromZirveCodeAsync_DuplicateCode_Fails));
        var svc = NewService(ctx);
        await svc.CreateAsync("SATIŞ DANIŞMANI", "Satış Danışmanı", null, 0, null, "admin");

        var result = await svc.ImportFromZirveCodeAsync("SATIŞ DANIŞMANI", "admin");

        Assert.False(result.IsSuccess);
        Assert.Contains("zaten Mosaik", result.Message);
    }
}
