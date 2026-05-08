using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mosaik.Models;
using Mosaik.Services;
using Mosaik.ViewModels;

namespace Mosaik.Controllers
{
    // Plan 20 Faz B — Organizasyon şeması admin partial.
    // Drag-drop tree (SortableJS + HTMX), seed import, cycle-safe reorder.
    [Authorize(Roles = "admin")]
    public partial class AdminController
    {
        [HttpGet]
        public async Task<IActionResult> OrgChart(string? view, [FromServices] IOrgChartService orgChart)
        {
            var allowedViews = new[] { "list", "tree", "horizontal" };
            var currentView = !string.IsNullOrWhiteSpace(view) && allowedViews.Contains(view) ? view! : "list";

            var positions = await orgChart.GetTreeAsync(includeInactive: true);
            var (unknown, err) = await orgChart.GetUnknownZirveCodesAsync();
            return View(new AdminOrgChartViewModel
            {
                Positions = positions,
                UnknownZirveCodes = unknown,
                ZirveDiscoveryError = err,
                CurrentView = currentView,
                Message = TempData["Message"] as string ?? string.Empty,
                MessageType = TempData["MessageType"] as string ?? string.Empty
            });
        }

        [HttpGet]
        public async Task<IActionResult> CreatePosition([FromServices] IOrgChartService orgChart)
        {
            var positions = await orgChart.GetAllAsync(includeInactive: true);
            return View("EditPosition", new AdminOrgPositionFormViewModel
            {
                AvailableParents = positions
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePosition(AdminOrgPositionFormViewModel input,
            [FromServices] IOrgChartService orgChart)
        {
            var by = User.Identity?.Name ?? "admin";
            var result = await orgChart.CreateAsync(input.Code, input.Title,
                input.ParentPositionId, input.DisplayOrder, input.Description, by);
            TempData["Message"] = result.Message;
            TempData["MessageType"] = result.IsSuccess ? "success" : "error";

            if (result.IsSuccess && result.Data != null)
            {
                await _auditLog.LogAsync(new AuditLogEntry
                {
                    EventType = "org_position_create",
                    TargetType = "org_position",
                    TargetKey = result.Data.Id.ToString(),
                    Description = $"Görev eklendi: {result.Data.Title}",
                    NewValuesJson = AuditLogService.ToJson(new { result.Data.Id, result.Data.Code, result.Data.Title, result.Data.ParentPositionId }),
                    IsSuccess = true
                });
                return RedirectToAction(nameof(OrgChart));
            }

            input.AvailableParents = await orgChart.GetAllAsync(includeInactive: true);
            input.Message = result.Message;
            input.MessageType = "error";
            return View("EditPosition", input);
        }

        [HttpGet]
        public async Task<IActionResult> EditPosition(int id, [FromServices] IOrgChartService orgChart)
        {
            var position = await orgChart.GetByIdAsync(id);
            if (position == null) return NotFound();

            var positions = await orgChart.GetAllAsync(includeInactive: true);
            return View(new AdminOrgPositionFormViewModel
            {
                Id = position.Id,
                Code = position.Code,
                Title = position.Title,
                ParentPositionId = position.ParentPositionId,
                DisplayOrder = position.DisplayOrder,
                IsActive = position.IsActive,
                Description = position.Description,
                AvailableParents = positions.Where(p => p.Id != id).ToList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPosition(int id, AdminOrgPositionFormViewModel input,
            [FromServices] IOrgChartService orgChart)
        {
            var by = User.Identity?.Name ?? "admin";
            var result = await orgChart.UpdateAsync(id, input.Title, input.ParentPositionId,
                input.DisplayOrder, input.IsActive, input.Description, by);
            TempData["Message"] = result.Message;
            TempData["MessageType"] = result.IsSuccess ? "success" : "error";

            if (result.IsSuccess)
            {
                await _auditLog.LogAsync(new AuditLogEntry
                {
                    EventType = "org_position_update",
                    TargetType = "org_position",
                    TargetKey = id.ToString(),
                    Description = $"Görev güncellendi: {input.Title}",
                    NewValuesJson = AuditLogService.ToJson(new { id, input.Title, input.ParentPositionId, input.DisplayOrder, input.IsActive }),
                    IsSuccess = true
                });
                return RedirectToAction(nameof(OrgChart));
            }

            input.Id = id;
            var positions = await orgChart.GetAllAsync(includeInactive: true);
            input.AvailableParents = positions.Where(p => p.Id != id).ToList();
            input.Message = result.Message;
            input.MessageType = "error";
            return View(input);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePosition(int id, [FromServices] IOrgChartService orgChart)
        {
            var by = User.Identity?.Name ?? "admin";
            var result = await orgChart.DeleteAsync(id, by);
            TempData["Message"] = result.Message;
            TempData["MessageType"] = result.IsSuccess ? "success" : "error";

            if (result.IsSuccess)
            {
                await _auditLog.LogAsync(new AuditLogEntry
                {
                    EventType = "org_position_delete",
                    TargetType = "org_position",
                    TargetKey = id.ToString(),
                    Description = $"Görev silindi: {id}",
                    IsSuccess = true
                });
            }
            return RedirectToAction(nameof(OrgChart));
        }

        // Drag-drop reorder — JSON body. HTMX `hx-post` ile çağrılır.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OrgChartReorder([FromBody] OrgChartReorderRequest req,
            [FromServices] IOrgChartService orgChart)
        {
            if (req == null) return BadRequest(new { message = "Geçersiz istek." });
            var by = User.Identity?.Name ?? "admin";
            var result = await orgChart.ReorderAsync(req.PositionId, req.NewParentId, req.SiblingOrderIds, by);

            if (result.IsSuccess)
            {
                await _auditLog.LogAsync(new AuditLogEntry
                {
                    EventType = "org_position_reorder",
                    TargetType = "org_position",
                    TargetKey = req.PositionId.ToString(),
                    Description = $"Görev sıralaması güncellendi: id={req.PositionId} → parent={req.NewParentId}",
                    NewValuesJson = AuditLogService.ToJson(new { req.PositionId, req.NewParentId, req.SiblingOrderIds }),
                    IsSuccess = true
                });
                return Ok(new { message = result.Message });
            }
            return BadRequest(new { message = result.Message });
        }

        // Tanımsız Zirve unvanını tek-tık import.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportZirvePosition(string code, [FromServices] IOrgChartService orgChart)
        {
            var by = User.Identity?.Name ?? "admin";
            var result = await orgChart.ImportFromZirveCodeAsync(code, by);
            TempData["Message"] = result.Message;
            TempData["MessageType"] = result.IsSuccess ? "success" : "error";

            if (result.IsSuccess && result.Data != null)
            {
                await _auditLog.LogAsync(new AuditLogEntry
                {
                    EventType = "org_position_import",
                    TargetType = "org_position",
                    TargetKey = result.Data.Id.ToString(),
                    Description = $"Zirve unvanı içe aktarıldı: {result.Data.Code}",
                    NewValuesJson = AuditLogService.ToJson(new { result.Data.Id, result.Data.Code }),
                    IsSuccess = true
                });
            }
            return RedirectToAction(nameof(OrgChart));
        }
    }
}
