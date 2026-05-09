using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Domain;
using Mosaik.Core.Lookup;
using Mosaik.Core.Workflow;

namespace Mosaik.Models
{
    public class MosaikContext : DbContext
    {
        public MosaikContext(DbContextOptions<MosaikContext> options) : base(options)
        {
        }

        public DbSet<DataSource> DataSources { get; set; }
        public DbSet<ReportCatalog> ReportCatalog { get; set; }
        public DbSet<ReportRunLog> ReportRunLog { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<ReportFavorite> ReportFavorites { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<ReportGroup> ReportGroups { get; set; }
        public DbSet<ReportGroupLink> ReportGroupLinks { get; set; }
        public DbSet<ReportAllowedRole> ReportAllowedRoles { get; set; }
        public DbSet<UserDataFilter> UserDataFilters { get; set; }
        public DbSet<FilterDefinition> FilterDefinitions { get; set; }
        public DbSet<BrandSettings> BrandSettings { get; set; }
        public DbSet<AppModule> AppModules { get; set; }
        public DbSet<AiSettings> AiSettings { get; set; }

        // Plan 16.5 Faz A — Workflow primitives
        public DbSet<ApprovalRequest> ApprovalRequests { get; set; }
        public DbSet<ApprovalStep> ApprovalSteps { get; set; }
        public DbSet<StatusTransition> StatusTransitions { get; set; }

        // Plan 16.5 Faz B — Lookup (dictionary)
        public DbSet<DictionaryType> DictionaryTypes { get; set; }
        public DbSet<DictionaryValue> DictionaryValues { get; set; }

        // Plan 20 Faz A — Organizasyon şeması
        public DbSet<OrgPosition> OrgPositions { get; set; }

        // Plan 17 Faz H — cross-modül bildirim
        public DbSet<Mosaik.Core.Notification.Notification> Notifications { get; set; } = default!;

        // Plan 25 — Sözleşme modülü (ADR-012: int PK, FirmaId zorunlu)
        public DbSet<Firma> Firmas { get; set; }
        public DbSet<Contract> Contracts { get; set; }
        public DbSet<ContractObligation> ContractObligations { get; set; }
        public DbSet<ContractRecurrence> ContractRecurrences { get; set; }
        public DbSet<ContractFile> ContractFiles { get; set; }
        public DbSet<ContractAiExtraction> ContractAiExtractions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // DataSource configuration
            modelBuilder.Entity<DataSource>(entity =>
            {
                entity.HasKey(e => e.DataSourceKey);
                entity.Property(e => e.DataSourceKey).HasMaxLength(50);
                entity.Property(e => e.Title).HasMaxLength(100).IsRequired();
                entity.Property(e => e.ConnString).HasMaxLength(1000).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            });

            // ReportCatalog configuration
            modelBuilder.Entity<ReportCatalog>(entity =>
            {
                entity.HasKey(e => e.ReportId);
                entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.DataSourceKey).HasMaxLength(50);
                entity.Property(e => e.ProcName).HasMaxLength(200).IsRequired();
                entity.Property(e => e.AllowedRoles).HasMaxLength(200).IsRequired();
#pragma warning disable CS0618 // ADR-009: ReportType [Obsolete] — Migration 19 drop edince bu satir da silinir.
                entity.Property(e => e.ReportType).HasMaxLength(20).IsRequired().HasDefaultValue("dashboard");
#pragma warning restore CS0618
                entity.Property(e => e.DashboardConfigJson);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
                
                entity.HasOne(d => d.DataSource)
                    .WithMany(p => p.Reports)
                    .HasForeignKey(d => d.DataSourceKey);
            });

            // ReportRunLog configuration
            modelBuilder.Entity<ReportRunLog>(entity =>
            {
                entity.HasKey(e => e.RunId);
                entity.Property(e => e.Username).HasMaxLength(100).IsRequired();
                entity.Property(e => e.DataSourceKey).HasMaxLength(50).IsRequired();
                entity.Property(e => e.RunAt).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
                
                entity.HasOne(d => d.Report)
                    .WithMany(p => p.RunLogs)
                    .HasForeignKey(d => d.ReportId);
            });

            // AuditLog configuration
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.AuditId);
                entity.ToTable("AuditLog");
                entity.Property(e => e.Username).HasMaxLength(100).IsRequired();
                entity.Property(e => e.EventType).HasMaxLength(50).IsRequired();
                entity.Property(e => e.TargetType).HasMaxLength(50);
                entity.Property(e => e.TargetKey).HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
                entity.Property(e => e.DataSourceKey).HasMaxLength(50);
                entity.Property(e => e.IpAddress).HasMaxLength(45);
                entity.Property(e => e.UserAgent).HasMaxLength(300);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            });

            // Users configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.UserId);
                entity.Property(e => e.Username).HasMaxLength(50).IsRequired();
                entity.Property(e => e.PasswordHash).HasMaxLength(255).IsRequired();
                entity.Property(e => e.FullName).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.IsAdUser).HasDefaultValue(false);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()");
            });

            // Roles configuration
            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(e => e.RoleId);
                entity.Property(e => e.Name).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(200);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            });

            // UserRoles configuration
            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.RoleId });
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Role)
                    .WithMany()
                    .HasForeignKey(e => e.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ReportGroups configuration (Plan 07 son rename: ReportCategories → ReportGroups)
            modelBuilder.Entity<ReportGroup>(entity =>
            {
                entity.HasKey(e => e.GroupId);
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(300);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            });

            // ReportGroupLinks configuration
            modelBuilder.Entity<ReportGroupLink>(entity =>
            {
                entity.HasKey(e => new { e.ReportId, e.GroupId });
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.Report)
                    .WithMany(r => r.ReportGroups)
                    .HasForeignKey(e => e.ReportId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Group)
                    .WithMany()
                    .HasForeignKey(e => e.GroupId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ReportAllowedRoles configuration
            modelBuilder.Entity<ReportAllowedRole>(entity =>
            {
                entity.HasKey(e => new { e.ReportId, e.RoleId });
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.Report)
                    .WithMany(r => r.ReportAllowedRoles)
                    .HasForeignKey(e => e.ReportId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Role)
                    .WithMany()
                    .HasForeignKey(e => e.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ReportFavorites configuration
            modelBuilder.Entity<ReportFavorite>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.ReportId });
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Report)
                    .WithMany()
                    .HasForeignKey(e => e.ReportId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // UserDataFilters configuration
            modelBuilder.Entity<UserDataFilter>(entity =>
            {
                entity.HasKey(e => e.FilterId);
                entity.Property(e => e.FilterKey).HasMaxLength(50).IsRequired();
                entity.Property(e => e.FilterValue).HasMaxLength(100).IsRequired();
                entity.Property(e => e.DataSourceKey).HasMaxLength(50);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.DataSource)
                    .WithMany()
                    .HasForeignKey(e => e.DataSourceKey)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.Report)
                    .WithMany()
                    .HasForeignKey(e => e.ReportId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // FilterDefinition configuration (Plan 07 Faz 2 — master tablo, Migration 20)
            modelBuilder.Entity<FilterDefinition>(entity =>
            {
                entity.ToTable("FilterDefinition");
                entity.HasKey(e => e.FilterDefinitionId);
                entity.Property(e => e.FilterKey).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Label).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Scope).HasMaxLength(20).IsRequired();
                entity.Property(e => e.DataSourceKey).HasMaxLength(50);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.DisplayOrder).HasDefaultValue(0);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

                // Plan B (5 Mayis): composite (DataSourceKey, FilterKey) — ayni 'sube' 3 DataSource icin 3 satir
                entity.HasIndex(e => new { e.DataSourceKey, e.FilterKey }).IsUnique();

                entity.HasOne(e => e.DataSource)
                    .WithMany()
                    .HasForeignKey(e => e.DataSourceKey)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // BrandSettings — tek satır (Id=1), singleton config
            modelBuilder.Entity<BrandSettings>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SiteTitle).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Slogan).HasMaxLength(200);
                entity.Property(e => e.LogoPath).HasMaxLength(500);
                entity.Property(e => e.PrimaryColor).HasMaxLength(7).IsRequired();
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
            });

            // AppModules — sidebar modül listesi, IsEnabled toggle
            modelBuilder.Entity<AppModule>(entity =>
            {
                entity.HasKey(e => e.ModuleId);
                entity.Property(e => e.ModuleKey).HasMaxLength(50).IsRequired();
                entity.Property(e => e.DisplayName).HasMaxLength(100).IsRequired();
                entity.HasIndex(e => e.ModuleKey).IsUnique();
            });

            // Plan 16.5 Faz A — Workflow primitives
            modelBuilder.Entity<ApprovalRequest>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.EntityType).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Subject).HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.HasIndex(e => new { e.EntityType, e.EntityId });
                entity.HasIndex(e => e.Status);
            });

            modelBuilder.Entity<ApprovalStep>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ApproverRole).HasMaxLength(50);
                entity.Property(e => e.Comment).HasMaxLength(500);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.HasOne(s => s.Request)
                      .WithMany(r => r.Steps)
                      .HasForeignKey(s => s.RequestId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => new { e.RequestId, e.StepOrder });
                entity.HasIndex(e => e.Status);
            });

            modelBuilder.Entity<StatusTransition>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.EntityType).HasMaxLength(50).IsRequired();
                entity.Property(e => e.FromStatus).HasMaxLength(30).IsRequired();
                entity.Property(e => e.ToStatus).HasMaxLength(30).IsRequired();
                entity.Property(e => e.AllowedRoles).HasMaxLength(200);
                entity.HasIndex(e => new { e.EntityType, e.FromStatus, e.ToStatus }).IsUnique();
            });

            // Plan 16.5 Faz B — Lookup
            modelBuilder.Entity<DictionaryType>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.HasIndex(e => e.Code).IsUnique();
            });

            modelBuilder.Entity<DictionaryValue>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Label).HasMaxLength(200).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.HasOne(v => v.Type)
                      .WithMany(t => t.Values)
                      .HasForeignKey(v => v.TypeId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => new { e.TypeId, e.Code }).IsUnique();
                entity.HasIndex(e => new { e.TypeId, e.IsActive, e.DisplayOrder });
            });

            // Plan 20 Faz A — OrgPosition (self-ref hierarchy)
            modelBuilder.Entity<OrgPosition>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Code).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Title).HasMaxLength(150).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.HasIndex(e => e.Code).IsUnique();
                entity.HasIndex(e => e.ParentPositionId);
                entity.HasOne(p => p.Parent)
                      .WithMany(p => p.Children)
                      .HasForeignKey(p => p.ParentPositionId)
                      .OnDelete(DeleteBehavior.Restrict); // self-ref CASCADE yasak
            });

            // Plan 25 — Sözleşme modülü
            modelBuilder.Entity<Firma>(e =>
            {
                e.HasKey(f => f.FirmaId);
                e.Property(f => f.Code).HasColumnName("Kod").HasMaxLength(50).IsRequired();
                e.Property(f => f.Name).HasColumnName("Ad").HasMaxLength(100).IsRequired();
                e.HasIndex(f => f.Code).IsUnique();
            });

            modelBuilder.Entity<Contract>(e =>
            {
                e.HasKey(c => c.Id);
                e.Property(c => c.Title).HasMaxLength(200).IsRequired();
                e.Property(c => c.Counterparty).HasMaxLength(200);
                e.Property(c => c.Notes).HasMaxLength(2000);
                e.Property(c => c.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                e.HasOne(c => c.Firma).WithMany().HasForeignKey(c => c.FirmaId).OnDelete(DeleteBehavior.Restrict);
                e.HasIndex(c => c.FirmaId);
                e.HasIndex(c => c.Status);
            });

            modelBuilder.Entity<ContractObligation>(e =>
            {
                e.HasKey(o => o.Id);
                e.Property(o => o.Title).HasMaxLength(200).IsRequired();
                e.Property(o => o.Currency).HasMaxLength(3);
                e.Property(o => o.Notes).HasMaxLength(2000);
                e.Property(o => o.CompletedBy).HasMaxLength(100);
                e.Property(o => o.Amount).HasPrecision(18, 2);
                e.Property(o => o.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                e.HasOne(o => o.Firma).WithMany().HasForeignKey(o => o.FirmaId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(o => o.Contract).WithMany(c => c.Obligations).HasForeignKey(o => o.ContractId).OnDelete(DeleteBehavior.SetNull);
                e.HasOne(o => o.Recurrence).WithMany(r => r.Obligations).HasForeignKey(o => o.RecurrenceId).OnDelete(DeleteBehavior.SetNull);
                e.HasOne(o => o.ParentObligation).WithMany(p => p.ChildObligations)
                    .HasForeignKey(o => o.ParentObligationId).OnDelete(DeleteBehavior.Restrict);
                e.HasIndex(o => new { o.FirmaId, o.Status });
                e.HasIndex(o => o.DueDate);
            });

            modelBuilder.Entity<ContractRecurrence>(e =>
            {
                e.HasKey(r => r.Id);
                e.Property(r => r.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            });

            modelBuilder.Entity<ContractFile>(e =>
            {
                e.HasKey(f => f.Id);
                e.Property(f => f.FileName).HasMaxLength(260).IsRequired();
                e.Property(f => f.FilePath).HasMaxLength(500).IsRequired();
                e.Property(f => f.MimeType).HasMaxLength(100);
                e.Property(f => f.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                e.HasOne(f => f.Firma).WithMany().HasForeignKey(f => f.FirmaId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(f => f.Contract).WithMany(c => c.Files).HasForeignKey(f => f.ContractId).OnDelete(DeleteBehavior.SetNull);
                e.HasOne(f => f.Obligation).WithMany().HasForeignKey(f => f.ObligationId).OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<ContractAiExtraction>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.PromptVersion).HasMaxLength(50);
                e.Property(x => x.ModelUsed).HasMaxLength(100);
                e.Property(x => x.ErrorMessage).HasMaxLength(1000);
                e.Property(x => x.ProgressStep).HasMaxLength(100);
                e.Property(x => x.ReviewedBy).HasMaxLength(100);
                e.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                e.HasOne(x => x.Firma).WithMany().HasForeignKey(x => x.FirmaId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.ContractFile).WithMany(f => f.AiExtractions).HasForeignKey(x => x.ContractFileId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Contract).WithMany().HasForeignKey(x => x.ContractId).OnDelete(DeleteBehavior.SetNull);
                e.HasIndex(x => x.Status);
            });

            // Plan 16.6 — Her vNext modül kendi entity'lerini ConfigureModelBuilder
            // metodu içinde kayıt eder. ModuleLoader assembly'leri tarar, sırayla
            // çağırır. Tamim, HR, Documents vs. burada aynı modelBuilder'a entity ekler.
            Mosaik.Core.Module.ModuleLoader.ApplyToModelBuilder(modelBuilder);

            base.OnModelCreating(modelBuilder);
        }
    }
}
