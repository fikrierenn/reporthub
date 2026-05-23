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
            // Plan 34 Faz E S-21 — günlük okuma hatırlatma cron.
            services.AddScoped<Services.SopReadReminderJob>();
            // Plan 34.1 Faz 1 — RAG pipeline (indexer + retriever).
            services.AddScoped<Services.SopIndexer>();
            services.AddScoped<Services.SopChunkRetriever>();
            // Plan 34.1 Faz 2 — RAG advisor.
            services.AddScoped<Services.SopRagAdvisorService>();
            // Plan 34.1 Faz 3 — Rate limit guard.
            services.AddScoped<Services.SopRateLimitGuard>();
            // Plan 34.1 Faz 6 — KVKK retention cleanup (daily).
            services.AddScoped<Services.SopAiHistoryCleanupJob>();
            // Plan 34.1 Save-time AI Enrichment — SOP save sonrası otomatik review.
            services.AddScoped<Services.SopEnrichmentService>();
        }

        public void ConfigureModelBuilder(ModelBuilder mb)
        {
            // DB tabloları migration'larda çoğul yaratıldı (SopDocuments, SopVersions, vb.)
            // EF default convention entity adına (tekil) eşler — explicit ToTable şart.
            mb.Entity<SopDocument>(e =>
            {
                e.ToTable("SopDocuments");
                e.HasKey(x => x.Id);
                e.Property(x => x.Title).HasMaxLength(200).IsRequired();
                e.Property(x => x.Category).HasMaxLength(80);
                e.Property(x => x.DepartmentIds).HasMaxLength(500);
                e.HasIndex(x => new { x.FirmaId, x.IsActive });
                e.HasIndex(x => x.OwnerUserId);
            });

            mb.Entity<SopVersion>(e =>
            {
                e.ToTable("SopVersions");
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
                e.ToTable("SopReadReceipts");
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
                e.ToTable("SopApprovalSubmissions");
                e.HasKey(x => x.Id);
                e.HasOne(x => x.SopVersion)
                    .WithMany(v => v.ApprovalSubmissions)
                    .HasForeignKey(x => x.SopVersionId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(x => x.SopVersionId);
                e.HasIndex(x => x.ApprovalRequestId);
            });

            // Plan 34.1 Faz 1 A-07 — RAG chunk + embedding.
            mb.Entity<SopChunk>(e =>
            {
                e.ToTable("SopChunks");
                e.HasKey(x => x.Id);
                e.HasOne(x => x.SopVersion)
                    .WithMany()
                    .HasForeignKey(x => x.SopVersionId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(x => new { x.SopVersionId, x.ChunkOrder });
            });

            // Plan 34.1 Faz 2 A-12 — RAG advisor soru-cevap.
            mb.Entity<SopAiConversation>(e =>
            {
                e.ToTable("SopAiConversations");
                e.HasKey(x => x.Id);
                e.HasIndex(x => new { x.UserId, x.CreatedAt });
            });

            // Plan 34.1 Save-time AI Enrichment — review report.
            mb.Entity<SopEnrichmentReport>(e =>
            {
                e.ToTable("SopEnrichmentReports");
                e.HasKey(x => x.Id);
                e.HasOne(x => x.SopVersion)
                    .WithMany()
                    .HasForeignKey(x => x.SopVersionId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(x => new { x.SopVersionId, x.CreatedAt });
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
