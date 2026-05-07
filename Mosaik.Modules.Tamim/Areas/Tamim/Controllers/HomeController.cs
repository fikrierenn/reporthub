using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mosaik.Modules.Tamim.Areas.Tamim.Controllers
{
    // Plan 17 Faz A — placeholder Index. Faz B'de gerçek CRUD eklenir.
    [Area("Tamim")]
    [Authorize]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
