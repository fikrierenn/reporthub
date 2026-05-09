using Hangfire;
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
builder.Services.AddScoped<Mosaik.Core.Logging.IAuditLog>(
    sp => sp.GetRequiredService<Mosaik.Services.AuditLogService>());
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

// Plan 17 Faz F — AI özet altyapısı (Groq/Gemini provider, runtime config Admin'den)
builder.Services.AddHttpClient("ai");
// Plan 25 — vision için ayrı timeout (sözleşme görseli 30-90s sürebilir).
builder.Services.AddHttpClient("ai-vision", c => c.Timeout = TimeSpan.FromSeconds(120));
builder.Services.AddScoped<Mosaik.Core.Ai.IAiSettingsProvider, Mosaik.Services.AiSettingsProvider>();
builder.Services.AddScoped<Mosaik.Core.Ai.IAiSummaryProvider, Mosaik.Services.AiSummaryProvider>();

// Plan 16.5 Faz A+B — Mosaik.Core
builder.Services.AddMemoryCache();
builder.Services.AddScoped<Mosaik.Services.ApprovalService>();
builder.Services.AddScoped<Mosaik.Services.LookupService>();
builder.Services.AddScoped<Mosaik.Core.Lookup.ILookupService>(
    sp => sp.GetRequiredService<Mosaik.Services.LookupService>());

// Plan 14 Faz C1 — IUserDataScope implementasyonları + Registry
builder.Services.AddScoped<Mosaik.Core.DataScope.IUserDataScope, Mosaik.Services.SpInjectionScope>();
builder.Services.AddScoped<Mosaik.Core.DataScope.IUserDataScope, Mosaik.Services.ReportAccessScope>();
builder.Services.AddScoped<Mosaik.Core.DataScope.DataScopeRegistry>();

// Plan 20 Faz A — Organizasyon şeması
builder.Services.AddScoped<Mosaik.Services.IOrgChartService, Mosaik.Services.OrgChartService>();

// Plan 17 Faz H — cross-modül bildirim
builder.Services.AddScoped<Mosaik.Core.Notification.INotificationService, Mosaik.Services.NotificationService>();
// Faz C2 (gelecek): UserDataFilterInjector + ReportsController.Index
// Registry'ye refactor (DRY). Şu an mevcut inline mantık çalışmaya devam ediyor.

// Plan 25 — Sözleşme modülü (ADR-012: IHostedService + Channel<int>)
builder.Services.AddScoped<Mosaik.Services.ICurrentUserService, Mosaik.Services.CurrentUserService>();
builder.Services.AddSingleton<Mosaik.Services.Ai.AiPipelineQueue>();
builder.Services.AddSingleton<Mosaik.Services.Ai.IPdfTextExtractor, Mosaik.Services.Ai.PdfPigTextExtractor>();
builder.Services.AddHostedService<Mosaik.Services.Ai.AiExtractionWorker>();

// Plan 25 AI Wizard — vision provider + extraction service (Faz 1).
// Plan 25.1 hardening (file serving + queue migration) bekliyor.
builder.Services.AddScoped<Mosaik.Core.Ai.IAiVisionProvider, Mosaik.Services.Ai.ZaiVisionProvider>();
builder.Services.AddScoped<Mosaik.Services.Ai.WizardExtractionService>();

// Plan 16.6 — Modular Monolith. ModuleLoader Mosaik.Modules.* assembly'lerini
// tarar, IMosaikModule implementasyonlarını DI + ModelBuilder + endpoint'lere
// register eder. Şu an modül yok — Plan 17+ Tamim ilk modül olacak.
Mosaik.Core.Module.ModuleLoader.RegisterAll(builder.Services);

// Add Entity Framework
builder.Services.AddDbContext<MosaikContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Plan 16.6 — Modüller MosaikContext'e direkt referans veremez (cross-csproj
// dairesel bağımlılık). Bunun yerine DbContext base'i resolve edip Set<T>() ile
// erişirler. Aynı MosaikContext instance'ı (scoped lifetime).
builder.Services.AddScoped<Microsoft.EntityFrameworkCore.DbContext>(
    sp => sp.GetRequiredService<MosaikContext>());

// Plan 17 v2 — Hangfire (background job altyapısı, 17:00 günlük tamim derleme cron).
// SQL Server storage (HANGFIRE schema otomatik yaratılır), aynı DefaultConnection.
// Dashboard /hangfire route'unda — auth filter Program.cs sonunda eklenir.
builder.Services.AddHangfire(cfg => cfg
    .SetDataCompatibilityLevel(Hangfire.CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection"),
        new Hangfire.SqlServer.SqlServerStorageOptions
        {
            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
            QueuePollInterval = TimeSpan.Zero,
            UseRecommendedIsolationLevel = true,
            DisableGlobalLocks = true
        }));
builder.Services.AddHangfireServer();

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

// Plan 17 v2 — Hangfire dashboard. Sadece admin rolü. UseAuthorization SONRASI.
app.UseHangfireDashboard("/hangfire", new Hangfire.DashboardOptions
{
    Authorization = new[] { new Mosaik.Services.HangfireAdminOnlyAuth() }
});

// Plan 16.6 — Modül endpoint mapping (Areas pattern). Default route'tan
// ÖNCE map edilmeli ki modül route'ları default'a düşmesin.
Mosaik.Core.Module.ModuleLoader.MapAll(app);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

// Plan 17 Faz D — Günlük tamim derleme cron (her gün 17:00 Europe/Istanbul).
// Bugünün pending bloklarını topla → tek Circular zarfı altında yayınla.
RecurringJob.AddOrUpdate<Mosaik.Modules.Circular.Services.CompileCircularJob>(
    recurringJobId: "circular-compile-daily",
    methodCall: job => job.ExecuteAsync(null),
    cronExpression: "0 17 * * *",
    options: new RecurringJobOptions
    {
        TimeZone = TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "Turkey Standard Time" : "Europe/Istanbul")
    });

app.Run();
