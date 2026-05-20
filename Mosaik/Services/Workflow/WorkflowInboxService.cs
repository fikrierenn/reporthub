using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Workflow;
using Mosaik.Models;
using Mosaik.ViewModels.Workflow;

namespace Mosaik.Services.Workflow
{
    // Plan 36 W-16 — Inbox query servisi.
    // WorkflowController.Inbox + DashboardController widget ortak kullanır.
    public class WorkflowInboxService
    {
        private readonly MosaikContext _db;

        public WorkflowInboxService(MosaikContext db)
        {
            _db = db;
        }

        // Aktif step'i userId'ye atanmış instance'ları döner. limit null → hepsi.
        public async Task<List<InboxItem>> GetPendingForUserAsync(
            int userId,
            ISet<string> userRoles,
            int? limit = null,
            CancellationToken ct = default)
        {
            var active = await _db.WorkflowInstances.AsNoTracking()
                .Where(i => i.Status == WorkflowInstanceStatus.Active && i.CurrentStepId != null)
                .Include(i => i.Template)
                .ToListAsync(ct);

            var items = new List<InboxItem>();
            foreach (var instance in active)
            {
                if (instance.Template is null) continue;
                var definition = WorkflowDefinition.Parse(instance.Template.DefinitionJson);
                var step = definition?.Steps.FirstOrDefault(s => s.Id == instance.CurrentStepId);
                if (step is null) continue;

                if (!IsAssignedToUser(step, userId, userRoles)) continue;

                items.Add(new InboxItem
                {
                    InstanceId = instance.Id,
                    TemplateName = instance.Template.Name,
                    StepLabel = step.Name ?? step.Id,
                    EntityType = instance.EntityType,
                    EntityId = instance.EntityId,
                    StartedAt = instance.StartedAt
                });

                if (limit.HasValue && items.Count >= limit.Value) break;
            }
            return items;
        }

        public async Task<int> CountPendingForUserAsync(
            int userId,
            ISet<string> userRoles,
            CancellationToken ct = default)
        {
            var items = await GetPendingForUserAsync(userId, userRoles, limit: null, ct);
            return items.Count;
        }

        // Step.properties.assigneeUserId | assigneeUserIds | assigneeRole assignee check.
        public static bool IsAssignedToUser(WorkflowDefinitionStep step, int userId, ISet<string> userRoles)
        {
            if (step.Properties is null) return false;

            if (step.Properties.TryGetValue("assigneeUserId", out var single)
                && single.ValueKind == JsonValueKind.Number
                && single.TryGetInt32(out var sid)
                && sid == userId)
                return true;

            if (step.Properties.TryGetValue("assigneeUserIds", out var many)
                && many.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in many.EnumerateArray())
                    if (el.TryGetInt32(out var id) && id == userId) return true;
            }

            if (step.Properties.TryGetValue("assigneeRole", out var role)
                && role.ValueKind == JsonValueKind.String)
            {
                var roleName = role.GetString();
                if (!string.IsNullOrWhiteSpace(roleName) && userRoles.Contains(roleName))
                    return true;
            }
            return false;
        }
    }
}
