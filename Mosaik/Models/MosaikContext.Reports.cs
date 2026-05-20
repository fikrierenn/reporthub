using Microsoft.EntityFrameworkCore;

namespace Mosaik.Models
{
    // Plan 39 Faz C-1 — Reports modülü EF config partial.
    // DataSource + ReportCatalog + ReportRunLog + ReportGroup/Link/AllowedRole/Favorite
    // + UserDataFilter + FilterDefinition.
    public partial class MosaikContext
    {
        private static void ConfigureReports(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DataSource>(entity =>
            {
                entity.HasKey(e => e.DataSourceKey);
                entity.Property(e => e.DataSourceKey).HasMaxLength(50);
                entity.Property(e => e.Title).HasMaxLength(100).IsRequired();
                entity.Property(e => e.ConnString).HasMaxLength(1000).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            });

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

            // ReportGroups (Plan 07 son rename: ReportCategories → ReportGroups)
            modelBuilder.Entity<ReportGroup>(entity =>
            {
                entity.HasKey(e => e.GroupId);
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(300);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            });

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

            // FilterDefinition (Plan 07 Faz 2 — master tablo, Migration 20)
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
        }
    }
}
