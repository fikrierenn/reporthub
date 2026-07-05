using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Mosaik.Modules.ProcessRuntime.Entities
{
    // Plan 42 REV 3 #7 — aspect = YÜRÜTME değil KORELASYON: bu vakaya hangi form/workflow/
    // doküman/karar ait. Polymorphic (AspectType + AspectId soft-ref). EntityRelations'a da
    // mirror edilir (Plan 38 çifte-görünürlük). Timeline = WorkflowInstanceLogs + bu tablo READ.
    public class ProcessInstanceAspect
    {
        [Key]
        [BindNever]
        public int Id { get; set; }

        [Required]
        public int ProcessInstanceId { get; set; }
        public ProcessInstance? ProcessInstance { get; set; }

        // 0 Form(FormSubmission) 1 Workflow(WorkflowInstance) 2 Document(ContractFile)
        // 3 Decision(DecisionLog) 4 Audit 5 KvkkContext 6 Comment 7 SubInstance(ProcessInstance)
        public byte AspectType { get; set; }

        public int AspectId { get; set; }         // polymorphic soft-ref (FK yok — modüller arası)

        [MaxLength(80)]
        public string? RelationLabel { get; set; } // "ilk başvuru", "onay kararı", "sonuç belgesi"

        [BindNever]
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        public int? AddedBy { get; set; }
    }

    public static class ProcessAspectType
    {
        public const byte Form = 0;
        public const byte Workflow = 1;
        public const byte Document = 2;
        public const byte Decision = 3;
        public const byte Audit = 4;
        public const byte KvkkContext = 5;
        public const byte Comment = 6;
        public const byte SubInstance = 7;
    }
}
