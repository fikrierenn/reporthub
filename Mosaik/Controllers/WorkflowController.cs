using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Workflow;
using Mosaik.Models;
using Mosaik.Services;
using Mosaik.Services.Workflow;
using Mosaik.ViewModels.Workflow;

namespace Mosaik.Controllers
{
    // Plan 36 — Workflow Designer + onay akışları.
    // Bu dosya: kullanıcı action'ları (Inbox + Instance + Respond).
    // Admin action'ları: WorkflowController.Admin.cs (sadece admin rolü).
    [Authorize]
    public partial class WorkflowController : Controller
    {
        private readonly MosaikContext _db;
        private readonly IWorkflowService _engine;
        private readonly WorkflowInboxService _inbox;
        private readonly AuditLogService _auditLog;
        private readonly ILogger<WorkflowController> _logger;

        public WorkflowController(
            MosaikContext db,
            IWorkflowService engine,
            WorkflowInboxService inbox,
            AuditLogService auditLog,
            ILogger<WorkflowController> logger)
        {
            _db = db;
            _engine = engine;
            _inbox = inbox;
            _auditLog = auditLog;
            _logger = logger;
        }

        // GET /Workflow → admin ise admin paneline, değilse inbox'a redirect.
        public IActionResult Index()
        {
            if (User.IsInRole("admin"))
                return RedirectToAction(nameof(AdminIndex));
            return RedirectToAction(nameof(Inbox));
        }

        // GET /Workflow/Inbox — bana atanan aktif step'ler.
        public async Task<IActionResult> Inbox(CancellationToken ct)
        {
            if (!TryGetUserId(out var userId)) return Forbid();
            var items = await _inbox.GetPendingForUserAsync(userId, GetUserRoles(), limit: null, ct);
            return View(new WorkflowInboxViewModel { Items = items });
        }

        // GET /Workflow/Instance/{id} — detay + Approve/Reject form.
        [HttpGet("Workflow/Instance/{id:int}")]
        public async Task<IActionResult> Instance(int id, CancellationToken ct)
        {
            var instance = await _db.WorkflowInstances.AsNoTracking()
                .Include(i => i.Template)
                .FirstOrDefaultAsync(i => i.Id == id, ct);
            if (instance is null) return NotFound();

            ViewBag.Logs = await _engine.GetLogsAsync(id, ct);

            // CanRespond → kullanıcı aktif step'in atanmış kişisi mi?
            bool canRespond = false;
            if (instance.Status == WorkflowInstanceStatus.Active
                && instance.CurrentStepId is not null
                && instance.Template is not null
                && TryGetUserId(out var userId))
            {
                var definition = WorkflowDefinition.Parse(instance.Template.DefinitionJson);
                var step = definition?.Steps.FirstOrDefault(s => s.Id == instance.CurrentStepId);
                if (step is not null)
                {
                    canRespond = WorkflowInboxService.IsAssignedToUser(step, userId, GetUserRoles());
                }
            }
            ViewBag.CanRespond = canRespond;
            return View(instance);
        }

        // W-13 — POST /Workflow/Instance/{id}/Respond
        [HttpPost("Workflow/Instance/{id:int}/Respond")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Respond(int id, RespondViewModel input, CancellationToken ct)
        {
            if (!TryGetUserId(out var actorId)) return Forbid();

            // Ownership kontrol — engine bunu bilmiyor, controller seviyesinde yapılır.
            var instance = await _db.WorkflowInstances.AsNoTracking()
                .Include(i => i.Template)
                .FirstOrDefaultAsync(i => i.Id == id, ct);
            if (instance is null) return NotFound();

            if (instance.Status != WorkflowInstanceStatus.Active || instance.CurrentStepId is null)
            {
                TempData["Message"] = "Bu workflow zaten kapalı.";
                return RedirectToAction(nameof(Instance), new { id });
            }

            var definition = WorkflowDefinition.Parse(instance.Template?.DefinitionJson ?? string.Empty);
            var step = definition?.Steps.FirstOrDefault(s => s.Id == instance.CurrentStepId);
            if (step is null) return BadRequest();

            if (!WorkflowInboxService.IsAssignedToUser(step, actorId, GetUserRoles())) return Forbid();

            var advance = new WorkflowAdvanceInput(
                ActorId: actorId,
                Approved: input.Approved,
                Comment: string.IsNullOrWhiteSpace(input.Comment) ? null : input.Comment.Trim());

            var result = await _engine.AdvanceAsync(id, advance, ct);

            await _auditLog.LogAsync(new AuditLogEntry
            {
                EventType = input.Approved ? "workflow_step_approved" : "workflow_step_rejected",
                TargetType = "workflow_instance",
                TargetKey = id.ToString(),
                Description = result.Message,
                IsSuccess = result.IsSuccess
            });

            TempData["Message"] = result.Message;
            return RedirectToAction(nameof(Instance), new { id });
        }

        public class RespondViewModel
        {
            public bool Approved { get; set; }
            public string? Comment { get; set; }
        }

        // W-12 — GET /Workflow/Instance/{id}.ics
        // Aktif step'in deadline'ını VCALENDAR formatında döner (Outlook/Apple Calendar import).
        [HttpGet("Workflow/Instance/{id:int}.ics")]
        public async Task<IActionResult> InstanceIcs(int id, CancellationToken ct)
        {
            var instance = await _db.WorkflowInstances.AsNoTracking()
                .Include(i => i.Template)
                .FirstOrDefaultAsync(i => i.Id == id, ct);
            if (instance is null) return NotFound();
            if (instance.Template is null || instance.CurrentStepId is null
                || instance.Status != WorkflowInstanceStatus.Active)
                return NotFound();

            var definition = WorkflowDefinition.Parse(instance.Template.DefinitionJson);
            var step = definition?.Steps.FirstOrDefault(s => s.Id == instance.CurrentStepId);
            if (step?.Properties is null) return NotFound();

            int deadlineDays = 0;
            if (step.Properties.TryGetValue("deadlineDays", out var dd))
            {
                if (dd.ValueKind == JsonValueKind.Number && dd.TryGetInt32(out var n)) deadlineDays = n;
                else if (dd.ValueKind == JsonValueKind.String && int.TryParse(dd.GetString(), out var s)) deadlineDays = s;
            }
            if (deadlineDays <= 0) return NotFound();

            // StepEntered tarihi
            var stepEnteredAt = await _db.WorkflowInstanceLogs.AsNoTracking()
                .Where(l => l.InstanceId == id
                         && l.EventType == WorkflowEventType.StepEntered
                         && l.StepId == instance.CurrentStepId)
                .OrderByDescending(l => l.OccurredAt)
                .Select(l => (DateTime?)l.OccurredAt)
                .FirstOrDefaultAsync(ct);
            if (stepEnteredAt is null) return NotFound();

            var deadline = stepEnteredAt.Value.AddDays(deadlineDays);
            var dtStart = deadline.ToString("yyyyMMddTHHmmssZ");
            var dtEnd = deadline.AddHours(1).ToString("yyyyMMddTHHmmssZ");
            var dtStamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ");

            // ICS metni — VCALENDAR + tek VEVENT
            var summary = IcsEscape($"{instance.Template.Name} — {step.Name ?? step.Id}");
            var description = IcsEscape(
                $"Workflow #{instance.Id} ({instance.EntityType} #{instance.EntityId}) onay adımı son tarihi.");
            var uid = $"workflow-{instance.Id}-step-{instance.CurrentStepId}@mosaik";

            var ics =
                "BEGIN:VCALENDAR\r\n" +
                "VERSION:2.0\r\n" +
                "PRODID:-//Mosaik//Workflow//TR\r\n" +
                "METHOD:PUBLISH\r\n" +
                "BEGIN:VEVENT\r\n" +
                $"UID:{uid}\r\n" +
                $"DTSTAMP:{dtStamp}\r\n" +
                $"DTSTART:{dtStart}\r\n" +
                $"DTEND:{dtEnd}\r\n" +
                $"SUMMARY:{summary}\r\n" +
                $"DESCRIPTION:{description}\r\n" +
                "STATUS:CONFIRMED\r\n" +
                "END:VEVENT\r\n" +
                "END:VCALENDAR\r\n";

            return File(System.Text.Encoding.UTF8.GetBytes(ics), "text/calendar",
                fileDownloadName: $"workflow-{id}.ics");
        }

        // RFC 5545 escape — virgül, noktalı virgül, ters slash, satır sonu.
        private static string IcsEscape(string s) =>
            s.Replace("\\", "\\\\")
             .Replace(",", "\\,")
             .Replace(";", "\\;")
             .Replace("\n", "\\n")
             .Replace("\r", "");

        // ─── ortak yardımcılar (Admin partial da kullanır) ───

        private bool TryGetUserId(out int userId)
        {
            return int.TryParse(
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                out userId);
        }

        private ISet<string> GetUserRoles() =>
            User.Claims
                .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
                .Select(c => c.Value)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
