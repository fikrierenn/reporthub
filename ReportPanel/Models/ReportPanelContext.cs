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
                entity.Property(e => e.Roles).HasMaxLength(200).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()");
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
