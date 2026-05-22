using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Domain;
using Mosaik.Core.Lookup;
using Mosaik.Core.Workflow;

namespace Mosaik.Models
{
    // Plan 39 Faz C-1 — partial class split. OnModelCreating private metodlara
    // dağıtıldı, her partial dosya bir domain alanı:
    //   MosaikContext.Reports.cs    → DataSource + ReportCatalog + ReportRunLog + ReportGroup/Link/AllowedRole/Favorite + UserDataFilter + FilterDefinition
    //   MosaikContext.Workflow.cs   → Approval + StatusTransition + Dictionary + WorkflowTemplate/Instance/Log
    //   MosaikContext.Contracts.cs  → Firma + Contract + ContractObligation/Recurrence/File/AiExtraction/Event + AiSuggestion + ComplianceTemplate
    //   MosaikContext.Compliance.cs → Holiday + HolidayOccurrence + ImportantDate + EntityRelation + DecisionLog
    public partial class MosaikContext : DbContext
    {
        public MosaikContext(DbContextOptions<MosaikContext> options) : base(options)
        {
        }

        // Core: Reports + DataSource + Filter
        public DbSet<DataSource> DataSources { get; set; }
        public DbSet<ReportCatalog> ReportCatalog { get; set; }
        public DbSet<ReportRunLog> ReportRunLog { get; set; }
        public DbSet<ReportFavorite> ReportFavorites { get; set; }
        public DbSet<ReportGroup> ReportGroups { get; set; }
        public DbSet<ReportGroupLink> ReportGroupLinks { get; set; }
        public DbSet<ReportAllowedRole> ReportAllowedRoles { get; set; }
        public DbSet<UserDataFilter> UserDataFilters { get; set; }
        public DbSet<FilterDefinition> FilterDefinitions { get; set; }

        // Core: Identity + Audit
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }

        // Core: Brand + Modules + AI config
        public DbSet<BrandSettings> BrandSettings { get; set; }
        public DbSet<AppModule> AppModules { get; set; }
        public DbSet<ModuleRoleAccess> ModuleRoleAccess { get; set; }
        public DbSet<AiSettings> AiSettings { get; set; }

        // Plan 16.5 Faz A — Workflow primitives (eski approval model — Plan 36 yanında devam ediyor)
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
        public DbSet<DocumentVersion> DocumentVersions { get; set; }
        public DbSet<ContractAiExtraction> ContractAiExtractions { get; set; }
        public DbSet<ContractEvent> ContractEvents { get; set; }
        public DbSet<AiSuggestion> AiSuggestions { get; set; }
        public DbSet<ComplianceTemplate> ComplianceTemplates { get; set; }

        // Plan 22 — Resmi tatiller + önemli günler (ADR-016)
        public DbSet<Holiday> Holidays { get; set; }
        public DbSet<HolidayOccurrence> HolidayOccurrences { get; set; }
        public DbSet<ImportantDate> ImportantDates { get; set; }

        // Plan 38 — Living Org Map + Decision Memory (VISION §7)
        public DbSet<Intelligence.EntityRelation> EntityRelations { get; set; }
        public DbSet<Intelligence.DecisionLog> DecisionLogs { get; set; }

        // Plan 36 — Workflow Designer + onay akışları
        public DbSet<Workflow.WorkflowTemplate> WorkflowTemplates { get; set; }
        public DbSet<Workflow.WorkflowInstance> WorkflowInstances { get; set; }
        public DbSet<Workflow.WorkflowInstanceLog> WorkflowInstanceLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ConfigureIdentityAndAudit(modelBuilder);
            ConfigureBrandAndModules(modelBuilder);
            ConfigureOrgChart(modelBuilder);
            ConfigureReports(modelBuilder);
            ConfigureWorkflowAndLookup(modelBuilder);
            ConfigureContracts(modelBuilder);
            ConfigureCompliance(modelBuilder);

            // Plan 16.6 — Her vNext modül kendi entity'lerini ConfigureModelBuilder
            // metodu içinde kayıt eder. ModuleLoader assembly'leri tarar, sırayla
            // çağırır. Tamim, HR, Documents vs. burada aynı modelBuilder'a entity ekler.
            Mosaik.Core.Module.ModuleLoader.ApplyToModelBuilder(modelBuilder);

            base.OnModelCreating(modelBuilder);
        }

        // Identity, Audit, Roles — core tablolar
        private static void ConfigureIdentityAndAudit(ModelBuilder modelBuilder)
        {
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

            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(e => e.RoleId);
                entity.Property(e => e.Name).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(200);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            });

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
        }

        private static void ConfigureBrandAndModules(ModelBuilder modelBuilder)
        {
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
        }

        // Plan 20 Faz A — OrgPosition (self-ref hierarchy)
        private static void ConfigureOrgChart(ModelBuilder modelBuilder)
        {
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
        }
    }
}
