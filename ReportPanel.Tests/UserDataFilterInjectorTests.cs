using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ReportPanel.Models;
using ReportPanel.Services;

namespace ReportPanel.Tests;

/// <summary>
/// M-13 R6.2 regression coverage: UserDataFilterInjector.
/// G-03 multi-tenant filter enjeksiyon — InMemory EF Core + AuditLogService.
/// </summary>
public class UserDataFilterInjectorTests
{
    private static (UserDataFilterInjector sut, ReportPanelContext ctx) CreateSut()
    {
        var options = new DbContextOptionsBuilder<ReportPanelContext>()
            .UseInMemoryDatabase($"udf-{Guid.NewGuid()}")
            .Options;
        var ctx = new ReportPanelContext(options);
        // AuditLogService dependency: HttpContextAccessor + IConfiguration. NullLogger pattern yok burada;
        // ama InjectAsync sadece LogAsync çağrısı yapıyor — minimal stub yeterli.
        var auditLog = new AuditLogService(ctx, new Microsoft.AspNetCore.Http.HttpContextAccessor());
        return (new UserDataFilterInjector(ctx, auditLog), ctx);
    }

    [Fact]
    public async Task InjectAsync_null_user_does_nothing()
    {
        var (sut, _) = CreateSut();
        var parameters = new List<SqlParameter>();

        await sut.InjectAsync(parameters, userId: null, reportId: 1, dataSourceKey: "PDKS");

        Assert.Empty(parameters);
    }

    [Fact]
    public async Task InjectAsync_no_filters_no_parameters_added()
    {
        var (sut, _) = CreateSut();
        var parameters = new List<SqlParameter>();

        await sut.InjectAsync(parameters, userId: 99, reportId: 1, dataSourceKey: "PDKS");

        Assert.Empty(parameters);
    }

    [Fact]
    public async Task InjectAsync_single_valid_filter_creates_param()
    {
        var (sut, ctx) = CreateSut();
        ctx.UserDataFilters.Add(new UserDataFilter
        {
            FilterId = 1,
            UserId = 5,
            FilterKey = "bolum",
            FilterValue = "FSM",
            ReportId = null,
            DataSourceKey = null
        });
        await ctx.SaveChangesAsync();

        var parameters = new List<SqlParameter>();
        await sut.InjectAsync(parameters, userId: 5, reportId: 1, dataSourceKey: "PDKS");

        Assert.Single(parameters);
        Assert.Equal("@bolum_Filtre", parameters[0].ParameterName);
        Assert.Equal("FSM", parameters[0].Value);
    }

    [Fact]
    public async Task InjectAsync_multiple_values_same_key_csv_joined()
    {
        var (sut, ctx) = CreateSut();
        ctx.UserDataFilters.AddRange(
            new UserDataFilter { FilterId = 1, UserId = 5, FilterKey = "bolum", FilterValue = "FSM" },
            new UserDataFilter { FilterId = 2, UserId = 5, FilterKey = "bolum", FilterValue = "HEYKEL" }
        );
        await ctx.SaveChangesAsync();

        var parameters = new List<SqlParameter>();
        await sut.InjectAsync(parameters, userId: 5, reportId: 1, dataSourceKey: "PDKS");

        Assert.Single(parameters);
        Assert.Equal("FSM,HEYKEL", parameters[0].Value);
    }

    [Fact]
    public async Task InjectAsync_other_user_filter_ignored()
    {
        var (sut, ctx) = CreateSut();
        ctx.UserDataFilters.Add(new UserDataFilter
        {
            FilterId = 1, UserId = 999, FilterKey = "bolum", FilterValue = "FSM"
        });
        await ctx.SaveChangesAsync();

        var parameters = new List<SqlParameter>();
        await sut.InjectAsync(parameters, userId: 5, reportId: 1, dataSourceKey: "PDKS");

        Assert.Empty(parameters);
    }

    [Fact]
    public async Task InjectAsync_existing_form_param_not_overwritten()
    {
        var (sut, ctx) = CreateSut();
        ctx.UserDataFilters.Add(new UserDataFilter
        {
            FilterId = 1, UserId = 5, FilterKey = "bolum", FilterValue = "FSM"
        });
        await ctx.SaveChangesAsync();

        var parameters = new List<SqlParameter>
        {
            new("@bolum_Filtre", "USER_OVERRIDE")  // form'dan zaten geldi
        };
        await sut.InjectAsync(parameters, userId: 5, reportId: 1, dataSourceKey: "PDKS");

        Assert.Single(parameters);
        Assert.Equal("USER_OVERRIDE", parameters[0].Value);  // korundu
    }

    [Fact]
    public async Task InjectAsync_invalid_key_rejected_audit_logged()
    {
        var (sut, ctx) = CreateSut();
        ctx.UserDataFilters.Add(new UserDataFilter
        {
            FilterId = 1,
            UserId = 5,
            FilterKey = "1invalid; DROP TABLE",  // whitelist regex fail
            FilterValue = "X"
        });
        await ctx.SaveChangesAsync();

        var parameters = new List<SqlParameter>();
        await sut.InjectAsync(parameters, userId: 5, reportId: 1, dataSourceKey: "PDKS");

        Assert.Empty(parameters);
        var auditEntries = ctx.AuditLogs.Where(a => a.EventType == "user_filter_rejected").ToList();
        Assert.Single(auditEntries);
        Assert.Contains("1invalid", auditEntries[0].Description!);
    }

    // Plan 07 Faz 4: Aktif FilterDefinition var ama user'in o key icin kaydi yok → deny.
    [Fact]
    public async Task InjectAsync_active_definition_no_user_record_throws_deny()
    {
        var (sut, ctx) = CreateSut();
        ctx.FilterDefinitions.Add(new FilterDefinition
        {
            FilterKey = "bolum",
            Label = "Şube",
            Scope = FilterDefinition.ScopeSpInjection,
            DataSourceKey = "PDKS",
            OptionsQuery = "SELECT 1 AS Value, 'x' AS Label",
            IsActive = true
        });
        await ctx.SaveChangesAsync();

        var parameters = new List<SqlParameter>();

        var ex = await Assert.ThrowsAsync<UserDataFilterDeniedException>(() =>
            sut.InjectAsync(parameters, userId: 5, reportId: 1, dataSourceKey: "PDKS"));

        Assert.Equal("bolum", ex.FilterKey);
        Assert.Equal(5, ex.UserId);
        Assert.Empty(parameters);
    }

    // Plan 07 Faz 4: Inactive FilterDefinition deny check'e dahil degil.
    [Fact]
    public async Task InjectAsync_inactive_definition_does_not_deny()
    {
        var (sut, ctx) = CreateSut();
        ctx.FilterDefinitions.Add(new FilterDefinition
        {
            FilterKey = "bolum",
            Label = "Şube",
            Scope = FilterDefinition.ScopeSpInjection,
            IsActive = false
        });
        await ctx.SaveChangesAsync();

        var parameters = new List<SqlParameter>();
        await sut.InjectAsync(parameters, userId: 5, reportId: 1, dataSourceKey: "PDKS");

        Assert.Empty(parameters);
    }

    // Plan 07 Faz 4: '*' kayit deny check'i memnun eder ama parametre eklenmiyor.
    [Fact]
    public async Task InjectAsync_star_record_satisfies_deny_check_but_no_param_added()
    {
        var (sut, ctx) = CreateSut();
        ctx.FilterDefinitions.Add(new FilterDefinition
        {
            FilterKey = "bolum",
            Label = "Şube",
            Scope = FilterDefinition.ScopeSpInjection,
            IsActive = true
        });
        ctx.UserDataFilters.Add(new UserDataFilter
        {
            FilterId = 1,
            UserId = 5,
            FilterKey = "bolum",
            FilterValue = FilterDefinition.ValueAll
        });
        await ctx.SaveChangesAsync();

        var parameters = new List<SqlParameter>();
        await sut.InjectAsync(parameters, userId: 5, reportId: 1, dataSourceKey: "PDKS");

        Assert.Empty(parameters);
    }

    // Plan 07 Faz 4: Concrete + '*' kayit karisik — sadece concrete enjekte.
    [Fact]
    public async Task InjectAsync_mixed_star_and_concrete_only_concrete_injected()
    {
        var (sut, ctx) = CreateSut();
        ctx.FilterDefinitions.AddRange(
            new FilterDefinition { FilterKey = "bolum", Label = "Şube", Scope = FilterDefinition.ScopeSpInjection, IsActive = true },
            new FilterDefinition { FilterKey = "bolum", Label = "Bölüm", Scope = FilterDefinition.ScopeSpInjection, IsActive = true }
        );
        ctx.UserDataFilters.AddRange(
            new UserDataFilter { FilterId = 1, UserId = 5, FilterKey = "bolum", FilterValue = "FSM" },
            new UserDataFilter { FilterId = 2, UserId = 5, FilterKey = "bolum", FilterValue = FilterDefinition.ValueAll }
        );
        await ctx.SaveChangesAsync();

        var parameters = new List<SqlParameter>();
        await sut.InjectAsync(parameters, userId: 5, reportId: 1, dataSourceKey: "PDKS");

        Assert.Single(parameters);
        Assert.Equal("@bolum_Filtre", parameters[0].ParameterName);
        Assert.Equal("FSM", parameters[0].Value);
    }

    // Plan 07 Faz 4: Bir aktif def icin kayit varsa, ikinci aktif def icin kayitsizsa yine deny.
    [Fact]
    public async Task InjectAsync_partial_records_still_deny_for_missing_key()
    {
        var (sut, ctx) = CreateSut();
        ctx.FilterDefinitions.AddRange(
            new FilterDefinition { FilterKey = "bolum", Label = "Şube", Scope = FilterDefinition.ScopeSpInjection, IsActive = true },
            new FilterDefinition { FilterKey = "kategori", Label = "Kategori", Scope = FilterDefinition.ScopeReportAccess, IsActive = true }
        );
        ctx.UserDataFilters.Add(new UserDataFilter
        {
            FilterId = 1, UserId = 5, FilterKey = "bolum", FilterValue = "FSM"
        });
        await ctx.SaveChangesAsync();

        var parameters = new List<SqlParameter>();

        var ex = await Assert.ThrowsAsync<UserDataFilterDeniedException>(() =>
            sut.InjectAsync(parameters, userId: 5, reportId: 1, dataSourceKey: "PDKS"));

        Assert.Equal("kategori", ex.FilterKey);
    }

    // Plan 07 Faz 5b: canonical sube → SubeMapping translate (SubeId → ExternalCode).
    [Fact]
    public async Task InjectAsync_sube_canonical_translates_to_external_code()
    {
        var (sut, ctx) = CreateSut();
        ctx.Subeler.AddRange(
            new Sube { SubeId = 1, SubeAd = "FSM", IsActive = true },
            new Sube { SubeId = 2, SubeAd = "HEYKEL", IsActive = true }
        );
        ctx.SubeMappings.AddRange(
            new SubeMapping { MappingId = 1, SubeId = 1, DataSourceKey = "PDKS", ExternalCode = "2" },
            new SubeMapping { MappingId = 2, SubeId = 2, DataSourceKey = "PDKS", ExternalCode = "4" }
        );
        ctx.UserDataFilters.AddRange(
            new UserDataFilter { FilterId = 1, UserId = 5, FilterKey = "sube", FilterValue = "1" },
            new UserDataFilter { FilterId = 2, UserId = 5, FilterKey = "sube", FilterValue = "2" }
        );
        await ctx.SaveChangesAsync();

        var parameters = new List<SqlParameter>();
        await sut.InjectAsync(parameters, userId: 5, reportId: 1, dataSourceKey: "PDKS");

        Assert.Single(parameters);
        Assert.Equal("@sube_Filtre", parameters[0].ParameterName);
        var values = parameters[0].Value!.ToString()!.Split(',').OrderBy(v => v).ToArray();
        Assert.Equal(new[] { "2", "4" }, values);
    }

    // Plan 07 Faz 5b: SubeMapping eksik DataSource'ta sube parametre olarak gonderilmez (sessiz drop).
    [Fact]
    public async Task InjectAsync_sube_no_mapping_silent_drop()
    {
        var (sut, ctx) = CreateSut();
        ctx.Subeler.Add(new Sube { SubeId = 1, SubeAd = "FSM", IsActive = true });
        ctx.SubeMappings.Add(new SubeMapping
        {
            MappingId = 1, SubeId = 1, DataSourceKey = "PDKS", ExternalCode = "2"
        });
        // DER icin mapping yok
        ctx.UserDataFilters.Add(new UserDataFilter
        {
            FilterId = 1, UserId = 5, FilterKey = "sube", FilterValue = "1"
        });
        await ctx.SaveChangesAsync();

        var parameters = new List<SqlParameter>();
        await sut.InjectAsync(parameters, userId: 5, reportId: 1, dataSourceKey: "DER");

        Assert.Empty(parameters);
    }

    // Plan 07 Faz 5b: bazi SubeId'lerde mapping eksik → sadece mapping olanlar enjekte edilir.
    [Fact]
    public async Task InjectAsync_sube_partial_mapping_only_mapped_injected()
    {
        var (sut, ctx) = CreateSut();
        ctx.Subeler.AddRange(
            new Sube { SubeId = 1, SubeAd = "FSM", IsActive = true },
            new Sube { SubeId = 2, SubeAd = "FSM KAFE", IsActive = true }
        );
        // Sadece SubeId=1 icin DER mapping; SubeId=2 (FSM KAFE) DER'de yok
        ctx.SubeMappings.Add(new SubeMapping
        {
            MappingId = 1, SubeId = 1, DataSourceKey = "DER", ExternalCode = "M01"
        });
        ctx.UserDataFilters.AddRange(
            new UserDataFilter { FilterId = 1, UserId = 5, FilterKey = "sube", FilterValue = "1" },
            new UserDataFilter { FilterId = 2, UserId = 5, FilterKey = "sube", FilterValue = "2" }
        );
        await ctx.SaveChangesAsync();

        var parameters = new List<SqlParameter>();
        await sut.InjectAsync(parameters, userId: 5, reportId: 1, dataSourceKey: "DER");

        Assert.Single(parameters);
        Assert.Equal("M01", parameters[0].Value);
    }

    [Fact]
    public async Task InjectAsync_report_specific_filter_applies_only_to_matching_report()
    {
        var (sut, ctx) = CreateSut();
        ctx.UserDataFilters.Add(new UserDataFilter
        {
            FilterId = 1, UserId = 5, FilterKey = "bolum", FilterValue = "FSM",
            ReportId = 13
        });
        await ctx.SaveChangesAsync();

        var pForReport13 = new List<SqlParameter>();
        await sut.InjectAsync(pForReport13, userId: 5, reportId: 13, dataSourceKey: "PDKS");
        Assert.Single(pForReport13);

        var pForReport14 = new List<SqlParameter>();
        await sut.InjectAsync(pForReport14, userId: 5, reportId: 14, dataSourceKey: "PDKS");
        Assert.Empty(pForReport14);
    }
}
