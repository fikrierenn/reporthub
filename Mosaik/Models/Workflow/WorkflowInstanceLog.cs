using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Mosaik.Models.Workflow
{
    // Plan 36 — append-only event sourcing.
    // VISION §7 Friction Heatmap + Digital Twin altyapısı.
    // UPDATE YASAK — her state değişimi yeni satır.
    [Table("WorkflowInstanceLogs")]
    public class WorkflowInstanceLog
    {
        [Key]
        [BindNever]
        public long Id { get; set; }

        public int InstanceId { get; set; }

        [MaxLength(100)]
        public string? StepId { get; set; }

        [Required]
        [MaxLength(50)]
        public string EventType { get; set; } = string.Empty;

        public int? ActorId { get; set; }

        [BindNever]
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

        public string? PayloadJson { get; set; }
    }
}
