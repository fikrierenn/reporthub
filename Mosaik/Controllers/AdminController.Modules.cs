using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mosaik.Models;
using Mosaik.Services;

namespace Mosaik.Controllers
{
    public partial class AdminController
    {
        [Route("Admin/Modules")]
        public async Task<IActionResult> Modules()
        {
            var modules = await _context.AppModules.AsNoTracking().OrderBy(m => m.SortOrder).ToListAsync();
            return View(modules);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/Modules")]
        public async Task<IActionResult> Modules(List<int> enabledIds)
        {
            var modules = await _context.AppModules.ToListAsync();
            foreach (var m in modules)
                m.IsEnabled = enabledIds.Contains(m.ModuleId);

            await _context.SaveChangesAsync();
            _moduleService.Invalidate();

            await _auditLog.LogAsync(new AuditLogEntry
            {
                EventType = "modules_update",
                TargetType = "modules",
                TargetKey = "all",
                Description = "Modül görünürlükleri güncellendi",
                NewValuesJson = AuditLogService.ToJson(modules.Select(m => new { m.ModuleKey, m.IsEnabled }))
            });

            TempData["Message"] = "Modül ayarları kaydedildi.";
            TempData["MessageType"] = "success";
            return RedirectToAction(nameof(Modules));
        }
    }
}
