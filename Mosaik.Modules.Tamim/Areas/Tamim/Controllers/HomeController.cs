using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mosaik.Modules.Tamim.Areas.Tamim.Controllers
{
    // Plan 17 Faz A — placeholder. Plan 17 v2'de gerçek controller'lar:
    // BlokController (CRUD), TamimController (read-only + ack), AdminController (okuma raporu).
    [Area("Tamim")]
    [Authorize]
    public class HomeController : Controller
    {
        public IActionResult Index() => View();
    }
}
