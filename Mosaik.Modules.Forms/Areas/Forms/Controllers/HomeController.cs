using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mosaik.Modules.Forms.Areas.Forms.Controllers
{
    // Plan 41 Faz 0 — Forms modülü giriş sayfası (placeholder).
    // Faz 1'de FormsController oluşturulunca burası oraya yönlendirir.
    [Area("Forms")]
    [Authorize]
    public class HomeController : Controller
    {
        public IActionResult Index() => View();
    }
}
