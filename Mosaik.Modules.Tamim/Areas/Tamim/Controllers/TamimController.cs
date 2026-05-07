using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mosaik.Core.Lookup;
using Mosaik.Modules.Tamim.Services;

namespace Mosaik.Modules.Tamim.Areas.Tamim.Controllers
{
    // Plan 17 Faz C — Tamim okuma sayfaları (tüm Mosaik kullanıcıları erişebilir).
    // Detail sayfa ziyaretinde IAuditLog "tamim_okundu" event'i otomatik yazılır.
    [Area("Tamim")]
    [Authorize]
    public class TamimController : Controller
    {
        private readonly TamimService _service;
        private readonly ILookupService _lookup;

        public TamimController(TamimService service, ILookupService lookup)
        {
            _service = service;
            _lookup = lookup;
        }

        public async Task<IActionResult> Index()
        {
            var tamimler = await _service.ListAsync(sonNGun: 30);
            return View(tamimler);
        }

        public async Task<IActionResult> Details(int id)
        {
            var tamim = await _service.GetAsync(id);
            if (tamim == null) return NotFound();

            var bloklar = await _service.GetBloklarAsync(id);
            ViewBag.Bloklar = bloklar;
            ViewBag.BlokTuruMap = (await _lookup.GetValuesAsync("blokTuru"))
                .ToDictionary(v => v.Id, v => v.Label);

            // Audit log: "tamim_okundu" — kullanıcı sayfa ziyaret etti
            await _service.TrackReadAsync(id);

            return View(tamim);
        }
    }
}
