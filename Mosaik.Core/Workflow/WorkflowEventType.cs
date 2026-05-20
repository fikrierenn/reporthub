namespace Mosaik.Core.Workflow
{
    // Plan 36 — WorkflowInstanceLogs append-only event sourcing event tipleri.
    // VISION §7: Friction Heatmap + Digital Twin what-if query'leri bu enum üzerinden çalışır.
    public static class WorkflowEventType
    {
        public const string InstanceStarted = "InstanceStarted";
        public const string StepEntered = "StepEntered";
        public const string StepCompleted = "StepCompleted";
        public const string StepRejected = "StepRejected";
        public const string StepReassigned = "StepReassigned";
        public const string EscalationFired = "EscalationFired";
        public const string InstanceCompleted = "InstanceCompleted";
        public const string InstanceCancelled = "InstanceCancelled";

        public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
        {
            InstanceStarted, StepEntered, StepCompleted, StepRejected,
            StepReassigned, EscalationFired, InstanceCompleted, InstanceCancelled
        };

        public static bool IsValid(string? type) => type is not null && All.Contains(type);
    }

    public enum WorkflowInstanceStatus
    {
        Active = 0,
        Completed = 1,
        Cancelled = 2
    }
}
