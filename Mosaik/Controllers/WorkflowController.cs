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
        private readonly AuditLogService _auditLog;
        private readonly ILogger<WorkflowController> _logger;

        public WorkflowController(
            MosaikContext db,
            IWorkflowService engine,
            AuditLogService auditLog,
            ILogger<WorkflowController> logger)
        {
            _db = db;
            _engine = engine;
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

            var userRoles = User.Claims
                .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
                .Select(c => c.Value)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

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
            }

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
                    var userRoles = User.Claims
                        .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
                        .Select(c => c.Value)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);
                    canRespond = IsAssignedToUser(step, userId, userRoles);
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

            var userRoles = User.Claims
                .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
                .Select(c => c.Value)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (!IsAssignedToUser(step, actorId, userRoles)) return Forbid();

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

        // ─── ortak yardımcılar (Admin partial da kullanır) ───

        private bool TryGetUserId(out int userId)
        {
            return int.TryParse(
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                out userId);
        }

        // Step.properties.assigneeUserId | assigneeUserIds | assigneeRole pattern.
        private static bool IsAssignedToUser(WorkflowDefinitionStep step, int userId, ISet<string> userRoles)
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
