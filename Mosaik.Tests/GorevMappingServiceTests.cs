using Microsoft.EntityFrameworkCore;
using Mosaik.Modules.GorevTanimlari;
using Mosaik.Modules.GorevTanimlari.Entities;
using Mosaik.Modules.GorevTanimlari.Services;

namespace Mosaik.Tests;

// T-16 — MappingService.SaveAsync 4 dal (add / update / remove / no-op).
// GetPeopleAsync harici IK SqlConnection kullandığı için birim test kapsamı dışı;
// SaveAsync yalnızca base DbContext.Set<GorevPersonelMap>() üzerinde çalışır → InMemory yeterli.
public class GorevMappingServiceTests
{
    // Modülün EF map'ini uygulayan minimal test-context (MosaikContext InMemory deseni).
    private sealed class GorevTestContext : DbContext
    {
        public GorevTestContext(DbContextOptions<GorevTestContext> options) : base(options) { }
        protected override void OnModelCreating(ModelBuilder mb) =>
            new GorevTanimlariModule().ConfigureModelBuilder(mb);
    }

    private static GorevTestContext NewContext(string name)
    {
        var options = new DbContextOptionsBuilder<GorevTestContext>()
            .UseInMemoryDatabase(databaseName: name + "_" + Guid.NewGuid())
            .Options;
        return new GorevTestContext(options);
    }

    [Fact]
    public async Task SaveAsync_NoExisting_DocId_AddsPersonMap()
    {
        await using var ctx = NewContext(nameof(SaveAsync_NoExisting_DocId_AddsPersonMap));
        var svc = new MappingService(ctx);

        await svc.SaveAsync("4634-BKM", 7, "admin");

        var row = await ctx.Set<GorevPersonelMap>().SingleAsync();
        Assert.Equal("4634-BKM", row.Personelno);
        Assert.Equal(7, row.GorevDocumentId);
        Assert.Equal("manual", row.Source);
        Assert.Equal("admin", row.MappedBy);
    }

    [Fact]
    public async Task SaveAsync_Existing_NewDocId_UpdatesInPlace()
    {
        await using var ctx = NewContext(nameof(SaveAsync_Existing_NewDocId_UpdatesInPlace));
        ctx.Add(new GorevPersonelMap { Personelno = "4634-BKM", GorevDocumentId = 3, Source = "manual" });
        await ctx.SaveChangesAsync();

        var svc = new MappingService(ctx);
        await svc.SaveAsync("4634-BKM", 9, "editor");

        var row = await ctx.Set<GorevPersonelMap>().SingleAsync(); // tek satır → update, ikinci ekleme yok
        Assert.Equal(9, row.GorevDocumentId);
        Assert.Equal("editor", row.MappedBy);
    }

    [Fact]
    public async Task SaveAsync_Existing_NullDocId_RemovesPersonMap()
    {
        await using var ctx = NewContext(nameof(SaveAsync_Existing_NullDocId_RemovesPersonMap));
        ctx.Add(new GorevPersonelMap { Personelno = "4634-BKM", GorevDocumentId = 3, Source = "manual" });
        await ctx.SaveChangesAsync();

        var svc = new MappingService(ctx);
        await svc.SaveAsync("4634-BKM", null, "admin"); // kişi override sil → rol varsayılanına düşer

        Assert.Empty(await ctx.Set<GorevPersonelMap>().ToListAsync());
    }

    [Fact]
    public async Task SaveAsync_NoExisting_NullDocId_NoOp()
    {
        await using var ctx = NewContext(nameof(SaveAsync_NoExisting_NullDocId_NoOp));
        var svc = new MappingService(ctx);

        await svc.SaveAsync("9999-BKM", null, "admin"); // silinecek kayıt yok → sessiz no-op, exception yok

        Assert.Empty(await ctx.Set<GorevPersonelMap>().ToListAsync());
    }
}
