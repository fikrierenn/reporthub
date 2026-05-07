using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Mosaik.Core.Domain;

namespace Mosaik.Core.Workflow
{
    public enum ApprovalStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2,
        Cancelled = 3
    }

    // YonetIQ port — generic onay isteği. EntityType+EntityId ile herhangi bir
    // entity'yi onay sürecine bağlar (Tamim/Doküman/Sözleşme/Form).
    public class ApprovalRequest : BaseEntity, IAuditable
    {
        [Key]
        [BindNever]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string EntityType { get; set; } = string.Empty;

        public int EntityId { get; set; }

        [MaxLength(200)]
        public string? Subject { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

        public DateTime? CompletedAt { get; set; }

        public List<ApprovalStep> Steps { get; set; } = new();
    }

    // Sıralı onay adımı — StepOrder bazlı. MIN(StepOrder) sıradaki onayçı.
    public class ApprovalStep : BaseEntity, IAuditable
    {
        [Key]
        [BindNever]
        public int Id { get; set; }

        public int RequestId { get; set; }
        public ApprovalRequest? Request { get; set; }

        public int StepOrder { get; set; }

        public int? ApproverUserId { get; set; }
        public string? ApproverRole { get; set; }

        public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

        public DateTime? DecidedAt { get; set; }

        [MaxLength(500)]
        public string? Comment { get; set; }
    }
}
