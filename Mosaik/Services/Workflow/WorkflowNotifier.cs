using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Notification;
using Mosaik.Models;
using Mosaik.Models.Workflow;

namespace Mosaik.Services.Workflow
{
    // Plan 36 W-10 — workflow step bildirim katmanı.
    // StepEntered → atanan kullanıcıya in-app notification (email Faz B sonraki adım).
    // Step properties.assignee = "user:42" veya "role:mali" pattern destekler.
    public class WorkflowNotifier
    {
        private readonly MosaikContext _db;
        private readonly INotificationService _notifications;
        private readonly ILogger<WorkflowNotifier> _logger;

        public WorkflowNotifier(MosaikContext db, INotificationService notifications, ILogger<WorkflowNotifier> logger)
        {
            _db = db;
            _notifications = notifications;
            _logger = logger;
        }

        public async Task NotifyStepEnteredAsync(int instanceId, string stepId, CancellationToken ct = default)
        {
            var instance = await _db.WorkflowInstances.AsNoTracking()
                .Include(i => i.Template)
                .FirstOrDefaultAsync(i => i.Id == instanceId, ct);
            if (instance is null || instance.Template is null)
            {
                _logger.LogDebug("WorkflowNotifier: instance {Id} bulunamadı.", instanceId);
                return;
            }

            var definition = WorkflowDefinition.Parse(instance.Template.DefinitionJson);
            var step = definition?.Steps.FirstOrDefault(s => s.Id == stepId);
            if (step is null) return;

            var assigneeUserIds = await ResolveAssigneesAsync(step, instance.FirmaId, ct);
            if (assigneeUserIds.Count == 0)
            {
                _logger.LogInformation(
                    "WorkflowNotifier: step {StepId} (instance {InstanceId}) için atanan kullanıcı bulunamadı.",
                    stepId, instanceId);
                return;
            }

            var title = $"Onayınız bekleniyor: {step.Name ?? step.Id}";
            var message = $"{instance.EntityType} #{instance.EntityId} için workflow adımı sizde.";
            var targetUrl = $"/Workflow/Instance/{instance.Id}";
            var externalKey = $"workflow_step:{instance.Id}:{stepId}";

            await _notifications.CreateBulkIfNotExistsAsync(
                externalKey,
                assigneeUserIds,
                entityType: "workflow_instance",
                entityId: instance.Id,
                title: title,
                message: message,
                targetUrl: targetUrl,
                notificationType: "workflow_step",
                createdBy: "workflow");
        }

        // Step.properties.assigneeUserId | assigneeUserIds | assigneeRole okur.
        private async Task<List<int>> ResolveAssigneesAsync(WorkflowDefinitionStep step, int firmaId, CancellationToken ct)
        {
            var ids = new HashSet<int>();
            if (step.Properties is null) return ids.ToList();

            if (step.Properties.TryGetValue("assigneeUserId", out var single)
                && single.ValueKind == System.Text.Json.JsonValueKind.Number
                && single.TryGetInt32(out var sid))
                ids.Add(sid);

            if (step.Properties.TryGetValue("assigneeUserIds", out var many)
                && many.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var el in many.EnumerateArray())
                    if (el.TryGetInt32(out var id)) ids.Add(id);
            }

            if (step.Properties.TryGetValue("assigneeRole", out var role)
                && role.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                var roleName = role.GetString();
                if (!string.IsNullOrWhiteSpace(roleName))
                {
                    var roleUsers = await _db.UserRoles.AsNoTracking()
                        .Where(ur => ur.Role!.Name == roleName)
                        .Select(ur => ur.UserId)
                        .ToListAsync(ct);
                    foreach (var u in roleUsers) ids.Add(u);
                }
            }

            return ids.ToList();
        }
    }
}
