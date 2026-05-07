namespace Mosaik.Core.Workflow
{
    // Generic state machine — Tamim/Approval/DOF/Form ortak.
    // Implementasyonlar geçerli geçişleri tanımlar.
    public interface IWorkflow<TStatus> where TStatus : struct, Enum
    {
        TStatus Status { get; }
        bool CanTransitionTo(TStatus target, IEnumerable<string> userRoles);
    }

    // DB-driven izinli geçişler (Operax StatusTransition pattern).
    // EntityType + FromStatus + ToStatus + AllowedRoles (CSV).
    public class StatusTransition
    {
        public int Id { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public string FromStatus { get; set; } = string.Empty;
        public string ToStatus { get; set; } = string.Empty;
        public string AllowedRoles { get; set; } = string.Empty; // CSV
        public bool IsActive { get; set; } = true;
    }
}
