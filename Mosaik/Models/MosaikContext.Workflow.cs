using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Lookup;
using Mosaik.Core.Workflow;

namespace Mosaik.Models
{
    // Plan 39 Faz C-1 — Workflow + Lookup EF config partial.
    // Plan 16.5 Faz A approval primitives + Plan 16.5 Faz B dictionary lookup +
    // Plan 36 WorkflowTemplate/Instance/Log.
    public partial class MosaikContext
    {
        private static void ConfigureWorkflowAndLookup(ModelBuilder modelBuilder)
        {
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

            // Plan 36 — Workflow Designer + onay akışları (event sourcing)
            modelBuilder.Entity<Workflow.WorkflowTemplate>(e =>
            {
                e.HasKey(t => t.Id);
                e.Property(t => t.Name).HasMaxLength(200).IsRequired();
                e.Property(t => t.EntityType).HasMaxLength(50).IsRequired();
                e.Property(t => t.DefinitionJson).IsRequired();
                e.Property(t => t.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
                e.HasIndex(t => new { t.FirmaId, t.EntityType, t.IsActive })
                 .HasDatabaseName("IX_WorkflowTemplates_EntityType");
            });

            modelBuilder.Entity<Workflow.WorkflowInstance>(e =>
            {
                e.HasKey(i => i.Id);
                e.Property(i => i.EntityType).HasMaxLength(50).IsRequired();
                e.Property(i => i.CurrentStepId).HasMaxLength(100);
                e.Property(i => i.StartedAt).HasDefaultValueSql("SYSUTCDATETIME()");
                e.HasOne(i => i.Template).WithMany().HasForeignKey(i => i.TemplateId).OnDelete(DeleteBehavior.Restrict);
                e.HasIndex(i => new { i.FirmaId, i.EntityType, i.EntityId })
                 .HasDatabaseName("IX_WorkflowInstances_Entity");
                e.HasIndex(i => new { i.FirmaId, i.Status })
                 .HasDatabaseName("IX_WorkflowInstances_Active");
            });

            modelBuilder.Entity<Workflow.WorkflowInstanceLog>(e =>
            {
                e.HasKey(l => l.Id);
                e.Property(l => l.StepId).HasMaxLength(100);
                e.Property(l => l.EventType).HasMaxLength(50).IsRequired();
                e.Property(l => l.OccurredAt).HasDefaultValueSql("SYSUTCDATETIME()");
                e.HasIndex(l => new { l.InstanceId, l.OccurredAt })
                 .HasDatabaseName("IX_WorkflowInstanceLogs_Instance");
                e.HasIndex(l => new { l.StepId, l.EventType })
                 .HasDatabaseName("IX_WorkflowInstanceLogs_StepEvent");
            });
        }
    }
}
