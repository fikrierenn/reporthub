using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mosaik.Modules.Tamim.Areas.Tamim.Controllers
{
    // /Tamim ana giriş — Tamim listesine yönlendir.
    [Area("Tamim")]
    [Authorize]
    public class HomeController : Controller
    {
        public IActionResult Index() => RedirectToAction("Index", "Tamim");
    }
}
