using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mosaik.Modules.Tamim.Models;
using Mosaik.Modules.Tamim.Services;
using Mosaik.Modules.Tamim.ViewModels;

namespace Mosaik.Modules.Tamim.Areas.Tamim.Controllers
{
    [Area("Tamim")]
    [Authorize]
    public class HomeController : Controller
    {
        private readonly TamimService _service;

        public HomeController(TamimService service)
        {
            _service = service;
        }

        private int CurrentUserId =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        public async Task<IActionResult> Index(TamimStatus? status)
        {
            var list = await _service.ListAsync(status);
            ViewBag.StatusFilter = status;
            return View(list);
        }

        public async Task<IActionResult> Details(int id)
        {
            var tamim = await _service.GetAsync(id);
            if (tamim == null) return NotFound();
            return View(tamim);
        }

        public IActionResult Create()
        {
            return View(new TamimFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TamimFormViewModel form)
        {
            if (!ModelState.IsValid)
            {
                form.Message = "Form gecersiz, hatalari duzeltin.";
                form.MessageType = "error";
                return View(form);
            }

            var result = await _service.CreateAsync(
                form.Title, form.Body, CurrentUserId, form.PublishDate, form.ExpiresAt);

            if (!result.IsSuccess)
            {
                form.Message = result.Message;
                form.MessageType = "error";
                return View(form);
            }

            TempData["Message"] = result.Message;
            TempData["MessageType"] = "success";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var tamim = await _service.GetAsync(id);
            if (tamim == null) return NotFound();

            var form = new TamimFormViewModel
            {
                Id = tamim.Id,
                Title = tamim.Title,
                Body = tamim.Body,
                Status = tamim.Status,
                PublishDate = tamim.PublishDate,
                ExpiresAt = tamim.ExpiresAt,
                CreatedById = tamim.CreatedById,
                CreatedAt = tamim.CreatedAt,
                ApprovedById = tamim.ApprovedById
            };
            return View(form);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TamimFormViewModel form)
        {
            if (!ModelState.IsValid)
            {
                form.Id = id;
                form.Message = "Form gecersiz.";
                form.MessageType = "error";
                return View(form);
            }

            var result = await _service.UpdateAsync(
                id, form.Title, form.Body, form.PublishDate, form.ExpiresAt, CurrentUserId);

            if (!result.IsSuccess)
            {
                form.Id = id;
                form.Message = result.Message;
                form.MessageType = "error";
                return View(form);
            }

            TempData["Message"] = result.Message;
            TempData["MessageType"] = "success";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _service.DeleteAsync(id, CurrentUserId);
            TempData["Message"] = result.Message;
            TempData["MessageType"] = result.IsSuccess ? "success" : "error";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(int id, TamimStatus newStatus)
        {
            var result = await _service.ChangeStatusAsync(id, newStatus, CurrentUserId);
            TempData["Message"] = result.Message;
            TempData["MessageType"] = result.IsSuccess ? "success" : "error";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
