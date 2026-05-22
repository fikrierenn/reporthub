using System.ComponentModel.DataAnnotations;

namespace Mosaik.Modules.SOP.Entities
{
    // Plan 34 §2.1 — SOP versiyonu Mosaik.Core ApprovalRequest'e bağlar.
    // ApprovalRequestId: Mosaik.Core.Workflow.ApprovalRequest FK (cross-modül, abstraction üzerinden).
    // SOP basic 3-step flow (Yazan → DepYön → IK). Plan 36 Workflow Designer ile interface uyumlu upgrade.
    public class SopApprovalSubmission
    {
        public int Id { get; set; }

        [Required]
        public int SopVersionId { get; set; }
        public SopVersion? SopVersion { get; set; }

        [Required]
        public int ApprovalRequestId { get; set; }                  // Mosaik.Core.Workflow.ApprovalRequest.Id (cross-assembly FK)

        [Required]
        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
