using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Email;
using Mosaik.Core.Notification;
using Mosaik.Models;
using Mosaik.Models.Workflow;

namespace Mosaik.Services.Workflow
{
    // Plan 36 W-10 — workflow step bildirim katmanı.
    // StepEntered → atanan kullanıcıya in-app notification + email (Plan 31 SMTP caller).
    // Step properties.assignee = "user:42" veya "role:mali" pattern destekler.
    public class WorkflowNotifier
    {
        private readonly MosaikContext _db;
        private readonly INotificationService _notifications;
        private readonly IEmailService _email;
        private readonly ILogger<WorkflowNotifier> _logger;

        public WorkflowNotifier(
            MosaikContext db,
            INotificationService notifications,
            IEmailService email,
            ILogger<WorkflowNotifier> logger)
        {
            _db = db;
            _notifications = notifications;
            _email = email;
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

            // Email gönder (SMTP enabled ise). Idempotency: notification servisi zaten
            // "if-not-exists" — bu çağrı her StepEntered tetiklendiğinde mail at YOK,
            // sadece yeni-eklenen userIds icin. Şimdilik basit: tüm assignee'ye mail at.
            // (Çoklu retrigger durumu engine seviyesinde — StepEntered idempotent değil
            // ama Engine.AdvanceAsync zaten tek-yön zincirde tetikler.)
            await SendStepEmailAsync(assigneeUserIds, title, instance, step, targetUrl, ct);
        }

        private async Task SendStepEmailAsync(
            List<int> userIds,
            string subject,
            WorkflowInstance instance,
            WorkflowDefinitionStep step,
            string targetUrl,
            CancellationToken ct)
        {
            if (!_email.IsEnabled) return;

            var emails = await _db.Users.AsNoTracking()
                .Where(u => userIds.Contains(u.UserId)
                         && u.IsActive
                         && u.Email != null
                         && u.Email != "")
                .Select(u => u.Email!)
                .ToListAsync(ct);
            if (emails.Count == 0)
            {
                _logger.LogDebug("WorkflowNotifier email: hicbir assignee'nin email'i yok. InstanceId={Id}", instance.Id);
                return;
            }

            var stepName = System.Net.WebUtility.HtmlEncode(step.Name ?? step.Id);
            var entityRef = System.Net.WebUtility.HtmlEncode($"{instance.EntityType} #{instance.EntityId}");
            var baseUrl = _settingsBaseUrl();
            var fullUrl = $"{baseUrl}{targetUrl}";

            var htmlBody = $@"<!DOCTYPE html>
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

            var result = await _email.SendBulkAsync(emails, $"[Mosaik] {subject}", htmlBody, ct);
            if (!result.AllSucceeded)
            {
                _logger.LogWarning(
                    "WorkflowNotifier email: bazi alici(lara) gonderilemedi. InstanceId={Id} StepId={StepId} Sent={Sent} Failed={Failed}",
                    instance.Id, step.Id, result.Sent, result.Failures.Count);
            }
        }

        private string _settingsBaseUrl()
        {
            // SmtpSettings.BaseUrl yoksa relative URL email'de kirik link uretir.
            // Plan 32 sonrasi env-driven configure edilecek. Sadece path donmek
            // alici icin yetersiz — varsayilan: localhost:5197 (dev).
            return Environment.GetEnvironmentVariable("MOSAIK_BASE_URL") ?? "http://localhost:5197";
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
