using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Domain;
using Mosaik.Core.Intelligence;
using Mosaik.Core.Workflow;
using Mosaik.Models;
using Mosaik.Models.Workflow;

namespace Mosaik.Services.Workflow
{
    // Plan 36 Faz A — IWorkflowService implementasyonu.
    // Append-only event sourcing: state değişimi yeni WorkflowInstanceLog satırı.
    // CurrentStepId projeksiyon güncellenir (cache amaçlı, log otorite).
    //
    // Plan 38 §8.1 çift-yazma: Approve/Reject sonrası DecisionLog + EntityRelations
    // paralel kayıt. Eski FK (WorkflowInstanceLogs) yerinde, yeni okuma katmanı
    // (DecisionLog by-entity, EntityRelations by-source/target) ek değer.
    public class WorkflowEngine : IWorkflowService
    {
        private readonly MosaikContext _context;
        private readonly IDecisionLogService _decisionLog;
        private readonly IEntityRelationService _entityRelations;
        private readonly WorkflowNotifier? _notifier;
        private readonly ILogger<WorkflowEngine> _logger;

        public WorkflowEngine(
            MosaikContext context,
            IDecisionLogService decisionLog,
            IEntityRelationService entityRelations,
            ILogger<WorkflowEngine> logger,
            WorkflowNotifier? notifier = null)
        {
            _context = context;
            _decisionLog = decisionLog;
            _entityRelations = entityRelations;
            _notifier = notifier;
            _logger = logger;
        }

        public async Task<ServiceResult<int>> StartAsync(WorkflowStartInput input, CancellationToken ct = default)
        {
            var template = await _context.WorkflowTemplates
                .FirstOrDefaultAsync(t => t.Id == input.TemplateId && t.FirmaId == input.FirmaId && t.IsActive, ct);
            if (template is null)
                return ServiceResult<int>.Failure("Aktif workflow şablonu bulunamadı.", "TEMPLATE_NOT_FOUND");

            var definition = WorkflowDefinition.Parse(template.DefinitionJson);
            if (definition is null || definition.Steps.Count == 0)
                return ServiceResult<int>.Failure("Şablon en az 1 adım içermeli.", "EMPTY_DEFINITION");

            var firstStep = definition.Steps[0];

            var instance = new WorkflowInstance
            {
                FirmaId = input.FirmaId,
                TemplateId = input.TemplateId,
                EntityType = input.EntityType,
                EntityId = input.EntityId,
                CurrentStepId = firstStep.Id,
                Status = WorkflowInstanceStatus.Active,
                StartedAt = DateTime.UtcNow,
                StartedBy = input.StartedBy,
                PayloadJson = input.PayloadJson
            };
            _context.WorkflowInstances.Add(instance);
            await _context.SaveChangesAsync(ct);

            _context.WorkflowInstanceLogs.Add(NewLog(instance.Id, null, WorkflowEventType.InstanceStarted, input.StartedBy, input.PayloadJson));
            _context.WorkflowInstanceLogs.Add(NewLog(instance.Id, firstStep.Id, WorkflowEventType.StepEntered, input.StartedBy));
            await _context.SaveChangesAsync(ct);

            await NotifyStepEnteredSafeAsync(instance.Id, firstStep.Id, ct);
            return ServiceResult<int>.Ok(instance.Id, "Workflow başlatıldı.");
        }

        public async Task<ServiceResult> AdvanceAsync(int instanceId, WorkflowAdvanceInput input, CancellationToken ct = default)
        {
            var instance = await _context.WorkflowInstances
                .Include(i => i.Template)
                .FirstOrDefaultAsync(i => i.Id == instanceId, ct);
            if (instance is null) return ServiceResult.Failure("Workflow instance bulunamadı.", "NOT_FOUND");
            if (instance.Status != WorkflowInstanceStatus.Active)
                return ServiceResult.Failure("Workflow zaten kapalı.", "NOT_ACTIVE");
            if (string.IsNullOrEmpty(instance.CurrentStepId))
                return ServiceResult.Failure("Geçerli adım yok.", "NO_CURRENT_STEP");

            var definition = WorkflowDefinition.Parse(instance.Template?.DefinitionJson ?? string.Empty);
            if (definition is null || definition.Steps.Count == 0)
                return ServiceResult.Failure("Şablon bozuk.", "BROKEN_DEFINITION");

            var currentIndex = definition.Steps.FindIndex(s => s.Id == instance.CurrentStepId);
            if (currentIndex < 0)
                return ServiceResult.Failure("Mevcut adım şablonda bulunamadı.", "STEP_NOT_IN_TEMPLATE");

            var completedEvent = input.Approved ? WorkflowEventType.StepCompleted : WorkflowEventType.StepRejected;
            var currentStepName = definition.Steps[currentIndex].Name ?? instance.CurrentStepId;
            _context.WorkflowInstanceLogs.Add(NewLog(instance.Id, instance.CurrentStepId, completedEvent, input.ActorId, BuildPayload(input)));

            if (!input.Approved)
            {
                instance.Status = WorkflowInstanceStatus.Cancelled;
                instance.CompletedAt = DateTime.UtcNow;
                _context.WorkflowInstanceLogs.Add(NewLog(instance.Id, null, WorkflowEventType.InstanceCancelled, input.ActorId, input.Comment));
                await _context.SaveChangesAsync(ct);
                await WriteDecisionAndRelationAsync(instance, input, currentStepName, approved: false, ct);
                return ServiceResult.Ok("Workflow reddedildi.");
            }

            await WriteDecisionAndRelationAsync(instance, input, currentStepName, approved: true, ct);

            var nextIndex = currentIndex + 1;
            if (nextIndex >= definition.Steps.Count)
            {
                instance.Status = WorkflowInstanceStatus.Completed;
                instance.CurrentStepId = null;
                instance.CompletedAt = DateTime.UtcNow;
                _context.WorkflowInstanceLogs.Add(NewLog(instance.Id, null, WorkflowEventType.InstanceCompleted, input.ActorId));
                await _context.SaveChangesAsync(ct);
                return ServiceResult.Ok("Workflow tamamlandı.");
            }

            var nextStep = definition.Steps[nextIndex];
            instance.CurrentStepId = nextStep.Id;
            _context.WorkflowInstanceLogs.Add(NewLog(instance.Id, nextStep.Id, WorkflowEventType.StepEntered, input.ActorId));
            await _context.SaveChangesAsync(ct);
            await NotifyStepEnteredSafeAsync(instance.Id, nextStep.Id, ct);
            return ServiceResult.Ok($"Adım ilerletildi: {nextStep.Name ?? nextStep.Id}");
        }

        private async Task NotifyStepEnteredSafeAsync(int instanceId, string stepId, CancellationToken ct)
        {
            if (_notifier is null) return;
            try
            {
                await _notifier.NotifyStepEnteredAsync(instanceId, stepId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "WorkflowEngine: StepEntered bildirimi gönderilemedi. InstanceId={Id} StepId={StepId}",
                    instanceId, stepId);
            }
        }

        // Plan 38 §8.1 — Approve/Reject sonrası DecisionLog + EntityRelations paralel yazıcı.
        // Hata atılırsa workflow ana akışı kırılmaz — log + devam (eski FK = WorkflowInstanceLogs canonical).
        private async Task WriteDecisionAndRelationAsync(
            WorkflowInstance instance,
            WorkflowAdvanceInput input,
            string stepLabel,
            bool approved,
            CancellationToken ct)
        {
            try
            {
                var decision = new DecisionLogEntry(
                    FirmaId: instance.FirmaId,
                    Title: $"Workflow [{stepLabel}] {(approved ? "onaylandı" : "reddedildi")}",
                    MadeBy: input.ActorId,
                    Rationale: input.Comment,
                    RelatedEntityType: EntityType.WorkflowInstance,
                    RelatedEntityId: instance.Id);
                await _decisionLog.LogAsync(decision, ct);

                var relation = new EntityRelationInput(
                    FirmaId: instance.FirmaId,
                    SourceType: EntityType.User,
                    SourceId: input.ActorId,
                    RelationType: approved ? RelationType.Approved : RelationType.Rejected,
                    TargetType: EntityType.WorkflowInstance,
                    TargetId: instance.Id,
                    CreatedBy: input.ActorId);
                await _entityRelations.AddAsync(relation, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "WorkflowEngine: DecisionLog/EntityRelations çift-yazma başarısız. InstanceId={Id} Actor={Actor}",
                    instance.Id, input.ActorId);
            }
        }

        public async Task<ServiceResult> CancelAsync(int instanceId, int actorId, string? reason = null, CancellationToken ct = default)
        {
            var instance = await _context.WorkflowInstances.FirstOrDefaultAsync(i => i.Id == instanceId, ct);
            if (instance is null) return ServiceResult.Failure("Workflow instance bulunamadı.", "NOT_FOUND");
            if (instance.Status != WorkflowInstanceStatus.Active)
                return ServiceResult.Failure("Workflow zaten kapalı.", "NOT_ACTIVE");

            instance.Status = WorkflowInstanceStatus.Cancelled;
            instance.CompletedAt = DateTime.UtcNow;
            _context.WorkflowInstanceLogs.Add(NewLog(instance.Id, null, WorkflowEventType.InstanceCancelled, actorId, reason));
            await _context.SaveChangesAsync(ct);
            return ServiceResult.Ok("Workflow iptal edildi.");
        }

        public async Task<IReadOnlyList<WorkflowInstanceLogDto>> GetLogsAsync(int instanceId, CancellationToken ct = default)
        {
            return await _context.WorkflowInstanceLogs.AsNoTracking()
                .Where(l => l.InstanceId == instanceId)
                .OrderBy(l => l.OccurredAt)
                .Select(l => new WorkflowInstanceLogDto(
                    l.Id, l.InstanceId, l.StepId, l.EventType, l.ActorId, l.OccurredAt, l.PayloadJson))
                .ToListAsync(ct);
        }

        private static WorkflowInstanceLog NewLog(int instanceId, string? stepId, string eventType, int? actorId, string? payload = null)
            => new()
            {
                InstanceId = instanceId,
                StepId = stepId,
                EventType = eventType,
                ActorId = actorId,
                OccurredAt = DateTime.UtcNow,
                PayloadJson = payload
            };

        private static string? BuildPayload(WorkflowAdvanceInput input)
        {
            if (string.IsNullOrEmpty(input.Comment) && string.IsNullOrEmpty(input.PayloadJson))
                return null;
            return System.Text.Json.JsonSerializer.Serialize(new
            {
                approved = input.Approved,
                comment = input.Comment,
                payload = input.PayloadJson
            });
        }
    }
}
