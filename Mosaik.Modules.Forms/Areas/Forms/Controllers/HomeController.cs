using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mosaik.Modules.Forms.Areas.Forms.Controllers
{
    // Plan 41 Faz 2 — Forms modülü giriş sayfası → FormDefinition listesine yönlendirir.
    [Area("Forms")]
    [Authorize]
    public class HomeController : Controller
    {
        public IActionResult Index() => RedirectToAction("Index", "FormDefinition");
    }
}
