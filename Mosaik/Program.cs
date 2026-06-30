using Hangfire;
using Microsoft.EntityFrameworkCore;
using Mosaik.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Plan 27 Faz A — Serilog file sink. Console + günlük rolling file.
// Log dosyaları: D:/Dev/reporthub/Mosaik/logs/mosaik-YYYY-MM-DD.log (7 gün retention).
// AI çağrıları, hatalar, EF query'leri buraya yazılır — UI takılma debug için kritik.
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Mosaik.Services.AiSummaryProvider", LogEventLevel.Information)
    .MinimumLevel.Override("Mosaik.Services.Ai", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: Path.Combine(builder.Environment.ContentRootPath, "logs", "mosaik-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        outputTemplate: "{Timestamp:HH:mm:ss.fff} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

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
// Plan 54 M4 — Dashboard→Alert sweeper (Hangfire recurring job, aşağıda kaydedilir)
builder.Services.AddScoped<Mosaik.Services.EscalationSweeperJob>();
builder.Services.AddSingleton<Mosaik.Services.IBrandService, Mosaik.Services.BrandSettingsService>();
builder.Services.AddSingleton<Mosaik.Services.IModuleService, Mosaik.Services.ModuleService>();
// Plan 39 Faz B — Stored XSS koruma için render-time HTML sanitize (Block.Content + gelecek Comment/Mention).
builder.Services.AddSingleton<Mosaik.Core.Html.IContentSanitizer, Mosaik.Services.HtmlSanitizerContentSanitizer>();

// Plan 17 Faz F — AI özet altyapısı (Groq/Gemini provider, runtime config Admin'den)
builder.Services.AddHttpClient("ai");
// Plan 25 — vision için ayrı timeout (sözleşme görseli 30-90s sürebilir).
builder.Services.AddHttpClient("ai-vision", c => c.Timeout = TimeSpan.FromSeconds(120));
// Plan 25.1 Faz 2 — API key at-rest encryption.
// Prod'da .PersistKeysToFileSystem(new DirectoryInfo(@"D:\secrets\keys")) veya .PersistKeysToDbContext ekle;
// dev'de memory key ring yeterli (restart sonrası key değişir → mevcut ApiKey'ler NULL'a sıfırla).
builder.Services.AddDataProtection();
builder.Services.AddScoped<Mosaik.Core.Ai.IAiSettingsProvider, Mosaik.Services.AiSettingsProvider>();
builder.Services.AddScoped<Mosaik.Core.Ai.IAiSummaryProvider, Mosaik.Services.AiSummaryProvider>();
builder.Services.AddScoped<Mosaik.Core.Ai.ILlmService, Mosaik.Services.Ai.LlmServiceAdapter>();

// Plan 16.5 Faz A+B — Mosaik.Core
builder.Services.AddMemoryCache();

// Türkiye yerel saat dilimi — startup'ta fail-fast (UTC silent fallback yapma).
// IBusinessClock üzerinden tüm vade/iş günü kıyaslamaları çalışır.
var turkeyTz = Mosaik.Core.Domain.SystemBusinessClock.ResolveTurkeyTimeZone();
builder.Services.AddSingleton<Mosaik.Core.Domain.IBusinessClock>(
    new Mosaik.Core.Domain.SystemBusinessClock(turkeyTz));
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

// Plan 34 Faz E S-20 — modüllerin User entity'sine erişmeden aktif kullanıcı ID listesi alması için
builder.Services.AddScoped<Mosaik.Core.Users.IActiveUserDirectory, Mosaik.Services.ActiveUserDirectoryService>();

// Plan 54 M5 — cross-modül yorum + @mention servisi
builder.Services.AddScoped<Mosaik.Core.Comments.ICommentService, Mosaik.Services.CommentService>();
// Plan 54 M5 — günlük okunmamış bildirim digest job'ı (Hangfire recurring, aşağıda kaydedilir)
builder.Services.AddScoped<Mosaik.Services.NotificationDigestJob>();

// Plan 34.1 Faz 1 A-06 — SOP RAG embedder (e5-base ONNX). Singleton: ONNX session reuse.
builder.Services.AddSingleton<Mosaik.Core.AI.Embed.IMosaikEmbedder>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<Mosaik.Services.Ai.E5Embedder>>();
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    return new Mosaik.Services.Ai.E5Embedder(logger, env.ContentRootPath);
});

// Plan 34.1 Faz 2 A-13 — SOP RAG LLM runner (LLamaSharp + Qwen 2.5 3B). Singleton.
builder.Services.AddSingleton<Mosaik.Core.AI.Local.ILlmRunner>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<Mosaik.Services.Ai.LlamaSharpRunner>>();
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    return new Mosaik.Services.Ai.LlamaSharpRunner(logger, env.ContentRootPath);
});

// Plan 34.1 Faz 2 A-16 — Uygulama startup'ında AI modellerini arka planda warm-up.
builder.Services.AddHostedService<Mosaik.Services.Ai.ModelWarmupHostedService>();

// Plan 34.2 — AI Skill Catalog (App_Data/ai-skills/*.md domain expertise).
builder.Services.AddSingleton<Mosaik.Core.AI.Skills.ISkillCatalog>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<Mosaik.Services.Ai.MarkdownSkillCatalog>>();
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    return new Mosaik.Services.Ai.MarkdownSkillCatalog(logger, env.ContentRootPath);
});

// Plan 38 — Living Org Map + Decision Memory (VISION §7)
builder.Services.AddScoped<Mosaik.Core.Intelligence.IEntityRelationService, Mosaik.Services.Intelligence.EntityRelationService>();
builder.Services.AddScoped<Mosaik.Core.Intelligence.IDecisionLogService, Mosaik.Services.Intelligence.DecisionLogService>();

// Plan 36 — Workflow Designer + onay akışları engine + bildirim + processor
builder.Services.AddScoped<Mosaik.Services.Workflow.WorkflowNotifier>();
builder.Services.AddScoped<Mosaik.Services.Workflow.WorkflowStepProcessor>();
builder.Services.AddScoped<Mosaik.Services.Workflow.WorkflowInboxService>();
builder.Services.AddScoped<Mosaik.Core.Workflow.IEntityWorkflowProvider>(sp =>
    sp.GetRequiredService<Mosaik.Services.Workflow.WorkflowInboxService>());
builder.Services.AddScoped<Mosaik.Core.Workflow.IWorkflowService, Mosaik.Services.Workflow.WorkflowEngine>();

// M2 Unified Inbox — explicit provider kaydı (reflection YOK). Circular kendi modülünde.
builder.Services.AddScoped<Mosaik.Core.Module.Capabilities.IInboxProvider, Mosaik.Services.Inbox.WorkflowInboxProvider>();
builder.Services.AddScoped<Mosaik.Core.Module.Capabilities.IInboxProvider, Mosaik.Services.Inbox.ObligationInboxProvider>();
builder.Services.AddScoped<Mosaik.Services.Inbox.UnifiedInboxService>();

// M3 Cross-module Search — explicit provider kaydı (reflection YOK). Modül provider'ları (Circular) kendi modülünde.
builder.Services.AddScoped<Mosaik.Core.Module.Capabilities.ISearchProvider, Mosaik.Services.Search.ReportSearchProvider>();
builder.Services.AddScoped<Mosaik.Core.Module.Capabilities.ISearchProvider, Mosaik.Services.Search.ContractSearchProvider>();
builder.Services.AddScoped<Mosaik.Core.Module.Capabilities.ISearchProvider, Mosaik.Services.Search.ObligationSearchProvider>();
builder.Services.AddScoped<Mosaik.Services.Search.UnifiedSearchService>();

// Plan 31 — Email (SMTP)
builder.Services.Configure<Mosaik.Core.Email.SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
builder.Services.AddScoped<Mosaik.Core.Email.IEmailService, Mosaik.Services.Email.SmtpEmailService>();
builder.Services.AddScoped<Mosaik.Core.Messaging.IMessenger, Mosaik.Services.Messaging.MessengerService>();
// Plan 44 — RAG Chunk-Level Permission Guard
builder.Services.AddScoped<Mosaik.Core.AI.Rag.IRagAccessPolicy, Mosaik.Core.AI.Rag.DefaultRagAccessPolicy>();
builder.Services.AddTransient<Mosaik.Modules.SOP.Services.ChunkPermissionSyncJob>();
// Faz C2 (gelecek): UserDataFilterInjector + ReportsController.Index
// Registry'ye refactor (DRY). Şu an mevcut inline mantık çalışmaya devam ediyor.

// Plan 25 — Sözleşme modülü (ADR-012: IHostedService + Channel<int>)
builder.Services.AddScoped<Mosaik.Services.ICurrentUserService, Mosaik.Services.CurrentUserService>();
builder.Services.AddSingleton<Mosaik.Services.Ai.AiPipelineQueue>();
builder.Services.AddSingleton<Mosaik.Services.Ai.IPdfTextExtractor, Mosaik.Services.Ai.PdfPigTextExtractor>();
builder.Services.AddSingleton<Mosaik.Services.Ai.TesseractOcrExtractor>();
// Plan 27 Faz A-01
builder.Services.AddSingleton<Mosaik.Services.Ai.PageImportanceScorer>();
// Plan 27 Faz C-06 — doküman/klasör seviyesi izin servisi.
builder.Services.AddScoped<Mosaik.Services.IDocumentPermissionService, Mosaik.Services.DocumentPermissionService>();
// Plan 27 Faz B-01+B-03 — auto-classify + executive summary servisi.
builder.Services.AddScoped<Mosaik.Services.Ai.DocumentInsightService>();
// Plan 27 Faz B-05 — single-doc chat (RAG'siz, token <30K direkt z.ai context).
builder.Services.AddScoped<Mosaik.Services.Ai.DocumentChatService>();
builder.Services.AddHostedService<Mosaik.Services.Ai.AiExtractionWorker>();
builder.Services.AddHostedService<Mosaik.Services.WizardTempCleanupService>(); // N-3

// Plan 25 AI Wizard — vision provider + extraction service (Faz 1).
// D-03 (2026-05-22): ZaiVisionProvider → MultiProviderVisionProvider (zai+gemini+openai/grok fallback).
builder.Services.AddScoped<Mosaik.Core.Ai.IAiVisionProvider, Mosaik.Services.Ai.MultiProviderVisionProvider>();
builder.Services.AddScoped<Mosaik.Services.Ai.WizardExtractionService>();

// Plan 16.6 — Modular Monolith. ModuleLoader Mosaik.Modules.* assembly'lerini
// tarar, IMosaikModule implementasyonlarını DI + ModelBuilder + endpoint'lere
// register eder. Aktif modüller (2026-05-25): Circular, SOP, Forms.
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
builder.Services.AddScoped<Mosaik.Services.DailyReminderJob>();
builder.Services.AddScoped<Mosaik.Services.IDashboardRenderer, Mosaik.Services.DashboardRenderer>();

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

// C-01 (Plan 33 Faz 2) — Günlük yükümlülük hatırlatma cron (her gün 09:00 Europe/Istanbul).
// Pending ContractObligations: vade yaklaşan veya geçmiş → bildirim + SMTP email.
RecurringJob.AddOrUpdate<Mosaik.Services.DailyReminderJob>(
    recurringJobId: "daily-obligation-reminder",
    methodCall: job => job.ExecuteAsync(CancellationToken.None),
    cronExpression: "0 9 * * *",
    options: new RecurringJobOptions
    {
        TimeZone = TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "Turkey Standard Time" : "Europe/Istanbul")
    });

// C-03 (Plan 33 Faz 2) — Günlük tamim hatırlatma cron (her gün 09:00 Europe/Istanbul).
// Dün yayınlanan tamimler için henüz okumamış kullanıcılara in-app bildirim.
RecurringJob.AddOrUpdate<Mosaik.Modules.Circular.Services.TamimReminderJob>(
    recurringJobId: "daily-tamim-reminder",
    methodCall: job => job.ExecuteAsync(CancellationToken.None),
    cronExpression: "0 9 * * *",
    options: new RecurringJobOptions
    {
        TimeZone = TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "Turkey Standard Time" : "Europe/Istanbul")
    });

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

// Plan 36 W-09 + W-11 — Workflow step deadline + escalation processor (her 30 dk).
RecurringJob.AddOrUpdate<Mosaik.Services.Workflow.WorkflowStepProcessor>(
    recurringJobId: "workflow-step-processor",
    methodCall: job => job.ExecuteAsync(CancellationToken.None),
    cronExpression: "*/30 * * * *");

// Plan 34 Faz E S-21 — SOP okuma hatırlatması (her gün 09:00 Europe/Istanbul).
// 7 gün kala in-app push (count=0→1), 1 gün kala son uyarı (count=1→2).
RecurringJob.AddOrUpdate<Mosaik.Modules.SOP.Services.SopReadReminderJob>(
    recurringJobId: "sop-read-reminder-daily",
    methodCall: job => job.ExecuteAsync(CancellationToken.None),
    cronExpression: "0 9 * * *",
    options: new RecurringJobOptions
    {
        TimeZone = TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "Turkey Standard Time" : "Europe/Istanbul")
    });

// Plan 34.1 Faz 6 A-28 — SOP AI conversation 1-yıl KVKK retention cleanup (her gün 03:00 Europe/Istanbul).
RecurringJob.AddOrUpdate<Mosaik.Modules.SOP.Services.SopAiHistoryCleanupJob>(
    recurringJobId: "sop-ai-history-cleanup",
    methodCall: job => job.ExecuteAsync(CancellationToken.None),
    cronExpression: "0 3 * * *",
    options: new RecurringJobOptions
    {
        TimeZone = TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "Turkey Standard Time" : "Europe/Istanbul")
    });

// Plan 54 M4 — Dashboard→Alert eşik sweeper (her saat başı, Europe/Istanbul).
// Aktif EscalationRule'ları GLOBAL değerlendirir → eşik aşımında INotificationService bildirim (günlük dedup).
RecurringJob.AddOrUpdate<Mosaik.Services.EscalationSweeperJob>(
    recurringJobId: "escalation-sweeper",
    methodCall: job => job.ExecuteAsync(CancellationToken.None),
    cronExpression: "0 * * * *",
    options: new RecurringJobOptions
    {
        TimeZone = TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "Turkey Standard Time" : "Europe/Istanbul")
    });

// Plan 54 M5 — günlük okunmamış bildirim digest'i (her gün 08:00, Europe/Istanbul).
RecurringJob.AddOrUpdate<Mosaik.Services.NotificationDigestJob>(
    recurringJobId: "notification-digest-daily",
    methodCall: job => job.ExecuteAsync(CancellationToken.None),
    cronExpression: "0 8 * * *",
    options: new RecurringJobOptions
    {
        TimeZone = TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "Turkey Standard Time" : "Europe/Istanbul")
    });

app.Run();
