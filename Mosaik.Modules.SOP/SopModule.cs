using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mosaik.Core.Module;

namespace Mosaik.Modules.SOP
{
    // Plan 34 Faz A scaffold (2026-05-22).
    // SOP / Prosedür Yönetimi — vNext kalbi #1 (VISION.md).
    // Sonraki fazlar: B (entity + migration), C (admin CRUD + Quill + ApprovalRequest),
    // D (user-facing "Prosedürlerim" + okundu), E (bildirim), F (AI Danışman).
    public class SopModule : IMosaikModule
    {
        public string ModuleKey => "sop";
        public string DisplayName => "Prosedürler";
        public string? Icon => "fas fa-book-open";
        public int DisplayOrder => 270;
        public string? MigrationFolder => "Database";

        public void ConfigureServices(IServiceCollection services)
        {
            // Plan 34 Faz B+ servisleri (sonraki commit'lerde):
            // services.AddScoped<Services.SopService>();
            // services.AddScoped<Services.SopApprovalService>();
            // services.AddScoped<Services.SopReadReceiptService>();
            // services.AddScoped<Services.SopAiAdvisorService>();
            // services.AddScoped<Services.SopRateLimitGuard>();
        }

        public void ConfigureModelBuilder(ModelBuilder mb)
        {
            // Plan 34 Faz B'de SopDocument, SopVersion, SopReadReceipt,
            // SopApprovalSubmission, SopAiConversation entity'leri eklenecek.
        }

        public void MapEndpoints(IEndpointRouteBuilder endpoints)
        {
            endpoints.MapAreaControllerRoute(
                name: "sop",
                areaName: "SOP",
                pattern: "SOP/{controller=Home}/{action=Index}/{id?}");
        }
    }
}
