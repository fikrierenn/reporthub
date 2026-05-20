namespace Mosaik.Core.Workflow
{
    // Plan 36 — entity detay sayfalari (Contracts/Tamim/Documents/Obligations vb.) icin
    // ortak workflow ozet sorgusu. Modul projeleri ana Mosaik projesine referans veremez,
    // bu yuzden interface Core'da; implementasyon ana proje (WorkflowInboxService).
    public interface IEntityWorkflowProvider
    {
        Task<List<EntityWorkflowSummary>> GetForEntityAsync(
            string entityType,
            int entityId,
            CancellationToken ct = default);
    }

    public class EntityWorkflowSummary
    {
        public int InstanceId { get; set; }
        public string TemplateName { get; set; } = string.Empty;
        public WorkflowInstanceStatus Status { get; set; }
        public string? CurrentStepName { get; set; }
        public string? CurrentStepAssigneeName { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string StartedByName { get; set; } = string.Empty;
        public int StepCount { get; set; }
        public int CompletedStepCount { get; set; }
    }
}
