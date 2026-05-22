using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mosaik.Core.Module;
using Mosaik.Modules.SOP.Entities;

namespace Mosaik.Modules.SOP
{
    // Plan 34 Faz A scaffold (2026-05-22), Faz B entity binding (2026-05-22).
    // SOP / Prosedür Yönetimi — vNext kalbi #1 (VISION.md).
    // Sonraki fazlar: C (admin CRUD + Quill + ApprovalRequest),
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
            // Plan 34 Faz C registrations.
            services.AddScoped<Services.SopService>();
            services.AddScoped<Services.SopApprovalService>();
            services.AddScoped<Services.SopReadReceiptService>();
            // Faz F: services.AddScoped<Services.SopAiAdvisorService>();
            // Faz F: services.AddScoped<Services.SopRateLimitGuard>();
        }

        public void ConfigureModelBuilder(ModelBuilder mb)
        {
            mb.Entity<SopDocument>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Title).HasMaxLength(200).IsRequired();
                e.Property(x => x.Category).HasMaxLength(80);
                e.Property(x => x.DepartmentIds).HasMaxLength(500);
                e.HasIndex(x => new { x.FirmaId, x.IsActive });
                e.HasIndex(x => x.OwnerUserId);
            });

            mb.Entity<SopVersion>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.ContentJson).IsRequired();
                e.HasOne(x => x.SopDocument)
                    .WithMany(d => d.Versions)
                    .HasForeignKey(x => x.SopDocumentId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(x => new { x.SopDocumentId, x.VersionNumber }).IsUnique();
                e.HasIndex(x => x.Status);
            });

            mb.Entity<SopReadReceipt>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasOne(x => x.SopVersion)
                    .WithMany(v => v.ReadReceipts)
                    .HasForeignKey(x => x.SopVersionId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(x => new { x.SopVersionId, x.UserId }).IsUnique();
                // UserId + ConfirmedAt filtered index migration 01'de oluşturuldu (EF model'de tanımlanmıyor).
            });

            mb.Entity<SopApprovalSubmission>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasOne(x => x.SopVersion)
                    .WithMany(v => v.ApprovalSubmissions)
                    .HasForeignKey(x => x.SopVersionId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(x => x.SopVersionId);
                e.HasIndex(x => x.ApprovalRequestId);
            });
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
