using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mosaik.Core.Lookup;
using Mosaik.Services;

namespace Mosaik.Controllers
{
    [Authorize(Roles = "admin")]
    public partial class AdminController
    {
        [HttpGet]
        public async Task<IActionResult> Lookup()
        {
            var types = await _lookupService.GetTypesAsync();
            return View(types);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LookupAddValue(int typeId, string code, string label, int displayOrder)
        {
            var result = await _lookupService.CreateValueAsync(typeId, code, label, displayOrder, User.Identity?.Name ?? "admin");
            TempData["Message"] = result.Message;
            TempData["MessageType"] = result.IsSuccess ? "success" : "error";
            return RedirectToAction(nameof(Lookup));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LookupToggleValue(int valueId, bool active)
        {
            var result = await _lookupService.SetActiveAsync(valueId, active, User.Identity?.Name ?? "admin");
            TempData["Message"] = result.Message;
            TempData["MessageType"] = result.IsSuccess ? "success" : "error";
            return RedirectToAction(nameof(Lookup));
        }
    }
}
