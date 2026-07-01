using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mosaik.Core.Logging;
using Mosaik.Modules.Kvkk.Entities;
using Mosaik.Modules.Kvkk.Services;

namespace Mosaik.Modules.Kvkk.Areas.Kvkk.Controllers
{
    // Plan 54 M6 / Plan 40 Faz 5 — AI Integrity bulgu paneli. Modül-içi (Faz 4 precedent —
    // cross-modül Reports/Dashboard motoruna bağlanmaz).
    [Area("Kvkk")]
    [Authorize(Roles = "admin")]
    public class IntegrityFindingController : Controller
    {
        private readonly KvkkIntegrityFindingService _findings;
        private readonly IAuditLog _audit;

        public IntegrityFindingController(KvkkIntegrityFindingService findings, IAuditLog audit)
        {
            _findings = findings;
            _audit = audit;
        }

        private int CurrentFirmaId =>
            int.TryParse(User.FindFirstValue("FirmaId"), out var id) ? id : 0;
        private int CurrentUserId =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        [HttpGet]
        public async Task<IActionResult> Index(bool includeDismissed = false)
        {
            ViewBag.IncludeDismissed = includeDismissed;
            var list = await _findings.ListAsync(CurrentFirmaId, includeDismissed);
            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Dismiss(int id, string? reason)
        {
            var r = await _findings.DismissAsync(id, CurrentFirmaId, CurrentUserId, reason);
            TempData["Message"] = r.IsSuccess ? "Bulgu kapatıldı." : r.Message;
            TempData["MessageType"] = r.IsSuccess ? "success" : "error";
            if (r.IsSuccess)
                await _audit.LogAsync("kvkk_integrity_dismiss", "kvkk_integrity_finding", id.ToString(), reason ?? string.Empty);
            return RedirectToAction(nameof(Index));
        }
    }
}
