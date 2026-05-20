using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Notification;
using Mosaik.Core.Workflow;
using Mosaik.Models;
using Mosaik.Models.Workflow;

namespace Mosaik.Services.Workflow
{
    // Plan 36 W-09 + W-11 — Hangfire periodic job.
    // 30 dakikada bir aktif instance'ları tarar:
    //   1. StepEntered tarihi + deadlineDays > now → süresi dolmuş
    //   2. Süresi dolmuşsa + henüz EscalationFired log yoksa → escalate
    //   3. Escalation hedefi: step.properties.escalateTo (UserId int veya role string)
    //      - hedef yoksa fallback: admin rolüne bildirim
    // Idempotent — aynı instance+step için EscalationFired sadece bir kez yazılır.
    public class WorkflowStepProcessor
    {
        private readonly MosaikContext _db;
        private readonly INotificationService _notifications;
        private readonly ILogger<WorkflowStepProcessor> _logger;

        public WorkflowStepProcessor(
            MosaikContext db,
            INotificationService notifications,
            ILogger<WorkflowStepProcessor> logger)
        {
            _db = db;
            _notifications = notifications;
            _logger = logger;
        }

        public async Task ExecuteAsync(CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var active = await _db.WorkflowInstances.AsNoTracking()
                .Where(i => i.Status == WorkflowInstanceStatus.Active && i.CurrentStepId != null)
                .Include(i => i.Template)
                .ToListAsync(ct);

            int processed = 0;
            int escalated = 0;
            int failed = 0;

            foreach (var instance in active)
            {
                processed++;
                try
                {
                    if (instance.Template is null) continue;
                    var definition = WorkflowDefinition.Parse(instance.Template.DefinitionJson);
                    var step = definition?.Steps.FirstOrDefault(s => s.Id == instance.CurrentStepId);
                    if (step?.Properties is null) continue;

                    if (!TryGetInt(step.Properties, "deadlineDays", out var deadlineDays) || deadlineDays <= 0)
                        continue;

                    var stepEnteredAt = await _db.WorkflowInstanceLogs.AsNoTracking()
                        .Where(l => l.InstanceId == instance.Id
                                 && l.EventType == WorkflowEventType.StepEntered
                                 && l.StepId == instance.CurrentStepId)
                        .OrderByDescending(l => l.OccurredAt)
                        .Select(l => (DateTime?)l.OccurredAt)
                        .FirstOrDefaultAsync(ct);
                    if (stepEnteredAt is null) continue;

                    var deadline = stepEnteredAt.Value.AddDays(deadlineDays);
                    // Escalate threshold: deadline + 1 gün geçmişse
                    if (now < deadline.AddDays(1)) continue;

                    // Idempotent — aynı instance+step için EscalationFired var mı?
                    var alreadyFired = await _db.WorkflowInstanceLogs.AsNoTracking()
                        .AnyAsync(l => l.InstanceId == instance.Id
                                    && l.EventType == WorkflowEventType.EscalationFired
                                    && l.StepId == instance.CurrentStepId, ct);
                    if (alreadyFired) continue;

                    // EscalationFired log yaz
                    var payload = JsonSerializer.Serialize(new
                    {
                        stepName = step.Name,
                        deadlineDays,
                        deadline = deadline.ToString("o"),
                        delayedHours = (now - deadline).TotalHours
                    });
                    _db.WorkflowInstanceLogs.Add(new WorkflowInstanceLog
                    {
                        InstanceId = instance.Id,
                        StepId = instance.CurrentStepId,
                        EventType = WorkflowEventType.EscalationFired,
                        ActorId = null,
                        OccurredAt = now,
                        PayloadJson = payload
                    });
                    await _db.SaveChangesAsync(ct);

                    // Hedef belirle: escalateTo UserId | escalateTo role | admin fallback
                    var escalationTargets = await ResolveEscalationTargetsAsync(step, ct);
                    if (escalationTargets.Count == 0)
                    {
                        _logger.LogInformation(
                            "WorkflowStepProcessor: escalation hedefi bulunamadı, log yazıldı ama bildirim atılmadı. InstanceId={Id} StepId={StepId}",
                            instance.Id, instance.CurrentStepId);
                        escalated++;
                        continue;
                    }

                    var title = $"Geciken onay: {step.Name ?? step.Id}";
                    var message = $"{instance.EntityType} #{instance.EntityId} workflow adımı {(int)(now - deadline).TotalHours} saattir bekliyor.";
                    var externalKey = $"workflow_escalation:{instance.Id}:{instance.CurrentStepId}";

                    await _notifications.CreateBulkIfNotExistsAsync(
                        externalKey,
                        escalationTargets,
                        entityType: "workflow_instance",
                        entityId: instance.Id,
                        title: title,
                        message: message,
                        targetUrl: $"/Workflow/Instance/{instance.Id}",
                        notificationType: "workflow_escalation",
                        createdBy: "workflow");
                    escalated++;
                }
                catch (Exception ex)
                {
                    failed++;
                    _logger.LogError(ex,
                        "WorkflowStepProcessor: InstanceId={Id} işlenirken hata.", instance.Id);
                }
            }

            _logger.LogInformation(
                "WorkflowStepProcessor tamamlandı. Aktif: {Total}, Eskalasyon: {Esc}, Hata: {Failed}",
                processed, escalated, failed);
        }

        private async Task<List<int>> ResolveEscalationTargetsAsync(WorkflowDefinitionStep step, CancellationToken ct)
        {
            var ids = new HashSet<int>();
            if (step.Properties is null) return ids.ToList();

            // 1. escalateTo numeric → UserId
            if (TryGetInt(step.Properties, "escalateTo", out var uid))
            {
                ids.Add(uid);
                return ids.ToList();
            }

            // 2. escalateTo string → role lookup
            if (step.Properties.TryGetValue("escalateTo", out var role)
                && role.ValueKind == JsonValueKind.String)
            {
                var roleName = role.GetString();
                if (!string.IsNullOrWhiteSpace(roleName))
                {
                    var roleUsers = await _db.UserRoles.AsNoTracking()
                        .Where(ur => ur.Role!.Name == roleName)
                        .Select(ur => ur.UserId)
                        .ToListAsync(ct);
                    foreach (var u in roleUsers) ids.Add(u);
                    return ids.ToList();
                }
            }

            // 3. Fallback — admin rolü
            var adminUsers = await _db.UserRoles.AsNoTracking()
                .Where(ur => ur.Role!.Name == "admin")
                .Select(ur => ur.UserId)
                .ToListAsync(ct);
            foreach (var u in adminUsers) ids.Add(u);
            return ids.ToList();
        }

        private static bool TryGetInt(Dictionary<string, JsonElement> props, string key, out int value)
        {
            value = 0;
            if (!props.TryGetValue(key, out var el)) return false;
            if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var n))
            {
                value = n; return true;
            }
            if (el.ValueKind == JsonValueKind.String && int.TryParse(el.GetString(), out var s))
            {
                value = s; return true;
            }
            return false;
        }
    }
}
