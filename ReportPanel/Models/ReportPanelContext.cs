using Microsoft.EntityFrameworkCore;

namespace ReportPanel.Models
{
    public class ReportPanelContext : DbContext
    {
        public ReportPanelContext(DbContextOptions<ReportPanelContext> options) : base(options)
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
        public DbSet<ReportCategory> ReportCategories { get; set; }
        public DbSet<ReportCategoryLink> ReportCategoryLinks { get; set; }
        public DbSet<ReportAllowedRole> ReportAllowedRoles { get; set; }
        public DbSet<UserDataFilter> UserDataFilters { get; set; }
        public DbSet<FilterDefinition> FilterDefinitions { get; set; }
        public DbSet<Sube> Subeler { get; set; }
        public DbSet<SubeMapping> SubeMappings { get; set; }

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

            // ReportCategories configuration
            modelBuilder.Entity<ReportCategory>(entity =>
            {
                entity.HasKey(e => e.CategoryId);
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(300);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            });

            // ReportCategoryLinks configuration
            modelBuilder.Entity<ReportCategoryLink>(entity =>
            {
                entity.HasKey(e => new { e.ReportId, e.CategoryId });
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.Report)
                    .WithMany(r => r.ReportCategories)
                    .HasForeignKey(e => e.ReportId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Category)
                    .WithMany()
                    .HasForeignKey(e => e.CategoryId)
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

            // Sube canonical master (Plan 07 Faz 5b — Migration 21)
            modelBuilder.Entity<Sube>(entity =>
            {
                entity.ToTable("Sube");
                entity.HasKey(e => e.SubeId);
                entity.Property(e => e.SubeAd).HasMaxLength(100).IsRequired();
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.DisplayOrder).HasDefaultValue(0);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
                entity.HasIndex(e => e.SubeAd).IsUnique();
            });

            // SubeMapping per-DataSource external code (Plan 07 Faz 5b — Migration 21)
            modelBuilder.Entity<SubeMapping>(entity =>
            {
                entity.ToTable("SubeMapping");
                entity.HasKey(e => e.MappingId);
                entity.Property(e => e.DataSourceKey).HasMaxLength(50).IsRequired();
                entity.Property(e => e.ExternalCode).HasMaxLength(50).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

                entity.HasIndex(e => new { e.SubeId, e.DataSourceKey }).IsUnique();
                entity.HasIndex(e => new { e.DataSourceKey, e.ExternalCode }).IsUnique();

                entity.HasOne(e => e.Sube)
                    .WithMany()
                    .HasForeignKey(e => e.SubeId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.DataSource)
                    .WithMany()
                    .HasForeignKey(e => e.DataSourceKey)
                    .OnDelete(DeleteBehavior.Restrict);
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

                entity.HasIndex(e => e.FilterKey).IsUnique();

                entity.HasOne(e => e.DataSource)
                    .WithMany()
                    .HasForeignKey(e => e.DataSourceKey)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
