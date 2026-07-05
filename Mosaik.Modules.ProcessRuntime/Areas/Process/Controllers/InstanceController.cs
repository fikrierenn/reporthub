using System.Data.Common;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mosaik.Core.Logging;
using Mosaik.Modules.ProcessRuntime.Services;

namespace Mosaik.Modules.ProcessRuntime.Areas.Process.Controllers
{
    // Plan 42 REV 3 Faz 0 — vaka listesi + timeline + manuel başlat/iptal.
    // Adım kararları BURADA DEĞİL: onay /Workflow/Instance/{id}/Respond'da (tek motor).
    [Area("Process")]
    [Authorize]
    public class InstanceController : Controller
    {
        private readonly ProcessExecutionService _execution;
        private readonly ProcessInstanceQueryService _query;
        private readonly Microsoft.EntityFrameworkCore.DbContext _db;
        private readonly IAuditLog _audit;
        private readonly ILogger<InstanceController> _logger;

        public InstanceController(ProcessExecutionService execution, ProcessInstanceQueryService query,
            Microsoft.EntityFrameworkCore.DbContext db, IAuditLog audit, ILogger<InstanceController> logger)
        {
            _execution = execution;
            _query = query;
            _db = db;
            _audit = audit;
            _logger = logger;
        }

        private int CurrentFirmaId => int.TryParse(User.FindFirstValue("firmaId"), out var id) ? id : 0;
        private int CurrentUserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
        private bool IsAdmin => User.IsInRole("admin");

        // Süreçlerim: normal kullanıcı kendi başlattıklarını, admin hepsini görür.
        [HttpGet]
        public async Task<IActionResult> Index(byte? status, CancellationToken ct)
        {
            var rows = await _query.ListAsync(CurrentFirmaId, IsAdmin ? null : CurrentUserId, status, ct);
            ViewBag.StatusFilter = status;
            ViewBag.IsAdmin = IsAdmin;
            return View(rows);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id, CancellationToken ct)
        {
            var d = await _query.DetailAsync(id, CurrentFirmaId, ct);
            if (d is null) return NotFound();

            // Erişim: başlatan veya admin (vaka içeriği kişisel olabilir — DSAR/ihbar).
            if (!IsAdmin && d.Instance.InitiatedById != CurrentUserId)
                return Forbid();

            // Kaba-status tek-yön projeksiyon tazele (workflow bittiyse vaka kapanır — REV 3 #6).
            await _execution.RefreshStatusAsync(d.Instance, ct);
            return View(d);
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Start(CancellationToken ct)
        {
            ViewBag.WorkflowTemplates = await ListProcessTemplatesAsync(ct);
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Start(string title, int? workflowTemplateId, byte priority, DateTime? dueAt, CancellationToken ct)
        {
            var r = await _execution.StartAsync(new ProcessStartInput(
                FirmaId: CurrentFirmaId, Title: title ?? string.Empty, StartedBy: CurrentUserId,
                WorkflowTemplateId: workflowTemplateId, Priority: priority, DueAt: dueAt), ct);

            if (!r.IsSuccess)
            {
                TempData["Error"] = r.Message;
                ViewBag.WorkflowTemplates = await ListProcessTemplatesAsync(ct);
                return View();
            }

            await _audit.LogAsync("process_instance_start", "process_instance", r.Data.ToString(), title);
            TempData["Success"] = "Vaka açıldı.";
            return RedirectToAction(nameof(Details), new { id = r.Data });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string? reason, CancellationToken ct)
        {
            var d = await _query.DetailAsync(id, CurrentFirmaId, ct);
            if (d is null) return NotFound();
            if (!IsAdmin && d.Instance.InitiatedById != CurrentUserId)
                return Forbid();

            var r = await _execution.CancelAsync(id, CurrentFirmaId, CurrentUserId, reason, ct);
            TempData[r.IsSuccess ? "Success" : "Error"] = r.IsSuccess ? "Vaka iptal edildi." : r.Message;
            if (r.IsSuccess)
                await _audit.LogAsync("process_instance_cancel", "process_instance", id.ToString(), reason);
            return RedirectToAction(nameof(Details), new { id });
        }

        // EntityType='ProcessInstance' aktif şablonlar — raw SQL (WorkflowTemplate ana-proje
        // CLR tipi, modül compile bağlanamaz; WorkflowTemplateLookupService/Forms deseni).
        private async Task<List<TemplateRow>> ListProcessTemplatesAsync(CancellationToken ct)
        {
            try
            {
                return await _db.Database.SqlQueryRaw<TemplateRow>(
                        "SELECT Id, Name FROM dbo.WorkflowTemplates WHERE EntityType = 'ProcessInstance' AND IsActive = 1 AND FirmaId = {0} ORDER BY Name",
                        CurrentFirmaId)
                    .ToListAsync(ct);
            }
            catch (DbException ex)
            {
                _logger.LogWarning(ex, "WorkflowTemplates sorgulanamadı — boş şablon listesi.");
                return [];
            }
        }

        public sealed record TemplateRow(int Id, string Name);
    }
}
