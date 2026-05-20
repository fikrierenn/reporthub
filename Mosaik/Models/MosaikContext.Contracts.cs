using Microsoft.EntityFrameworkCore;

namespace Mosaik.Models
{
    // Plan 39 Faz C-1 — Sözleşme modülü EF config partial (Plan 25).
    // Firma + Contract + ContractObligation/Recurrence/File/AiExtraction/Event +
    // AiSuggestion + ComplianceTemplate.
    // ADR-012: int PK, FirmaId zorunlu, multi-firma sınır.
    public partial class MosaikContext
    {
        private static void ConfigureContracts(ModelBuilder modelBuilder)
        {
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
                e.HasIndex(o => o.ReminderSentAt);
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

            modelBuilder.Entity<ContractEvent>(e =>
            {
                e.HasKey(ev => ev.Id);
                e.Property(ev => ev.Title).HasMaxLength(200).IsRequired();
                e.Property(ev => ev.Notes).HasMaxLength(500);
                e.Property(ev => ev.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                e.HasOne(ev => ev.Firma).WithMany().HasForeignKey(ev => ev.FirmaId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(ev => ev.Contract).WithMany().HasForeignKey(ev => ev.ContractId).OnDelete(DeleteBehavior.SetNull);
                e.HasOne(ev => ev.Obligation).WithMany().HasForeignKey(ev => ev.ObligationId).OnDelete(DeleteBehavior.SetNull);
                e.HasIndex(ev => new { ev.FirmaId, ev.EventDate });
                e.HasIndex(ev => ev.Status);
            });

            modelBuilder.Entity<AiSuggestion>(e =>
            {
                e.HasKey(s => s.Id);
                e.Property(s => s.Title).HasMaxLength(200).IsRequired();
                e.Property(s => s.Description).HasMaxLength(2000);
                e.Property(s => s.ApprovedBy).HasMaxLength(100);
                e.Property(s => s.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                e.HasOne(s => s.Firma).WithMany().HasForeignKey(s => s.FirmaId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(s => s.Extraction).WithMany().HasForeignKey(s => s.ExtractionId).OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(s => new { s.ExtractionId, s.Status });
            });

            modelBuilder.Entity<ComplianceTemplate>(e =>
            {
                e.HasKey(t => t.Id);
                e.Property(t => t.PackageName).HasMaxLength(100).IsRequired();
                e.Property(t => t.Title).HasMaxLength(200).IsRequired();
                e.Property(t => t.Description).HasMaxLength(500);
                e.Property(t => t.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                e.HasIndex(t => t.PackageName);
                e.HasIndex(t => t.IsActive);
            });
        }
    }
}
