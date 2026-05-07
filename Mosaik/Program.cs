using Microsoft.EntityFrameworkCore;
using Mosaik.Models;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// URL launchSettings.json'dan okunuyor

// Add services to the container.
var mvcBuilder = builder.Services.AddControllersWithViews();
if (builder.Environment.IsDevelopment())
{
    mvcBuilder.AddRazorRuntimeCompilation();
}
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<Mosaik.Services.AuditLogService>();
builder.Services.AddScoped<Mosaik.Services.UserRoleSyncService>();
builder.Services.AddScoped<Mosaik.Services.ReportGroupService>();
builder.Services.AddScoped<Mosaik.Services.RoleManagementService>();
builder.Services.AddScoped<Mosaik.Services.DataSourceManagementService>();
builder.Services.AddScoped<Mosaik.Services.ReportManagementService>();
builder.Services.AddScoped<Mosaik.Services.UserManagementService>();
builder.Services.AddScoped<Mosaik.Services.SpExplorerService>();
builder.Services.AddScoped<Mosaik.Services.FilterOptionsService>();
builder.Services.AddScoped<Mosaik.Services.FilterDefinitionService>();
builder.Services.AddScoped<Mosaik.Services.ExcelExportService>();
builder.Services.AddScoped<Mosaik.Services.UserDataFilterInjector>();
builder.Services.AddScoped<Mosaik.Services.StoredProcedureExecutor>();
builder.Services.AddSingleton<Mosaik.Services.IBrandService, Mosaik.Services.BrandSettingsService>();
builder.Services.AddSingleton<Mosaik.Services.IModuleService, Mosaik.Services.ModuleService>();

// Plan 16.5 Faz A+B — Mosaik.Core
builder.Services.AddMemoryCache();
builder.Services.AddScoped<Mosaik.Core.Workflow.ApprovalService>();
builder.Services.AddScoped<Mosaik.Core.Lookup.LookupService>();

// Plan 14 Faz C1 — IUserDataScope implementasyonları + Registry
builder.Services.AddScoped<Mosaik.Core.DataScope.IUserDataScope, Mosaik.Core.DataScope.SpInjectionScope>();
builder.Services.AddScoped<Mosaik.Core.DataScope.IUserDataScope, Mosaik.Core.DataScope.ReportAccessScope>();
builder.Services.AddScoped<Mosaik.Core.DataScope.DataScopeRegistry>();
// Faz C2 (gelecek): UserDataFilterInjector + ReportsController.Index
// Registry'ye refactor (DRY). Şu an mevcut inline mantık çalışmaya devam ediyor.

// Add Entity Framework
builder.Services.AddDbContext<MosaikContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/AccessDenied";
        options.SlidingExpiration = true;

        // G-05: Auth cookie sertlestirme
        options.Cookie.HttpOnly = true;
        // Dev'de HTTPS olmadan calisiyoruz; prod'da Always.
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddAuthorization();

// Health Checks ekle
builder.Services.AddHealthChecks()
    .AddDbContextCheck<MosaikContext>("database")
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Health check endpoint'i ekle
app.MapHealthChecks("/health");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
