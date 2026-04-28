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
            FilterKey = "sube",
            FilterValue = "FSM",
            ReportId = null,
            DataSourceKey = null
        });
        await ctx.SaveChangesAsync();

        var parameters = new List<SqlParameter>();
        await sut.InjectAsync(parameters, userId: 5, reportId: 1, dataSourceKey: "PDKS");

        Assert.Single(parameters);
        Assert.Equal("@sube_Filtre", parameters[0].ParameterName);
        Assert.Equal("FSM", parameters[0].Value);
    }

    [Fact]
    public async Task InjectAsync_multiple_values_same_key_csv_joined()
    {
        var (sut, ctx) = CreateSut();
        ctx.UserDataFilters.AddRange(
            new UserDataFilter { FilterId = 1, UserId = 5, FilterKey = "sube", FilterValue = "FSM" },
            new UserDataFilter { FilterId = 2, UserId = 5, FilterKey = "sube", FilterValue = "HEYKEL" }
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
            FilterId = 1, UserId = 999, FilterKey = "sube", FilterValue = "FSM"
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
            FilterId = 1, UserId = 5, FilterKey = "sube", FilterValue = "FSM"
        });
        await ctx.SaveChangesAsync();

        var parameters = new List<SqlParameter>
        {
            new("@sube_Filtre", "USER_OVERRIDE")  // form'dan zaten geldi
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

    [Fact]
    public async Task InjectAsync_report_specific_filter_applies_only_to_matching_report()
    {
        var (sut, ctx) = CreateSut();
        ctx.UserDataFilters.Add(new UserDataFilter
        {
            FilterId = 1, UserId = 5, FilterKey = "sube", FilterValue = "FSM",
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
