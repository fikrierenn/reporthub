using Microsoft.EntityFrameworkCore;

namespace Mosaik.Models
{
    // Plan 39 Faz C-1 — Holidays + ImportantDate + Intelligence EF config partial.
    // Plan 22 (Holiday/HolidayOccurrence/ImportantDate, ADR-016 Calendar Unified
    // Event Source) + Plan 38 (EntityRelation/DecisionLog, VISION §7 Living Org
    // Map + Decision Memory).
    public partial class MosaikContext
    {
        private static void ConfigureCompliance(ModelBuilder modelBuilder)
        {
            // Plan 22 — Holidays (ADR-016)
            modelBuilder.Entity<Holiday>(e =>
            {
                e.HasKey(h => h.Id);
                e.Property(h => h.Name).HasMaxLength(100).IsRequired();
                e.Property(h => h.Code).HasMaxLength(50);
                e.HasIndex(h => h.Code).IsUnique();
                e.Property(h => h.Type).HasConversion<string>().HasMaxLength(20);
                e.HasMany(h => h.Occurrences).WithOne(o => o.Holiday).HasForeignKey(o => o.HolidayId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<HolidayOccurrence>(e =>
            {
                e.HasKey(o => o.Id);
                e.HasIndex(o => new { o.HolidayId, o.Year }).IsUnique();
            });

            modelBuilder.Entity<ImportantDate>(e =>
            {
                e.HasKey(d => d.Id);
                e.Property(d => d.Title).HasMaxLength(200).IsRequired();
                e.Property(d => d.Notes).HasMaxLength(500);
                e.Property(d => d.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                e.HasOne(d => d.Firma).WithMany().HasForeignKey(d => d.FirmaId).OnDelete(DeleteBehavior.Restrict);
                e.HasIndex(d => new { d.FirmaId, d.EventDate });
            });

            // Plan 38 — Living Org Map + Decision Memory
            modelBuilder.Entity<Intelligence.EntityRelation>(e =>
            {
                e.HasKey(r => r.Id);
                e.Property(r => r.SourceType).HasMaxLength(50).IsRequired();
                e.Property(r => r.RelationType).HasMaxLength(50).IsRequired();
                e.Property(r => r.TargetType).HasMaxLength(50).IsRequired();
                e.Property(r => r.SourceSystem).HasMaxLength(50);
                e.Property(r => r.ValidFrom).HasDefaultValueSql("SYSUTCDATETIME()");
                e.Property(r => r.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
                e.HasIndex(r => new { r.FirmaId, r.SourceType, r.SourceId })
                 .HasDatabaseName("IX_EntityRelations_Source");
                e.HasIndex(r => new { r.FirmaId, r.TargetType, r.TargetId })
                 .HasDatabaseName("IX_EntityRelations_Target");
                e.HasIndex(r => new { r.FirmaId, r.SourceType, r.SourceId, r.RelationType, r.TargetType, r.TargetId })
                 .HasDatabaseName("UQ_EntityRelations_Tuple")
                 .IsUnique();
            });

            modelBuilder.Entity<Intelligence.DecisionLog>(e =>
            {
                e.HasKey(d => d.Id);
                e.Property(d => d.Title).HasMaxLength(300).IsRequired();
                e.Property(d => d.RelatedEntityType).HasMaxLength(50);
                e.Property(d => d.Status).HasMaxLength(20).IsRequired().HasDefaultValue("Active");
                e.Property(d => d.MadeAt).HasDefaultValueSql("SYSUTCDATETIME()");
                e.HasIndex(d => new { d.FirmaId, d.RelatedEntityType, d.RelatedEntityId })
                 .HasDatabaseName("IX_DecisionLogs_Entity");
                e.HasIndex(d => new { d.FirmaId, d.MadeBy, d.MadeAt })
                 .HasDatabaseName("IX_DecisionLogs_MadeBy");
            });
        }
    }
}
