using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mosaik.Core.Module;
using Mosaik.Modules.Kvkk.Entities;

namespace Mosaik.Modules.Kvkk
{
    // Plan 54 M6 / Plan 40 — KVKK Veri Envanteri + Process backbone.
    // Faz 0: veri modeli omurgası (entity + REF lookup + DataElement seed + EntityRelations hook).
    // Sonraki fazlar: 1 xlsx import, 2 CRUD UI, 3 SOP entegrasyon, 4 reverse search,
    // 5 AI integrity, 6 VERBİS export, 7 risk dashboard.
    public class KvkkModule : IMosaikModule
    {
        public string ModuleKey => "kvkk";
        public string DisplayName => "KVKK Envanteri";
        public string? Icon => "fas fa-shield-halved";
        public int DisplayOrder => 280;
        public string? MigrationFolder => "Database";

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<Services.KvkkProcessService>();
            services.AddScoped<Services.DataElementService>();
            services.AddScoped<Services.XlsxImporter>();
            services.AddScoped<Services.VerbisExporter>();
            services.AddScoped<Services.KvkkIntegrityChecker>();
            services.AddScoped<Services.KvkkIntegrityFindingService>();
            services.AddScoped<Services.KvkkIntegrityScanJob>();
        }

        public void ConfigureModelBuilder(ModelBuilder mb)
        {
            // --- REF lookuplar ---
            mb.Entity<DataCategory>(e =>
            {
                e.ToTable("KvkkDataCategories");
                e.HasKey(x => x.Id);
                e.Property(x => x.Code).HasMaxLength(60).IsRequired();
                e.Property(x => x.Name).HasMaxLength(150).IsRequired();
                e.HasIndex(x => x.Code).IsUnique();
            });
            mb.Entity<LegalBasis>(e =>
            {
                e.ToTable("KvkkLegalBases");
                e.HasKey(x => x.Id);
                e.Property(x => x.Code).HasMaxLength(20).IsRequired();
                e.Property(x => x.Name).HasMaxLength(300).IsRequired();
                e.Property(x => x.Article).HasMaxLength(20).IsRequired();
                e.HasIndex(x => x.Code).IsUnique();
            });
            mb.Entity<PersonGroup>(e =>
            {
                e.ToTable("KvkkPersonGroups");
                e.HasKey(x => x.Id);
                e.Property(x => x.Code).HasMaxLength(60).IsRequired();
                e.Property(x => x.Name).HasMaxLength(150).IsRequired();
                e.HasIndex(x => x.Code).IsUnique();
            });
            mb.Entity<RetentionRule>(e =>
            {
                e.ToTable("KvkkRetentionRules");
                e.HasKey(x => x.Id);
                e.Property(x => x.Code).HasMaxLength(60).IsRequired();
                e.Property(x => x.Name).HasMaxLength(200).IsRequired();
                e.Property(x => x.DurationText).HasMaxLength(80).IsRequired();
                e.Property(x => x.LegalReference).HasMaxLength(200);
                e.HasIndex(x => x.Code).IsUnique();
            });
            mb.Entity<DisposalMethod>(e =>
            {
                e.ToTable("KvkkDisposalMethods");
                e.HasKey(x => x.Id);
                e.Property(x => x.Code).HasMaxLength(60).IsRequired();
                e.Property(x => x.Name).HasMaxLength(150).IsRequired();
                e.HasIndex(x => x.Code).IsUnique();
            });
            mb.Entity<MeasureStandard>(e =>
            {
                e.ToTable("KvkkMeasureStandards");
                e.HasKey(x => x.Id);
                e.Property(x => x.Code).HasMaxLength(60).IsRequired();
                e.Property(x => x.Name).HasMaxLength(250).IsRequired();
                e.HasIndex(x => x.Code).IsUnique();
            });
            mb.Entity<ProcessingPurpose>(e =>
            {
                e.ToTable("KvkkProcessingPurposes");
                e.HasKey(x => x.Id);
                e.Property(x => x.Code).HasMaxLength(60).IsRequired();
                e.Property(x => x.Name).HasMaxLength(250).IsRequired();
                e.HasIndex(x => x.Code).IsUnique();
            });
            mb.Entity<Recipient>(e =>
            {
                e.ToTable("KvkkRecipients");
                e.HasKey(x => x.Id);
                e.Property(x => x.Code).HasMaxLength(60).IsRequired();
                e.Property(x => x.Name).HasMaxLength(200).IsRequired();
                e.HasIndex(x => x.Code).IsUnique();
            });

            // --- Core ---
            mb.Entity<KvkkProcess>(e =>
            {
                e.ToTable("KvkkProcesses");
                e.HasKey(x => x.Id);
                e.Property(x => x.Department).HasMaxLength(120).IsRequired();
                e.Property(x => x.Unit).HasMaxLength(120);
                e.Property(x => x.Owner).HasMaxLength(200);
                e.Property(x => x.Name).HasMaxLength(300).IsRequired();
                e.Property(x => x.Purpose).IsRequired();
                e.Property(x => x.DataSource).HasMaxLength(500);
                e.Property(x => x.StorageMedium).HasMaxLength(500);
                e.Property(x => x.AccessAuthority).HasMaxLength(500);
                e.Property(x => x.RecipientGroups).HasMaxLength(500);
                e.Property(x => x.RetentionText).HasMaxLength(500);
                e.Property(x => x.DisposalText).HasMaxLength(500);
                e.HasIndex(x => new { x.FirmaId, x.Department });
                e.HasIndex(x => new { x.FirmaId, x.RiskLevel });
                e.HasIndex(x => new { x.FirmaId, x.Department, x.Name }).IsUnique(); // natural key

                e.HasOne(x => x.LegalBasis).WithMany().HasForeignKey(x => x.LegalBasisId)
                    .OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.RetentionRule).WithMany().HasForeignKey(x => x.RetentionRuleId)
                    .OnDelete(DeleteBehavior.SetNull);
                e.HasOne(x => x.DisposalMethod).WithMany().HasForeignKey(x => x.DisposalMethodId)
                    .OnDelete(DeleteBehavior.SetNull);
                e.HasOne(x => x.ProcessingPurpose).WithMany().HasForeignKey(x => x.ProcessingPurposeId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            mb.Entity<DataElement>(e =>
            {
                e.ToTable("KvkkDataElements");
                e.HasKey(x => x.Id);
                e.Property(x => x.ElementCode).HasMaxLength(80).IsRequired();
                e.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
                e.Property(x => x.DefaultRetentionHint).HasMaxLength(200);
                e.Property(x => x.Aliases).HasMaxLength(500);
                e.HasIndex(x => x.ElementCode).IsUnique();
                e.HasOne(x => x.DataCategory).WithMany().HasForeignKey(x => x.DataCategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            mb.Entity<ProcessDataLink>(e =>
            {
                e.ToTable("KvkkProcessDataLinks");
                e.HasKey(x => x.Id);
                e.Property(x => x.Notes).HasMaxLength(500);
                e.HasIndex(x => new { x.ProcessId, x.DataElementId, x.UsageType }).IsUnique();
                e.HasIndex(x => x.DataElementId);   // reverse lookup hot path
                e.HasOne(x => x.Process).WithMany(p => p.DataLinks).HasForeignKey(x => x.ProcessId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.DataElement).WithMany(d => d.ProcessLinks).HasForeignKey(x => x.DataElementId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            mb.Entity<CrossBorderTransfer>(e =>
            {
                e.ToTable("KvkkCrossBorderTransfers");
                e.HasKey(x => x.Id);
                e.Property(x => x.RecipientName).HasMaxLength(300).IsRequired();
                e.Property(x => x.Country).HasMaxLength(120).IsRequired();
                e.Property(x => x.LegalReference).HasMaxLength(200);
                e.Property(x => x.DocumentLink).HasMaxLength(500);
                e.HasIndex(x => x.ProcessId);
                e.HasOne(x => x.Process).WithMany(p => p.CrossBorderTransfers).HasForeignKey(x => x.ProcessId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            mb.Entity<KvkkIntegrityFinding>(e =>
            {
                e.ToTable("KvkkIntegrityFindings");
                e.HasKey(x => x.Id);
                e.Property(x => x.PatternCode).HasMaxLength(60).IsRequired();
                e.Property(x => x.Description).HasMaxLength(500).IsRequired();
                e.Property(x => x.DismissReason).HasMaxLength(500);
                e.HasIndex(x => new { x.FirmaId, x.PatternCode, x.ProcessId });
                e.HasIndex(x => new { x.FirmaId, x.IsDismissed, x.Severity });
                e.HasOne(x => x.Process).WithMany().HasForeignKey(x => x.ProcessId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }

        public void MapEndpoints(IEndpointRouteBuilder endpoints)
        {
            // Faz 2'de ProcessController vb. eklenince aktif olur (şimdilik controller yok).
            endpoints.MapAreaControllerRoute(
                name: "kvkk",
                areaName: "Kvkk",
                pattern: "Kvkk/{controller=Process}/{action=Index}/{id?}");
        }
    }
}
