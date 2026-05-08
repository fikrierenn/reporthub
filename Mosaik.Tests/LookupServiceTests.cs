using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Mosaik.Core.Lookup;
using Mosaik.Models;
using Mosaik.Services;

namespace Mosaik.Tests;

// Plan 16.5 Faz B — LookupService (DictionaryType/Value cached read + CRUD).
public class LookupServiceTests
{
    private static MosaikContext NewContext(string name)
    {
        var options = new DbContextOptionsBuilder<MosaikContext>()
            .UseInMemoryDatabase(databaseName: name + "_" + Guid.NewGuid())
            .Options;
        return new MosaikContext(options);
    }

    private static LookupService NewService(MosaikContext ctx, IMemoryCache? cache = null) =>
        new(ctx, cache ?? new MemoryCache(new MemoryCacheOptions()), NullLogger<LookupService>.Instance);

    private static async Task SeedAsync(MosaikContext ctx)
    {
        var dept = new DictionaryType { Code = "department", Name = "Departman", IsActive = true };
        var status = new DictionaryType { Code = "status", Name = "Durum", IsActive = true };
        var inactiveType = new DictionaryType { Code = "legacy", Name = "Eski", IsActive = false };
        ctx.DictionaryTypes.AddRange(dept, status, inactiveType);
        await ctx.SaveChangesAsync();

        ctx.DictionaryValues.AddRange(
            new DictionaryValue { TypeId = dept.Id, Code = "ik", Label = "İnsan Kaynakları", DisplayOrder = 2, IsActive = true },
            new DictionaryValue { TypeId = dept.Id, Code = "mali", Label = "Mali İşler", DisplayOrder = 1, IsActive = true },
            new DictionaryValue { TypeId = dept.Id, Code = "eski", Label = "Eski Departman", DisplayOrder = 99, IsActive = false },
            new DictionaryValue { TypeId = status.Id, Code = "open", Label = "Açık", DisplayOrder = 1, IsActive = true },
            new DictionaryValue { TypeId = inactiveType.Id, Code = "x", Label = "Görünmez", DisplayOrder = 1, IsActive = true }
        );
        await ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task GetValuesAsync_ReturnsActiveValuesOrderedByDisplayOrder()
    {
        await using var ctx = NewContext(nameof(GetValuesAsync_ReturnsActiveValuesOrderedByDisplayOrder));
        await SeedAsync(ctx);
        var svc = NewService(ctx);

        var values = await svc.GetValuesAsync("department");

        Assert.Equal(2, values.Count);
        Assert.Equal("mali", values[0].Code);
        Assert.Equal("ik", values[1].Code);
    }

    [Fact]
    public async Task GetValuesAsync_FiltersInactiveValues()
    {
        await using var ctx = NewContext(nameof(GetValuesAsync_FiltersInactiveValues));
        await SeedAsync(ctx);
        var svc = NewService(ctx);

        var values = await svc.GetValuesAsync("department");

        Assert.DoesNotContain(values, v => v.Code == "eski");
    }

    [Fact]
    public async Task GetValuesAsync_FiltersInactiveTypes()
    {
        await using var ctx = NewContext(nameof(GetValuesAsync_FiltersInactiveTypes));
        await SeedAsync(ctx);
        var svc = NewService(ctx);

        var values = await svc.GetValuesAsync("legacy");

        Assert.Empty(values);
    }

    [Fact]
    public async Task GetValuesAsync_UnknownTypeReturnsEmpty()
    {
        await using var ctx = NewContext(nameof(GetValuesAsync_UnknownTypeReturnsEmpty));
        await SeedAsync(ctx);
        var svc = NewService(ctx);

        var values = await svc.GetValuesAsync("doesNotExist");

        Assert.Empty(values);
    }

    [Fact]
    public async Task GetValuesAsync_SecondCallHitsCache()
    {
        await using var ctx = NewContext(nameof(GetValuesAsync_SecondCallHitsCache));
        await SeedAsync(ctx);
        var cache = new MemoryCache(new MemoryCacheOptions());
        var svc = NewService(ctx, cache);

        await svc.GetValuesAsync("department");
        // Cache entry exists.
        Assert.True(cache.TryGetValue("lookup_values_department", out _));

        // DB temizlense bile cache'den döner.
        ctx.DictionaryValues.RemoveRange(ctx.DictionaryValues);
        await ctx.SaveChangesAsync();

        var cached = await svc.GetValuesAsync("department");
        Assert.Equal(2, cached.Count);
    }

    [Fact]
    public async Task GetByCodeAsync_ReturnsMatchingValue()
    {
        await using var ctx = NewContext(nameof(GetByCodeAsync_ReturnsMatchingValue));
        await SeedAsync(ctx);
        var svc = NewService(ctx);

        var result = await svc.GetByCodeAsync("department", "ik");

        Assert.NotNull(result);
        Assert.Equal("İnsan Kaynakları", result!.Label);
    }

    [Fact]
    public async Task GetByCodeAsync_ReturnsNullForUnknownCode()
    {
        await using var ctx = NewContext(nameof(GetByCodeAsync_ReturnsNullForUnknownCode));
        await SeedAsync(ctx);
        var svc = NewService(ctx);

        var result = await svc.GetByCodeAsync("department", "unknown");

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateValueAsync_AddsValueAndInvalidatesCache()
    {
        await using var ctx = NewContext(nameof(CreateValueAsync_AddsValueAndInvalidatesCache));
        await SeedAsync(ctx);
        var cache = new MemoryCache(new MemoryCacheOptions());
        var svc = NewService(ctx, cache);
        var typeId = ctx.DictionaryTypes.First(t => t.Code == "department").Id;

        // Cache prime
        await svc.GetValuesAsync("department");
        Assert.True(cache.TryGetValue("lookup_values_department", out _));

        var result = await svc.CreateValueAsync(typeId, "satis", "Satış", 3, "admin");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.False(cache.TryGetValue("lookup_values_department", out _));

        var values = await svc.GetValuesAsync("department");
        Assert.Equal(3, values.Count);
        Assert.Contains(values, v => v.Code == "satis");
    }

    [Fact]
    public async Task CreateValueAsync_RejectsDuplicateCode()
    {
        await using var ctx = NewContext(nameof(CreateValueAsync_RejectsDuplicateCode));
        await SeedAsync(ctx);
        var svc = NewService(ctx);
        var typeId = ctx.DictionaryTypes.First(t => t.Code == "department").Id;

        var result = await svc.CreateValueAsync(typeId, "ik", "İnsan Kaynakları 2", 5, "admin");

        Assert.False(result.IsSuccess);
        Assert.Contains("zaten mevcut", result.Message);
    }

    [Fact]
    public async Task CreateValueAsync_RejectsEmptyCodeOrLabel()
    {
        await using var ctx = NewContext(nameof(CreateValueAsync_RejectsEmptyCodeOrLabel));
        await SeedAsync(ctx);
        var svc = NewService(ctx);
        var typeId = ctx.DictionaryTypes.First(t => t.Code == "department").Id;

        var emptyCode = await svc.CreateValueAsync(typeId, "", "Etiket", 1, "admin");
        var emptyLabel = await svc.CreateValueAsync(typeId, "kod", "  ", 1, "admin");

        Assert.False(emptyCode.IsSuccess);
        Assert.False(emptyLabel.IsSuccess);
    }

    [Fact]
    public async Task CreateValueAsync_RejectsUnknownType()
    {
        await using var ctx = NewContext(nameof(CreateValueAsync_RejectsUnknownType));
        await SeedAsync(ctx);
        var svc = NewService(ctx);

        var result = await svc.CreateValueAsync(typeId: 9999, "kod", "Etiket", 1, "admin");

        Assert.False(result.IsSuccess);
        Assert.Contains("bulunamadı", result.Message);
    }

    [Fact]
    public async Task SetActiveAsync_TogglesAndInvalidatesCache()
    {
        await using var ctx = NewContext(nameof(SetActiveAsync_TogglesAndInvalidatesCache));
        await SeedAsync(ctx);
        var cache = new MemoryCache(new MemoryCacheOptions());
        var svc = NewService(ctx, cache);
        var ikId = ctx.DictionaryValues.First(v => v.Code == "ik").Id;

        await svc.GetValuesAsync("department");
        Assert.True(cache.TryGetValue("lookup_values_department", out _));

        var result = await svc.SetActiveAsync(ikId, active: false, updatedBy: "admin");

        Assert.True(result.IsSuccess);
        Assert.False(cache.TryGetValue("lookup_values_department", out _));

        var values = await svc.GetValuesAsync("department");
        Assert.Single(values);
        Assert.Equal("mali", values[0].Code);
    }

    [Fact]
    public async Task SetActiveAsync_ReturnsFailureForMissingValue()
    {
        await using var ctx = NewContext(nameof(SetActiveAsync_ReturnsFailureForMissingValue));
        await SeedAsync(ctx);
        var svc = NewService(ctx);

        var result = await svc.SetActiveAsync(valueId: 9999, active: true, updatedBy: "admin");

        Assert.False(result.IsSuccess);
        Assert.Contains("bulunamadı", result.Message);
    }

    [Fact]
    public async Task GetTypesAsync_ReturnsActiveTypesWithValues()
    {
        await using var ctx = NewContext(nameof(GetTypesAsync_ReturnsActiveTypesWithValues));
        await SeedAsync(ctx);
        var svc = NewService(ctx);

        var types = await svc.GetTypesAsync();

        Assert.Equal(2, types.Count);
        Assert.DoesNotContain(types, t => t.Code == "legacy");
        var dept = types.First(t => t.Code == "department");
        Assert.Equal(3, dept.Values.Count); // includes inactive (admin view)
    }

    [Fact]
    public async Task InvalidateCache_ClearsCachedTypeCode()
    {
        await using var ctx = NewContext(nameof(InvalidateCache_ClearsCachedTypeCode));
        await SeedAsync(ctx);
        var cache = new MemoryCache(new MemoryCacheOptions());
        var svc = NewService(ctx, cache);

        await svc.GetValuesAsync("department");
        Assert.True(cache.TryGetValue("lookup_values_department", out _));

        svc.InvalidateCache("department");

        Assert.False(cache.TryGetValue("lookup_values_department", out _));
    }
}
