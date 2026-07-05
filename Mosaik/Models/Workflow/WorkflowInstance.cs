using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Mosaik.Core.Workflow;

namespace Mosaik.Models.Workflow
{
    // Plan 36 — template'dan başlatılan iş.
    // CurrentStepId = WorkflowInstanceLogs projeksiyon (latest StepEntered).
    [Table("WorkflowInstances")]
    public class WorkflowInstance
    {
        [Key]
        [BindNever]
        public int Id { get; set; }

        public int FirmaId { get; set; }

        public int TemplateId { get; set; }

        public WorkflowTemplate? Template { get; set; }

        [Required]
        [MaxLength(50)]
        public string EntityType { get; set; } = string.Empty;

        public int EntityId { get; set; }

        [MaxLength(100)]
        public string? CurrentStepId { get; set; }

        public WorkflowInstanceStatus Status { get; set; } = WorkflowInstanceStatus.Active;

        [BindNever]
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        public int StartedBy { get; set; }

        public DateTime? CompletedAt { get; set; }

        public string? PayloadJson { get; set; }

        // Plan 57 Part C — StartAsync anında çözülen assigneeKind:"manager" atamaları
        // {"s1":42}. Instance-başına DONDURULUR (audit: onay başladığındaki amir karar verir;
        // org sonradan değişse bile). Şablon DefinitionJson paylaşımlı → oraya yazılamaz;
        // PayloadJson caller-payload → çakışmasın diye ayrı kolon.
        // [BindNever]: yetki taşıyan alan — ileride WorkflowInstance bind edilirse
        // self-approve injection kapısı olmasın (security M-2, mass-assignment checklist).
        [BindNever]
        [MaxLength(400)]
        public string? ResolvedAssigneesJson { get; set; }
    }
}
