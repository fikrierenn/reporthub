using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Workflow;
using Mosaik.Models;
using Mosaik.Services;

namespace Mosaik.Controllers
{
    // Plan 36 — Workflow Designer + onay akışları.
    // Faz B: W-13 Respond endpoint çalışır (POST /Workflow/Instance/{id}/Respond).
    // UI (W-05..W-07) sonraki batch — Index/Create/Edit/Details view'lar henüz iskelet.
    [Authorize]
    public class WorkflowController : Controller
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

        // GET /Workflow — liste (UI W-06'da dolacak)
        public async Task<IActionResult> Index()
        {
            var instances = await _db.WorkflowInstances.AsNoTracking()
                .OrderByDescending(i => i.StartedAt)
                .Take(50)
                .ToListAsync();
            return View(instances);
        }

        // GET /Workflow/Instance/{id} — detay (UI W-06'da dolacak)
        [HttpGet("Workflow/Instance/{id:int}")]
        public async Task<IActionResult> Instance(int id, CancellationToken ct)
        {
            var instance = await _db.WorkflowInstances.AsNoTracking()
                .Include(i => i.Template)
                .FirstOrDefaultAsync(i => i.Id == id, ct);
            if (instance is null) return NotFound();

            ViewBag.Logs = await _engine.GetLogsAsync(id, ct);
            return View(instance);
        }

        // W-13 — POST /Workflow/Instance/{id}/Respond
        // approved=true → step ilerlet, false → instance reddet/iptal
        [HttpPost("Workflow/Instance/{id:int}/Respond")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Respond(int id, RespondViewModel input, CancellationToken ct)
        {
            if (!int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var actorId))
                return Forbid();

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

            if (!result.IsSuccess)
            {
                TempData["Message"] = result.Message;
                return RedirectToAction(nameof(Instance), new { id });
            }

            TempData["Message"] = result.Message;
            return RedirectToAction(nameof(Instance), new { id });
        }

        public class RespondViewModel
        {
            public bool Approved { get; set; }
            public string? Comment { get; set; }
        }
    }
}
