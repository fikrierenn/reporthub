using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mosaik.Modules.SOP.Areas.SOP.Controllers
{
    // Plan 34 Faz A — SOP modülü giriş sayfası (placeholder).
    // Faz C'de SopController + admin CRUD eklenecek, Faz D'de SopMyController user-facing.
    [Area("SOP")]
    [Authorize]
    public class HomeController : Controller
    {
        public IActionResult Index() => View();
    }
}
