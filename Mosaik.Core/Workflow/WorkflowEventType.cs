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

    // Plan 36 — designer step type'ları.
    // approval: kullanıcı kararı bekler (Approve/Reject)
    // notify: sadece bildirim atar, otomatik next step'e geçer
    // delay: N gün bekler, sonra otomatik next step'e geçer (WorkflowStepProcessor handle eder)
    public static class WorkflowStepKind
    {
        public const string Approval = "approval";
        public const string Notify = "notify";
        public const string Delay = "delay";

        public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
        {
            Approval, Notify, Delay
        };

        public static bool IsAuto(string? kind) =>
            kind == Notify || kind == Delay;
    }
}
