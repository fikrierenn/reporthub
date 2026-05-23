using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mosaik.Core.AI.Skills;

namespace Mosaik.Modules.SOP.Areas.SOP.Controllers
{
    // Plan 34.1 + Skill Catalog — admin sayfası: yüklü skill'leri listele, preview, reload.
    // App_Data/ai-skills/*.md kullanıcı manuel ekler (git tracked); Mosaik runtime'da görür.
    [Area("SOP")]
    [Authorize(Roles = "admin")]
    public class SkillsController : Controller
    {
        private readonly ISkillCatalog _catalog;

        public SkillsController(ISkillCatalog catalog) => _catalog = catalog;

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var list = await _catalog.ListAsync();
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var skill = await _catalog.GetAsync(id);
            if (skill == null) return NotFound();
            return View(skill);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Reload()
        {
            _catalog.Reload();
            TempData["Message"] = "Skill kataloğu yeniden yüklendi.";
            return RedirectToAction(nameof(Index));
        }
    }
}
