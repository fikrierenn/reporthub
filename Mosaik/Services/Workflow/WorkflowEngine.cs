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
        private readonly IManagerResolver? _managerResolver; // Plan 57 Part C — opsiyonel (testler resolver'sız)
        private readonly ILogger<WorkflowEngine> _logger;

        public WorkflowEngine(
            MosaikContext context,
            IDecisionLogService decisionLog,
            IEntityRelationService entityRelations,
            ILogger<WorkflowEngine> logger,
            WorkflowNotifier? notifier = null,
            IManagerResolver? managerResolver = null)
        {
            _context = context;
            _decisionLog = decisionLog;
            _entityRelations = entityRelations;
            _notifier = notifier;
            _managerResolver = managerResolver;
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

            // Plan 57 Part C — assigneeKind:"manager" step'leri BAŞLANGIÇTA bir kez çöz + dondur
            // (audit tutarlılığı: onay başladığındaki amir karar verir; org sonradan değişse bile).
            // Şablon DefinitionJson paylaşımlı → instance-scoped ResolvedAssigneesJson'a yazılır.
            // Çözülemezse null bırakılır → IsAssignedToUser şablondaki assigneeRole fallback'ine
            // düşer (fail-closed: onaysız geçiş yok) + ManagerResolutionFailed event log'lanır.
            string? resolvedJson = null;
            var managerSteps = definition.Steps
                .Where(s => s.Properties is not null
                         && s.Properties.TryGetValue("assigneeKind", out var kind)
                         && kind.ValueKind == System.Text.Json.JsonValueKind.String
                         && kind.GetString() == "manager")
                .ToList();
            var resolutionFailures = new List<string>();
            if (managerSteps.Count > 0 && _managerResolver is not null)
            {
                var managerUserId = await _managerResolver.ResolveManagerUserIdAsync(input.StartedBy, ct);
                if (managerUserId is int muid)
                {
                    var map = managerSteps.ToDictionary(s => s.Id, _ => muid);
                    resolvedJson = System.Text.Json.JsonSerializer.Serialize(map);
                }
                else
                {
                    resolutionFailures.AddRange(managerSteps.Select(s => s.Id));
                    _logger.LogWarning("Workflow {TemplateId}: amir çözülemedi (StartedBy={UserId}) — {Steps} şablon assigneeRole fallback'ine düşecek.",
                        input.TemplateId, input.StartedBy, string.Join(',', resolutionFailures));
                }
            }
            else if (managerSteps.Count > 0)
            {
                // H-2: resolver DI'da YOK = yapılandırma hatası — veri sorunundan (yukarıdaki dal)
                // DAHA alarmlı; sessiz kalamaz. Fallback yine çalışır ama iz bırakılır.
                resolutionFailures.AddRange(managerSteps.Select(s => s.Id));
                _logger.LogError("Workflow {TemplateId}: IManagerResolver kayıtlı DEĞİL — {Steps} manager step'i assigneeRole fallback'ine düştü (DI yapılandırması kontrol edin).",
                    input.TemplateId, string.Join(',', resolutionFailures));
            }

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
                PayloadJson = input.PayloadJson,
                ResolvedAssigneesJson = resolvedJson
            };
            _context.WorkflowInstances.Add(instance);
            await _context.SaveChangesAsync(ct);

            _context.WorkflowInstanceLogs.Add(NewLog(instance.Id, null, WorkflowEventType.InstanceStarted, input.StartedBy, input.PayloadJson));
            _context.WorkflowInstanceLogs.Add(NewLog(instance.Id, firstStep.Id, WorkflowEventType.StepEntered, input.StartedBy));
            // Part C — çözüm sonucu event-stream'e (denetim izi: kim/neden atandı ya da neden fallback).
            if (resolvedJson is not null)
                _context.WorkflowInstanceLogs.Add(NewLog(instance.Id, null, "ManagerResolved", input.StartedBy, resolvedJson));
            foreach (var failedStep in resolutionFailures)
                _context.WorkflowInstanceLogs.Add(NewLog(instance.Id, failedStep, "ManagerResolutionFailed", input.StartedBy));
            await _context.SaveChangesAsync(ct);

            await NotifyStepEnteredSafeAsync(instance.Id, firstStep.Id, ct);
            await AutoAdvanceIfNeededAsync(instance.Id, input.StartedBy, ct);
            return ServiceResult<int>.Ok(instance.Id, "Workflow başlatıldı.");
        }

        // Notify step: bildir + otomatik next. Delay step: log + WorkflowStepProcessor zamanı geldiğinde geçirir.
        private async Task AutoAdvanceIfNeededAsync(int instanceId, int actorId, CancellationToken ct)
        {
            var instance = await _context.WorkflowInstances
                .Include(i => i.Template)
                .FirstOrDefaultAsync(i => i.Id == instanceId, ct);
            if (instance is null || instance.Template is null) return;
            if (instance.Status != WorkflowInstanceStatus.Active) return;
            if (string.IsNullOrEmpty(instance.CurrentStepId)) return;

            var definition = WorkflowDefinition.Parse(instance.Template.DefinitionJson);
            var step = definition?.Steps.FirstOrDefault(s => s.Id == instance.CurrentStepId);
            if (step is null) return;

            var kind = (step.Type ?? WorkflowStepKind.Approval).ToLowerInvariant();
            if (!WorkflowStepKind.IsAuto(kind)) return; // approval — kullanıcı bekler

            if (kind == WorkflowStepKind.Notify)
            {
                // Bildirim notifier'da zaten atıldı (NotifyStepEnteredSafeAsync). Direkt complete + next.
                await AdvanceProgrammaticAsync(instance, definition!, actorId, "auto-notify", ct);
            }
            // delay → log "DelayScheduled" (idempotent), processor handle eder
            else if (kind == WorkflowStepKind.Delay)
            {
                var alreadyScheduled = await _context.WorkflowInstanceLogs.AsNoTracking()
                    .AnyAsync(l => l.InstanceId == instance.Id
                                && l.StepId == instance.CurrentStepId
                                && l.EventType == "DelayScheduled", ct);
                if (!alreadyScheduled)
                {
                    var waitDays = TryGetInt(step, "waitDays", 1);
                    _context.WorkflowInstanceLogs.Add(NewLog(instance.Id, instance.CurrentStepId, "DelayScheduled", null,
                        System.Text.Json.JsonSerializer.Serialize(new { waitDays })));
                    await _context.SaveChangesAsync(ct);
                }
            }
        }

        // Programatik advance — notify/delay step otomatik geçişi için (kullanıcı kararı YOK).
        // Engine logic'i AdvanceAsync ile aynı ama Approve/Reject yerine "auto-complete".
        private async Task AdvanceProgrammaticAsync(WorkflowInstance instance, WorkflowDefinition definition, int actorId, string reason, CancellationToken ct)
        {
            var currentIndex = definition.Steps.FindIndex(s => s.Id == instance.CurrentStepId);
            if (currentIndex < 0) return;

            _context.WorkflowInstanceLogs.Add(NewLog(instance.Id, instance.CurrentStepId,
                WorkflowEventType.StepCompleted, actorId,
                System.Text.Json.JsonSerializer.Serialize(new { auto = true, reason })));

            var nextIndex = currentIndex + 1;
            if (nextIndex >= definition.Steps.Count)
            {
                instance.Status = WorkflowInstanceStatus.Completed;
                instance.CurrentStepId = null;
                instance.CompletedAt = DateTime.UtcNow;
                _context.WorkflowInstanceLogs.Add(NewLog(instance.Id, null, WorkflowEventType.InstanceCompleted, actorId));
                await _context.SaveChangesAsync(ct);
                return;
            }

            var nextStep = definition.Steps[nextIndex];
            instance.CurrentStepId = nextStep.Id;
            _context.WorkflowInstanceLogs.Add(NewLog(instance.Id, nextStep.Id, WorkflowEventType.StepEntered, actorId));
            await _context.SaveChangesAsync(ct);

            await NotifyStepEnteredSafeAsync(instance.Id, nextStep.Id, ct);
            // Recursive — bir sonraki adım da auto ise zincir devam eder.
            await AutoAdvanceIfNeededAsync(instance.Id, actorId, ct);
        }

        private static int TryGetInt(WorkflowDefinitionStep step, string key, int defaultValue)
        {
            if (step.Properties is null) return defaultValue;
            if (!step.Properties.TryGetValue(key, out var el)) return defaultValue;
            if (el.ValueKind == System.Text.Json.JsonValueKind.Number && el.TryGetInt32(out var n)) return n;
            if (el.ValueKind == System.Text.Json.JsonValueKind.String && int.TryParse(el.GetString(), out var s)) return s;
            return defaultValue;
        }

        public async Task<int> TickDelayedStepsAsync(CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var active = await _context.WorkflowInstances
                .Where(i => i.Status == WorkflowInstanceStatus.Active && i.CurrentStepId != null)
                .Include(i => i.Template)
                .ToListAsync(ct);

            int advanced = 0;
            foreach (var instance in active)
            {
                try
                {
                    if (instance.Template is null) continue;
                    var definition = WorkflowDefinition.Parse(instance.Template.DefinitionJson);
                    var step = definition?.Steps.FirstOrDefault(s => s.Id == instance.CurrentStepId);
                    if (step is null) continue;
                    var kind = (step.Type ?? WorkflowStepKind.Approval).ToLowerInvariant();
                    if (kind != WorkflowStepKind.Delay) continue;

                    var waitDays = TryGetInt(step, "waitDays", 1);
                    if (waitDays <= 0) continue;

                    var stepEnteredAt = await _context.WorkflowInstanceLogs.AsNoTracking()
                        .Where(l => l.InstanceId == instance.Id
                                 && l.EventType == WorkflowEventType.StepEntered
                                 && l.StepId == instance.CurrentStepId)
                        .OrderByDescending(l => l.OccurredAt)
                        .Select(l => (DateTime?)l.OccurredAt)
                        .FirstOrDefaultAsync(ct);
                    if (stepEnteredAt is null) continue;

                    var due = stepEnteredAt.Value.AddDays(waitDays);
                    if (now < due) continue;

                    await AdvanceProgrammaticAsync(instance, definition!, actorId: 0, "delay-expired", ct);
                    advanced++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "WorkflowEngine.TickDelayedSteps: InstanceId={Id} hata.", instance.Id);
                }
            }

            return advanced;
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
            await AutoAdvanceIfNeededAsync(instance.Id, input.ActorId, ct);
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
