using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mosaik.Modules.Kvkk.Entities;
using Mosaik.Modules.Kvkk.Services;

namespace Mosaik.Modules.Kvkk.Areas.Kvkk.Controllers
{
    // Plan 54 M6 / Plan 40 Faz 2 — KVİE görüntüleme (Index + Details). Read-side.
    // Write (Create/Edit/Delete/status) Faz 2b. Firma sınırı session claim'den.
    [Area("Kvkk")]
    [Authorize(Roles = "admin")]
    public class ProcessController : Controller
    {
        private readonly KvkkProcessService _processes;

        public ProcessController(KvkkProcessService processes)
        {
            _processes = processes;
        }

        private int CurrentFirmaId =>
            int.TryParse(User.FindFirstValue("FirmaId"), out var id) ? id : 0;

        [HttpGet]
        public async Task<IActionResult> Index(string? department = null, byte? risk = null, string? q = null)
        {
            var firmaId = CurrentFirmaId;
            var all = await _processes.ListByFirmaAsync(firmaId);

            // Departman filtre seçenekleri (tüm firmadan, filtreden bağımsız).
            ViewBag.Departments = all.Select(p => p.Department).Distinct().OrderBy(d => d).ToList();
            ViewBag.Department = department;
            ViewBag.Risk = risk;
            ViewBag.Query = q;

            IEnumerable<KvkkProcess> filtered = all;
            if (!string.IsNullOrWhiteSpace(department))
                filtered = filtered.Where(p => p.Department == department);
            if (risk.HasValue)
                filtered = filtered.Where(p => p.RiskLevel == risk.Value);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                filtered = filtered.Where(p =>
                    p.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (p.Purpose?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (p.Owner?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            return View(filtered.ToList());
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var p = await _processes.GetAsync(id, CurrentFirmaId);
            if (p == null)
                return NotFound();
            return View(p);
        }
    }
}
