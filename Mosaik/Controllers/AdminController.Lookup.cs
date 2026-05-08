using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mosaik.Models;
using Mosaik.Services;

namespace Mosaik.Controllers
{
    [Authorize(Roles = "admin")]
    public partial class AdminController
    {
        [HttpGet]
        public async Task<IActionResult> Lookup()
        {
            try
            {
                var types = await _lookupService.GetTypesAsync();
                return View(types);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AdminController.Lookup GET failed");
                TempData["Message"] = "Beklenmedik bir hata oluştu.";
                TempData["MessageType"] = "error";
                return View(new List<Mosaik.Core.Lookup.DictionaryType>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LookupAddValue(int typeId, string code, string label, int displayOrder)
        {
            try
            {
                var result = await _lookupService.CreateValueAsync(typeId, code, label, displayOrder, User.Identity?.Name ?? "admin");
                TempData["Message"] = result.Message;
                TempData["MessageType"] = result.IsSuccess ? "success" : "error";

                if (result.IsSuccess)
                {
                    await _auditLog.LogAsync(new AuditLogEntry
                    {
                        EventType = "lookup_value_create",
                        TargetType = "lookup",
                        TargetKey = $"{typeId}/{code}",
                        Description = $"Lookup değeri eklendi: {label}",
                        NewValuesJson = AuditLogService.ToJson(new { typeId, code, label, displayOrder }),
                        IsSuccess = true
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AdminController.LookupAddValue failed typeId={TypeId} code={Code}", typeId, code);
                TempData["Message"] = "Beklenmedik bir hata oluştu.";
                TempData["MessageType"] = "error";
            }
            return RedirectToAction(nameof(Lookup));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LookupToggleValue(int valueId, bool active)
        {
            try
            {
                var result = await _lookupService.SetActiveAsync(valueId, active, User.Identity?.Name ?? "admin");
                TempData["Message"] = result.Message;
                TempData["MessageType"] = result.IsSuccess ? "success" : "error";

                if (result.IsSuccess)
                {
                    await _auditLog.LogAsync(new AuditLogEntry
                    {
                        EventType = "lookup_value_toggle",
                        TargetType = "lookup",
                        TargetKey = valueId.ToString(),
                        Description = active ? "Lookup değeri aktif edildi" : "Lookup değeri pasif edildi",
                        NewValuesJson = AuditLogService.ToJson(new { valueId, active }),
                        IsSuccess = true
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AdminController.LookupToggleValue failed valueId={ValueId}", valueId);
                TempData["Message"] = "Beklenmedik bir hata oluştu.";
                TempData["MessageType"] = "error";
            }
            return RedirectToAction(nameof(Lookup));
        }
    }
}
