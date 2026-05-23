using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mosaik.Modules.SOP.Entities;

namespace Mosaik.Modules.SOP.Areas.SOP.Controllers
{
    // Plan 34 + Plan 34.1 — SOP modülü landing page.
    // Rol + atama bazlı 3 kart: Prosedürlerim, AI Danışman, Yönetim (admin/sop-editor).
    [Area("SOP")]
    [Authorize]
    public class HomeController : Controller
    {
        private readonly DbContext _db;

        public HomeController(DbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
            var isAdmin = User.IsInRole("admin");
            var isEditor = User.IsInRole("sop-editor");

            var pendingCount = userId > 0
                ? await _db.Set<SopReadReceipt>()
                    .CountAsync(r => r.UserId == userId && r.ConfirmedAt == null)
                : 0;

            ViewBag.PendingCount = pendingCount;
            ViewBag.IsAdminOrEditor = isAdmin || isEditor;
            return View();
        }
    }
}
