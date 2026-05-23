using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mosaik.Modules.SOP.Services;
using Mosaik.Modules.SOP.ViewModels;

namespace Mosaik.Modules.SOP.Areas.SOP.Controllers
{
    // Plan 34 Faz D — User-facing "Prosedürlerim".
    // Tüm authenticated user erişebilir (sop-reader default = tüm user'lar).
    [Area("SOP")]
    [Authorize]
    public class MyController : Controller
    {
        private readonly SopService _sop;
        private readonly SopReadReceiptService _receipts;

        public MyController(SopService sop, SopReadReceiptService receipts)
        {
            _sop = sop;
            _receipts = receipts;
        }

        private int CurrentUserId =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        // "Prosedürlerim" — atanmış SOP'ları listele (bekleyen üstte).
        public async Task<IActionResult> Index(bool onlyPending = false)
        {
            var list = await _receipts.GetMyReceiptsAsync(CurrentUserId, onlyPending);
            var items = list.Select(SopMyListItemViewModel.FromReceipt).ToList();
            ViewBag.OnlyPending = onlyPending;
            return View(items);
        }

        // Detay — versiyonu oku + ReadAt set.
        public async Task<IActionResult> Read(int versionId)
        {
            var version = await _sop.GetVersionAsync(versionId);
            if (version?.SopDocument == null) return NotFound();

            await _receipts.MarkReadAsync(versionId, CurrentUserId);
            var receipt = await _receipts.GetByVersionUserAsync(versionId, CurrentUserId);

            return View(new SopMyDetailsViewModel
            {
                Document = version.SopDocument,
                Version = version,
                Receipt = receipt
            });
        }

        // Yazdırma görünümü — kullanıcı kendi okuma sayfasından da PDF üretebilir.
        [HttpGet]
        public async Task<IActionResult> Print(int versionId)
        {
            var version = await _sop.GetVersionAsync(versionId);
            if (version?.SopDocument == null) return NotFound();
            var receipt = await _receipts.GetByVersionUserAsync(versionId, CurrentUserId);
            return View("~/Areas/SOP/Views/Sop/Print.cshtml", new SopMyDetailsViewModel
            {
                Document = version.SopDocument,
                Version = version,
                Receipt = receipt
            });
        }

        // "Okudum + onayladım" POST.
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int versionId)
        {
            var result = await _receipts.ConfirmAsync(versionId, CurrentUserId);
            TempData["Message"] = result.Message;
            return RedirectToAction(nameof(Read), new { versionId });
        }
    }
}
