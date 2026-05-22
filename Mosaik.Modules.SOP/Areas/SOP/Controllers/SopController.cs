using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mosaik.Core.Lookup;
using Mosaik.Core.Workflow;
using Mosaik.Modules.SOP.Services;
using Mosaik.Modules.SOP.ViewModels;

namespace Mosaik.Modules.SOP.Areas.SOP.Controllers
{
    // Plan 34 Faz C — SOP admin CRUD + versiyon + onay başlatma.
    // Roller: admin + sop-editor (Faz B'de role seed atlandı, S-13 sonrası eklenir).
    [Area("SOP")]
    [Authorize(Roles = "admin,sop-editor")]
    public class SopController : Controller
    {
        private readonly SopService _sop;
        private readonly SopApprovalService _approval;
        private readonly ILookupService _lookup;

        public SopController(SopService sop, SopApprovalService approval, ILookupService lookup)
        {
            _sop = sop;
            _approval = approval;
            _lookup = lookup;
        }

        private int CurrentUserId =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        private int CurrentFirmaId =>
            int.TryParse(User.FindFirstValue("FirmaId"), out var id) ? id : 0;

        public async Task<IActionResult> Index()
        {
            var list = await _sop.ListByFirmaAsync(CurrentFirmaId);
            ViewBag.CategoryMap = (await _lookup.GetValuesAsync("sopCategory"))
                .ToDictionary(v => v.Code, v => v.Label);
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await PopulateLookupsAsync();
            return View(new SopFormViewModel
            {
                FirmaId = CurrentFirmaId,
                ReadDeadlineDays = 30,
                RequiresIKApproval = true,
                AiAdvisorEnabled = true
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SopFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateLookupsAsync();
                return View(model);
            }

            var input = MapToInput(model);
            var result = await _sop.CreateAsync(input, CurrentUserId);
            if (!result.IsSuccess || result.Data == null)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                await PopulateLookupsAsync();
                return View(model);
            }

            TempData["Message"] = result.Message;
            return RedirectToAction(nameof(Details), new { id = result.Data.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var doc = await _sop.GetAsync(id);
            if (doc == null) return NotFound();

            await PopulateLookupsAsync();
            return View(new SopFormViewModel
            {
                Id = doc.Id,
                FirmaId = doc.FirmaId,
                Title = doc.Title,
                Description = doc.Description,
                Category = doc.Category,
                DepartmentIds = doc.DepartmentIds,
                IsCompanyWide = doc.IsCompanyWide,
                OwnerUserId = doc.OwnerUserId,
                ReadDeadlineDays = doc.ReadDeadlineDays,
                RequiresIKApproval = doc.RequiresIKApproval,
                AiAdvisorEnabled = doc.AiAdvisorEnabled
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SopFormViewModel model)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid)
            {
                await PopulateLookupsAsync();
                return View(model);
            }

            var input = MapToInput(model);
            var result = await _sop.UpdateAsync(id, input, CurrentUserId);
            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                await PopulateLookupsAsync();
                return View(model);
            }

            TempData["Message"] = result.Message;
            return RedirectToAction(nameof(Details), new { id });
        }

        public async Task<IActionResult> Details(int id)
        {
            var doc = await _sop.GetAsync(id);
            if (doc == null) return NotFound();

            var active = await _sop.GetActiveVersionAsync(id);
            return View(new SopDetailsViewModel
            {
                Document = doc,
                Versions = doc.Versions.OrderByDescending(v => v.VersionNumber).ToList(),
                ActiveVersion = active
            });
        }

        [HttpGet]
        public async Task<IActionResult> NewVersion(int id)
        {
            var doc = await _sop.GetAsync(id);
            if (doc == null) return NotFound();
            ViewBag.Document = doc;
            return View(new SopVersionFormViewModel { SopDocumentId = id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> NewVersion(SopVersionFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Document = await _sop.GetAsync(model.SopDocumentId);
                return View(model);
            }

            var result = await _sop.NewVersionAsync(model.SopDocumentId, model.ContentJson, CurrentUserId);
            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                ViewBag.Document = await _sop.GetAsync(model.SopDocumentId);
                return View(model);
            }

            TempData["Message"] = result.Message;
            return RedirectToAction(nameof(Details), new { id = model.SopDocumentId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int versionId)
        {
            var result = await _approval.SubmitAsync(versionId, CurrentUserId);
            TempData["Message"] = result.Message;

            var version = await _sop.GetVersionAsync(versionId);
            if (version == null) return RedirectToAction(nameof(Index));
            return RedirectToAction(nameof(Details), new { id = version.SopDocumentId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Decide(int stepId, int versionId, string decision, string? comment)
        {
            var parsed = decision == "approve" ? ApprovalStatus.Approved
                       : decision == "reject"  ? ApprovalStatus.Rejected
                       : ApprovalStatus.Pending;
            if (parsed == ApprovalStatus.Pending)
            {
                TempData["Message"] = "Karar geçersiz.";
                return RedirectToAction(nameof(Details), new { id = (await _sop.GetVersionAsync(versionId))?.SopDocumentId ?? 0 });
            }

            var result = await _approval.DecideAsync(stepId, parsed, CurrentUserId, comment);
            TempData["Message"] = result.Message;

            var version = await _sop.GetVersionAsync(versionId);
            if (version == null) return RedirectToAction(nameof(Index));
            return RedirectToAction(nameof(Details), new { id = version.SopDocumentId });
        }

        private async Task PopulateLookupsAsync()
        {
            ViewBag.Categories = (await _lookup.GetValuesAsync("sopCategory"))
                .Select(v => new SelectListItemDto(v.Code, v.Label))
                .ToList();
        }

        private static SopDocumentInput MapToInput(SopFormViewModel m) =>
            new(
                FirmaId: m.FirmaId,
                Title: m.Title,
                Description: m.Description,
                Category: m.Category,
                DepartmentIds: m.DepartmentIds,
                IsCompanyWide: m.IsCompanyWide,
                OwnerUserId: m.OwnerUserId,
                ReadDeadlineDays: m.ReadDeadlineDays,
                RequiresIKApproval: m.RequiresIKApproval,
                AiAdvisorEnabled: m.AiAdvisorEnabled);
    }

    public sealed record SelectListItemDto(string Code, string Label);
}
