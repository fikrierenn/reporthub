using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mosaik.Modules.Kvkk.Areas.Kvkk.ViewModels;
using Mosaik.Modules.Kvkk.Entities;
using Mosaik.Modules.Kvkk.Services;

namespace Mosaik.Modules.Kvkk.Areas.Kvkk.Controllers
{
    // Plan 54 M6 / Plan 40 Faz 4 — reverse search: "bu veri nerede işleniyor?".
    // Modül-içi sayfa (M3 global topbar'a ikinci kutu eklenmez — advisor kararı).
    // Katalog GLOBAL (read), süreç sonuçları FİRMA-scoped.
    [Area("Kvkk")]
    [Authorize(Roles = "admin")]
    public class DataElementController : Controller
    {
        private readonly DataElementService _dataElements;

        public DataElementController(DataElementService dataElements) => _dataElements = dataElements;

        private int CurrentFirmaId =>
            int.TryParse(User.FindFirstValue("FirmaId"), out var id) ? id : 0;

        [HttpGet]
        public async Task<IActionResult> Index(string? q = null)
        {
            ViewBag.Query = q;
            List<DataElement> elements = string.IsNullOrWhiteSpace(q)
                ? await _dataElements.ListAsync()
                : await _dataElements.SearchElementsAsync(q);
            return View(elements);
        }

        [HttpGet]
        public async Task<IActionResult> Reverse(int id)
        {
            var element = await _dataElements.GetElementAsync(id);
            if (element == null)
                return NotFound();

            // Fail-closed: firma claim yoksa süreç sonuçları sızmasın (deny-by-default).
            var firmaId = CurrentFirmaId;
            if (firmaId <= 0)
                return Forbid();

            var processes = await _dataElements.GetProcessesForElementAsync(id, firmaId);
            var withRetention = processes.Count(p => p.HasRetention);

            return View(new DataElementReverseViewModel
            {
                Element = element,
                Processes = processes,
                CompliancePercent = DataElementService.CompliancePercent(processes.Count, withRetention),
                WithoutRetention = processes.Count - withRetention
            });
        }
    }
}
