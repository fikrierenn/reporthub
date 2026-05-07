using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mosaik.Modules.Circular.Areas.Circular.Controllers
{
    // /Circular ana giriş — Circular listesine yönlendir.
    [Area("Circular")]
    [Authorize]
    public class HomeController : Controller
    {
        public IActionResult Index() => RedirectToAction("Index", "Circular");
    }
}
