using Microsoft.EntityFrameworkCore;
using ReportPanel.Models;
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
builder.Services.AddScoped<ReportPanel.Services.AuditLogService>();
builder.Services.AddScoped<ReportPanel.Services.UserRoleSyncService>();
builder.Services.AddScoped<ReportPanel.Services.CategoryManagementService>();
builder.Services.AddScoped<ReportPanel.Services.RoleManagementService>();
builder.Services.AddScoped<ReportPanel.Services.DataSourceManagementService>();
builder.Services.AddScoped<ReportPanel.Services.ReportManagementService>();
builder.Services.AddScoped<ReportPanel.Services.UserManagementService>();
builder.Services.AddScoped<ReportPanel.Services.SpExplorerService>();
builder.Services.AddScoped<ReportPanel.Services.FilterOptionsService>();
builder.Services.AddScoped<ReportPanel.Services.ExcelExportService>();
builder.Services.AddScoped<ReportPanel.Services.UserDataFilterInjector>();
builder.Services.AddScoped<ReportPanel.Services.StoredProcedureExecutor>();

// Add Entity Framework
builder.Services.AddDbContext<ReportPanelContext>(options =>
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
    .AddDbContextCheck<ReportPanelContext>("database")
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
