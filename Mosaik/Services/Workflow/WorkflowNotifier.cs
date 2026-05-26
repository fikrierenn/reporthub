using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Mosaik.Core.Email;
using Mosaik.Core.Messaging;
using Mosaik.Models;
using Mosaik.Models.Workflow;

namespace Mosaik.Services.Workflow
{
    // Plan 36 W-10 — workflow step bildirim katmanı.
    // StepEntered → atanan kullanıcıya in-app notification + email (IMessenger ile birleşik).
    public class WorkflowNotifier
    {
        private readonly MosaikContext _db;
        private readonly IMessenger _messenger;
        private readonly SmtpSettings _smtpSettings;
        private readonly ILogger<WorkflowNotifier> _logger;

        public WorkflowNotifier(
            MosaikContext db,
            IMessenger messenger,
            IOptions<SmtpSettings> smtpOptions,
            ILogger<WorkflowNotifier> logger)
        {
            _db = db;
            _messenger = messenger;
            _smtpSettings = smtpOptions.Value;
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
            var targetUrl = $"/Workflow/Instance/{instance.Id}";
            var externalKey = $"workflow_step:{instance.Id}:{stepId}";
            var htmlEmail = BuildStepEmail(step, instance, targetUrl);

            await _messenger.SendBulkAsync(assigneeUserIds, new MessengerPayload(
                Title: title,
                Body: $"{instance.EntityType} #{instance.EntityId} için workflow adımı sizde.",
                HtmlEmail: htmlEmail,
                EntityType: "workflow_instance",
                EntityId: instance.Id,
                TargetUrl: targetUrl,
                NotificationType: "workflow_step",
                ExternalKey: externalKey
            ), ct);
        }

        private string BuildStepEmail(WorkflowDefinitionStep step, WorkflowInstance instance, string targetUrl)
        {
            var stepName = System.Net.WebUtility.HtmlEncode(step.Name ?? step.Id);
            var entityRef = System.Net.WebUtility.HtmlEncode($"{instance.EntityType} #{instance.EntityId}");
            var baseUrl = !string.IsNullOrWhiteSpace(_smtpSettings.AppUrl)
                ? _smtpSettings.AppUrl.TrimEnd('/')
                : "http://localhost:5197";
            var fullUrl = $"{baseUrl}{targetUrl}";

            return $@"<!DOCTYPE html>
<html lang=""tr""><head><meta charset=""utf-8""></head>
<body style=""font-family:Segoe UI,Arial,sans-serif;background:#f6f7fb;padding:24px;color:#111;"">
  <div style=""max-width:560px;margin:0 auto;background:#fff;border:1px solid #e2e8f0;border-radius:8px;padding:24px;"">
    <h2 style=""margin:0 0 16px 0;font-size:18px;"">Onayınız bekleniyor</h2>
    <p style=""margin:0 0 12px 0;""><strong>{stepName}</strong></p>
    <p style=""margin:0 0 16px 0;color:#475569;"">{entityRef} için workflow adımı sizde.</p>
    <p style=""margin:24px 0;"">
      <a href=""{fullUrl}"" style=""display:inline-block;background:#2563eb;color:#fff;text-decoration:none;padding:10px 18px;border-radius:6px;font-weight:600;"">Akışı Aç</a>
    </p>
    <p style=""margin:24px 0 0 0;color:#94a3b8;font-size:12px;border-top:1px solid #e2e8f0;padding-top:12px;"">
      Bu bildirim Mosaik tarafından otomatik gönderildi. Yanıtlamayın.
    </p>
  </div>
</body></html>";
        }

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
