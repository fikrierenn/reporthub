using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Workflow;
using Mosaik.Models;
using Mosaik.Models.Workflow;
using Mosaik.ViewModels.Workflow;

namespace Mosaik.Services.Workflow
{
    // Plan 36 W-16 — Inbox query servisi.
    // WorkflowController.Inbox + DashboardController widget ortak kullanır.
    // IEntityWorkflowProvider impl — modül projeleri Core üzerinden erişir.
    public class WorkflowInboxService : IEntityWorkflowProvider
    {
        private readonly MosaikContext _db;

        public WorkflowInboxService(MosaikContext db)
        {
            _db = db;
        }

        // Aktif step'i userId'ye atanmış instance'ları döner. limit null → hepsi.
        // firmaIds boşsa sonuç boş döner (güvenli fail).
        public async Task<List<InboxItem>> GetPendingForUserAsync(
            int userId,
            ISet<string> userRoles,
            IReadOnlyList<int> firmaIds,
            int? limit = null,
            CancellationToken ct = default)
        {
            if (firmaIds.Count == 0) return new();

            var active = await _db.WorkflowInstances.AsNoTracking()
                .Where(i => firmaIds.Contains(i.FirmaId)
                         && i.Status == WorkflowInstanceStatus.Active
                         && i.CurrentStepId != null)
                .Include(i => i.Template)
                .ToListAsync(ct);

            // In-memory assignment filter (WorkflowDefinition.Parse gerektirir).
            var matched = new List<WorkflowInstance>();
            foreach (var instance in active)
            {
                if (instance.Template is null) continue;
                var definition = WorkflowDefinition.Parse(instance.Template.DefinitionJson);
                var step = definition?.Steps.FirstOrDefault(s => s.Id == instance.CurrentStepId);
                if (step is null) continue;
                if (!IsAssignedToUser(step, userId, userRoles)) continue;
                matched.Add(instance);
                if (limit.HasValue && matched.Count >= limit.Value) break;
            }

            if (matched.Count == 0) return new();

            // Batch: tüm StartedBy kullanıcı adlarını tek sorguda al.
            var userIds = matched.Select(i => i.StartedBy).Where(id => id > 0).Distinct().ToList();
            var userNames = await ResolveUserNamesAsync(userIds, ct);

            // Batch: entity preview'ları tip bazında tek sorguda al.
            var previews = await ResolveBatchEntityPreviewsAsync(matched, ct);

            var items = new List<InboxItem>(matched.Count);
            foreach (var instance in matched)
            {
                if (instance.Template is null) continue;
                var definition = WorkflowDefinition.Parse(instance.Template.DefinitionJson);
                var step = definition?.Steps.FirstOrDefault(s => s.Id == instance.CurrentStepId);
                if (step is null) continue;

                previews.TryGetValue((instance.EntityType, instance.EntityId), out var preview);
                items.Add(new InboxItem
                {
                    InstanceId = instance.Id,
                    TemplateName = instance.Template.Name,
                    StepLabel = step.Name ?? step.Id,
                    EntityType = instance.EntityType,
                    EntityId = instance.EntityId,
                    StartedAt = instance.StartedAt,
                    EntityTitle = preview.Title ?? $"{instance.EntityType} #{instance.EntityId}",
                    EntityUrl = preview.Url ?? string.Empty,
                    StartedByName = userNames.GetValueOrDefault(instance.StartedBy, string.Empty)
                });
            }
            return items;
        }

        private async Task<Dictionary<(string, int), (string? Title, string? Url)>> ResolveBatchEntityPreviewsAsync(
            IReadOnlyList<WorkflowInstance> instances,
            CancellationToken ct)
        {
            var result = new Dictionary<(string, int), (string?, string?)>();

            var contractIds = instances
                .Where(i => i.EntityType.Equals("contract", StringComparison.OrdinalIgnoreCase))
                .Select(i => i.EntityId).Distinct().ToList();
            if (contractIds.Count > 0)
            {
                var rows = await _db.Contracts.AsNoTracking()
                    .Where(c => contractIds.Contains(c.Id))
                    .Select(c => new { c.Id, c.Title })
                    .ToListAsync(ct);
                foreach (var r in rows)
                    result[("contract", r.Id)] = (r.Title, $"/Contracts/Details/{r.Id}");
            }

            var circularIds = instances
                .Where(i => i.EntityType.Equals("circular", StringComparison.OrdinalIgnoreCase)
                         || i.EntityType.Equals("tamim", StringComparison.OrdinalIgnoreCase))
                .Select(i => i.EntityId).Distinct().ToList();
            if (circularIds.Count > 0)
            {
                var rows = await _db.Set<Mosaik.Modules.Circular.Models.Circular>().AsNoTracking()
                    .Where(c => circularIds.Contains(c.Id))
                    .Select(c => new { c.Id, c.Title })
                    .ToListAsync(ct);
                foreach (var r in rows)
                {
                    result[("circular", r.Id)] = (r.Title, $"/Circular/Circular/Details/{r.Id}");
                    result[("tamim", r.Id)] = (r.Title, $"/Circular/Circular/Details/{r.Id}");
                }
            }

            // Plan 57 B4 — form-tetikli onay (izin talebi vb.): inbox linki yanıt detayına gider
            // (yetki+decrypt-gated ekran). Title = form adı (yanıt İÇERİĞİ değil — şifreli/anonim sızmaz).
            var formSubmissionIds = instances
                .Where(i => i.EntityType.Equals("FormSubmission", StringComparison.OrdinalIgnoreCase))
                .Select(i => i.EntityId).Distinct().ToList();
            if (formSubmissionIds.Count > 0)
            {
                var rows = await _db.Set<Mosaik.Modules.Forms.Entities.FormSubmission>().AsNoTracking()
                    .Where(s => formSubmissionIds.Contains(s.Id))
                    .Select(s => new { s.Id, FormName = s.FormDefinition!.Name })
                    .ToListAsync(ct);
                foreach (var r in rows)
                    result[("FormSubmission", r.Id)] = ($"{r.FormName} — Yanıt #{r.Id}", $"/Forms/FormDefinition/SubmissionDetail/{r.Id}");
            }

            return result;
        }

        // Entity tipine göre title + URL + kısa özet döner. Bilinmeyen tip → ham EntityType #EntityId.
        public async Task<(string Title, string Url, string Summary)> ResolveEntityPreviewAsync(
            string entityType,
            int entityId,
            CancellationToken ct = default)
        {
            switch (entityType?.ToLowerInvariant())
            {
                case "contract":
                    var c = await _db.Contracts.AsNoTracking()
                        .Where(x => x.Id == entityId)
                        .Select(x => new
                        {
                            x.Title,
                            x.Counterparty,
                            x.ContractValue,
                            x.Currency,
                            x.StartDate,
                            x.EndDate
                        })
                        .FirstOrDefaultAsync(ct);
                    if (c is null) return ($"Sözleşme #{entityId}", $"/Contracts/Details/{entityId}", string.Empty);
                    var parts = new List<string>();
                    if (!string.IsNullOrWhiteSpace(c.Counterparty)) parts.Add(c.Counterparty);
                    if (c.ContractValue.HasValue) parts.Add($"{c.ContractValue:N0} {c.Currency ?? "TRY"}");
                    if (c.StartDate.HasValue || c.EndDate.HasValue)
                    {
                        var startStr = c.StartDate?.ToString("dd.MM.yyyy") ?? "?";
                        var endStr = c.EndDate?.ToString("dd.MM.yyyy") ?? "süresiz";
                        parts.Add($"{startStr} — {endStr}");
                    }
                    return (c.Title, $"/Contracts/Details/{entityId}", string.Join(" · ", parts));

                case "circular":
                case "tamim":
                    var circ = await _db.Set<Mosaik.Modules.Circular.Models.Circular>().AsNoTracking()
                        .Where(x => x.Id == entityId)
                        .Select(x => new { x.Title, x.CircularNumber, x.PublishedAt })
                        .FirstOrDefaultAsync(ct);
                    if (circ is null) return ($"Tamim #{entityId}", $"/Circular/Circular/Details/{entityId}", string.Empty);
                    var cParts = new List<string>();
                    if (!string.IsNullOrWhiteSpace(circ.CircularNumber)) cParts.Add(circ.CircularNumber);
                    cParts.Add($"Yayın: {circ.PublishedAt:dd.MM.yyyy}");
                    return (circ.Title, $"/Circular/Circular/Details/{entityId}", string.Join(" · ", cParts));

                case "formsubmission":
                    var fs = await _db.Set<Mosaik.Modules.Forms.Entities.FormSubmission>().AsNoTracking()
                        .Where(x => x.Id == entityId)
                        .Select(x => new { FormName = x.FormDefinition!.Name, x.SubmittedAt })
                        .FirstOrDefaultAsync(ct);
                    if (fs is null) return ($"Form Yanıtı #{entityId}", $"/Forms/FormDefinition/SubmissionDetail/{entityId}", string.Empty);
                    return ($"{fs.FormName} — Yanıt #{entityId}",
                        $"/Forms/FormDefinition/SubmissionDetail/{entityId}",
                        $"Gönderim: {fs.SubmittedAt:dd.MM.yyyy HH:mm}");

                default:
                    return ($"{entityType} #{entityId}", string.Empty, string.Empty);
            }
        }

        public async Task<string> ResolveUserNameAsync(int? userId, CancellationToken ct = default)
        {
            if (!userId.HasValue || userId.Value <= 0) return string.Empty;
            var name = await _db.Users.AsNoTracking()
                .Where(u => u.UserId == userId.Value)
                .Select(u => !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : u.Username)
                .FirstOrDefaultAsync(ct);
            return name ?? $"UserId {userId}";
        }

        // Entity detay sayfaları için — bu entity'ye bağlı tüm workflow instance'ları (en yeni önce).
        public async Task<List<EntityWorkflowSummary>> GetForEntityAsync(
            string entityType,
            int entityId,
            CancellationToken ct = default)
        {
            var instances = await _db.WorkflowInstances.AsNoTracking()
                .Where(i => i.EntityType == entityType && i.EntityId == entityId)
                .Include(i => i.Template)
                .OrderByDescending(i => i.StartedAt)
                .ToListAsync(ct);

            if (instances.Count == 0) return new();

            // Tüm actor ID'lerini topla — tek query'de username çek.
            var actorIds = instances.Select(i => i.StartedBy).Where(id => id > 0).Distinct().ToList();
            var actorNames = await ResolveUserNamesAsync(actorIds, ct);

            var instanceIds = instances.Select(i => i.Id).ToList();
            var completedCounts = await _db.WorkflowInstanceLogs.AsNoTracking()
                .Where(l => instanceIds.Contains(l.InstanceId)
                         && (l.EventType == WorkflowEventType.StepCompleted || l.EventType == WorkflowEventType.StepRejected))
                .GroupBy(l => l.InstanceId)
                .Select(g => new { InstanceId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.InstanceId, x => x.Count, ct);

            var result = new List<EntityWorkflowSummary>();
            foreach (var instance in instances)
            {
                if (instance.Template is null) continue;
                var definition = WorkflowDefinition.Parse(instance.Template.DefinitionJson);
                var step = definition?.Steps.FirstOrDefault(s => s.Id == instance.CurrentStepId);

                string? assigneeName = null;
                if (step?.Properties is not null)
                {
                    if (step.Properties.TryGetValue("assigneeUserId", out var aid)
                        && aid.ValueKind == JsonValueKind.Number && aid.TryGetInt32(out var uid) && uid > 0)
                    {
                        assigneeName = await ResolveUserNameAsync(uid, ct);
                    }
                    else if (step.Properties.TryGetValue("assigneeRole", out var ar)
                        && ar.ValueKind == JsonValueKind.String)
                    {
                        var role = ar.GetString();
                        if (!string.IsNullOrWhiteSpace(role)) assigneeName = $"Rol: {role}";
                    }
                }

                result.Add(new EntityWorkflowSummary
                {
                    InstanceId = instance.Id,
                    TemplateName = instance.Template.Name,
                    Status = instance.Status,
                    CurrentStepName = step?.Name ?? instance.CurrentStepId,
                    CurrentStepAssigneeName = assigneeName,
                    StartedAt = instance.StartedAt,
                    CompletedAt = instance.CompletedAt,
                    StartedByName = actorNames.GetValueOrDefault(instance.StartedBy, ""),
                    StepCount = definition?.Steps.Count ?? 0,
                    CompletedStepCount = completedCounts.GetValueOrDefault(instance.Id, 0)
                });
            }
            return result;
        }

        public async Task<Dictionary<int, string>> ResolveUserNamesAsync(IEnumerable<int> userIds, CancellationToken ct = default)
        {
            var ids = userIds.Where(i => i > 0).Distinct().ToList();
            if (ids.Count == 0) return new();
            var rows = await _db.Users.AsNoTracking()
                .Where(u => ids.Contains(u.UserId))
                .Select(u => new { u.UserId, Name = !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : u.Username })
                .ToListAsync(ct);
            return rows.ToDictionary(r => r.UserId, r => r.Name);
        }

        // DB-side firmaId filter + status filter → in-memory assignment check. Entity preview yok.
        public async Task<int> CountPendingForUserAsync(
            int userId,
            ISet<string> userRoles,
            IReadOnlyList<int> firmaIds,
            CancellationToken ct = default)
        {
            if (firmaIds.Count == 0) return 0;

            var active = await _db.WorkflowInstances.AsNoTracking()
                .Where(i => firmaIds.Contains(i.FirmaId)
                         && i.Status == WorkflowInstanceStatus.Active
                         && i.CurrentStepId != null)
                .Include(i => i.Template)
                .ToListAsync(ct);

            int count = 0;
            foreach (var instance in active)
            {
                if (instance.Template is null) continue;
                var definition = WorkflowDefinition.Parse(instance.Template.DefinitionJson);
                var step = definition?.Steps.FirstOrDefault(s => s.Id == instance.CurrentStepId);
                if (step is null) continue;
                if (IsAssignedToUser(step, userId, userRoles)) count++;
            }
            return count;
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
