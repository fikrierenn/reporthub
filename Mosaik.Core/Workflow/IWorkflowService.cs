using Mosaik.Core.Domain;

namespace Mosaik.Core.Workflow
{
    // Plan 36 Faz A — template-driven workflow engine.
    // ApprovalRequest (Plan 16.5) basit sıralı onay; bu sistem template + designer + event sourcing.
    public interface IWorkflowService
    {
        Task<ServiceResult<int>> StartAsync(WorkflowStartInput input, CancellationToken ct = default);

        Task<ServiceResult> AdvanceAsync(int instanceId, WorkflowAdvanceInput input, CancellationToken ct = default);

        Task<ServiceResult> CancelAsync(int instanceId, int actorId, string? reason = null, CancellationToken ct = default);

        Task<IReadOnlyList<WorkflowInstanceLogDto>> GetLogsAsync(int instanceId, CancellationToken ct = default);
    }

    public sealed record WorkflowStartInput(
        int FirmaId,
        int TemplateId,
        string EntityType,
        int EntityId,
        int StartedBy,
        string? PayloadJson = null);

    public sealed record WorkflowAdvanceInput(
        int ActorId,
        bool Approved,
        string? Comment = null,
        string? PayloadJson = null);

    public sealed record WorkflowInstanceLogDto(
        long Id,
        int InstanceId,
        string? StepId,
        string EventType,
        int? ActorId,
        DateTime OccurredAt,
        string? PayloadJson);
}
