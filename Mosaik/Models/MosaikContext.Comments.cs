using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Comments;

namespace Mosaik.Models
{
    // Plan 54 M5 — polymorphic yorum EF config.
    public partial class MosaikContext
    {
        private static void ConfigureComments(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Comment>(entity =>
            {
                entity.ToTable("Comments");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.EntityType).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Body).HasMaxLength(4000).IsRequired();
                entity.Property(e => e.AuthorName).HasMaxLength(150).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

                // Varlık-bazlı yorum çekimi (firma sınırı dahil).
                entity.HasIndex(e => new { e.FirmaId, e.EntityType, e.EntityId });
            });
        }
    }
}
