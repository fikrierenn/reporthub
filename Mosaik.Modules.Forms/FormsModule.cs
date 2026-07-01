using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mosaik.Core.Module;
using Mosaik.Modules.Forms.Entities;

namespace Mosaik.Modules.Forms
{
    // Plan 41 Faz 0 scaffold (2026-05-21).
    // Hybrid yaklaşım: v1 JSON config + admin form CRUD; v2 drag-drop builder UI ileride.
    // Stack (ADR-020): SurveyJS Form Library (MIT) renderer + DIY builder + signature_pad
    //                  + honeypot/time-based AntiSpam + Cloudflare Turnstile opsiyonel.
    public class FormsModule : IMosaikModule
    {
        public string ModuleKey => "forms";
        public string DisplayName => "Formlar";
        public string? Icon => "fas fa-clipboard-list";
        public int DisplayOrder => 250;
        public string? MigrationFolder => "Database";

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<Services.FormRendererService>();
            services.AddScoped<Services.FormValidationService>();
            services.AddScoped<Services.FormSubmissionService>();
            services.AddScoped<Services.FormDefinitionService>();
            services.AddScoped<Services.FormFieldService>();
            // Plan 41 Faz 3+ servisleri (sonraki commit'lerde):
            // services.AddScoped<Services.PublicTokenService>();
            // services.AddScoped<Services.FormEncryptionService>();
            // services.AddScoped<Services.DataElementMapValidator>();
        }

        public void ConfigureModelBuilder(ModelBuilder mb)
        {
            mb.Entity<FormDefinition>(e =>
            {
                e.ToTable("FormDefinitions");
                e.HasKey(x => x.Id);
                e.Property(x => x.Slug).HasMaxLength(120).IsRequired();
                e.Property(x => x.Name).HasMaxLength(200).IsRequired();
                e.Property(x => x.Description);
                e.Property(x => x.Category).HasMaxLength(80);
                e.HasIndex(x => new { x.FirmaId, x.Slug }).IsUnique();  // multi-tenant: her firmada ayrı slug
                e.HasIndex(x => new { x.FirmaId, x.Status });
            });

            mb.Entity<FormField>(e =>
            {
                e.ToTable("FormFields");
                e.HasKey(x => x.Id);
                e.Property(x => x.FieldKey).HasMaxLength(80).IsRequired();
                e.Property(x => x.Label).HasMaxLength(300).IsRequired();
                e.Property(x => x.HelpText).HasMaxLength(500);
                e.Property(x => x.Placeholder).HasMaxLength(200);
                e.HasOne(x => x.FormDefinition)
                    .WithMany(d => d.Fields)
                    .HasForeignKey(x => x.FormDefinitionId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(x => new { x.FormDefinitionId, x.FieldKey }).IsUnique();
                e.HasIndex(x => new { x.FormDefinitionId, x.Order });
            });

            mb.Entity<FormFieldDataElementMap>(e =>
            {
                e.ToTable("FormFieldDataElementMaps");
                e.HasKey(x => x.Id);
                e.Property(x => x.Notes).HasMaxLength(500);
                e.HasOne(x => x.FormField)
                    .WithMany(f => f.DataElementMaps)
                    .HasForeignKey(x => x.FormFieldId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(x => new { x.FormFieldId, x.DataElementId }).IsUnique();
            });

            mb.Entity<FormSubmission>(e =>
            {
                e.ToTable("FormSubmissions");
                e.HasKey(x => x.Id);
                e.Property(x => x.SubmitterEmail).HasMaxLength(200);
                e.Property(x => x.SubmitterPhone).HasMaxLength(40);
                e.Property(x => x.SubmitterIp).HasMaxLength(45);
                e.Property(x => x.SubmitterUserAgent).HasMaxLength(500);
                e.HasOne(x => x.FormDefinition)
                    .WithMany(d => d.Submissions)
                    .HasForeignKey(x => x.FormDefinitionId)
                    .OnDelete(DeleteBehavior.Restrict);  // KVKK: submission kayıtları silinmez
                e.HasOne(x => x.FormVersion)
                    .WithMany()
                    .HasForeignKey(x => x.FormVersionId)
                    .OnDelete(DeleteBehavior.Restrict);  // rev 2 §4.6 — ZORUNLU, versiyon geçmişi silinmez
                e.HasIndex(x => new { x.FormDefinitionId, x.SubmittedAt });
                e.HasIndex(x => new { x.SubmittedById, x.SubmittedAt });
            });

            mb.Entity<FormSubmissionFieldValue>(e =>
            {
                e.ToTable("FormSubmissionFieldValues");
                e.HasKey(x => x.Id);
                e.Property(x => x.FieldKey).HasMaxLength(80).IsRequired();
                e.Property(x => x.ValueNumber).HasPrecision(18, 4);   // DECIMAL(18,4) — SQL migration ile eşleşir
                e.HasOne(x => x.FormSubmission)
                    .WithMany(s => s.Values)
                    .HasForeignKey(x => x.FormSubmissionId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.FormField)
                    .WithMany()
                    .HasForeignKey(x => x.FormFieldId)
                    .OnDelete(DeleteBehavior.Restrict);
                e.HasIndex(x => new { x.FormSubmissionId, x.FieldKey });
            });

            mb.Entity<PublicFormToken>(e =>
            {
                e.ToTable("PublicFormTokens");
                e.HasKey(x => x.Id);
                e.Property(x => x.TokenHash).IsRequired();
                e.Property(x => x.RecipientEmail).HasMaxLength(200);
                e.HasOne(x => x.FormDefinition)
                    .WithMany(d => d.PublicTokens)
                    .HasForeignKey(x => x.FormDefinitionId)
                    .OnDelete(DeleteBehavior.Cascade);   // form silinince tokenlar da silinir
                e.HasIndex(x => x.TokenHash).IsUnique();
            });

            mb.Entity<FormDefinitionVersion>(e =>
            {
                e.ToTable("FormDefinitionVersions");
                e.HasKey(x => x.Id);
                e.HasOne(x => x.FormDefinition)
                    .WithMany(d => d.Versions)
                    .HasForeignKey(x => x.FormDefinitionId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(x => new { x.FormDefinitionId, x.Version }).IsUnique();
            });
        }

        public void MapEndpoints(IEndpointRouteBuilder endpoints)
        {
            endpoints.MapAreaControllerRoute(
                name: "forms",
                areaName: "Forms",
                pattern: "Forms/{controller=Home}/{action=Index}/{id?}");
        }
    }
}
