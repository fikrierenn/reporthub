using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Lookup;
using Mosaik.Modules.Tamim.Services;
using Mosaik.Modules.Tamim.ViewModels;

namespace Mosaik.Modules.Tamim.Areas.Tamim.Controllers
{
    [Area("Tamim")]
    [Authorize]
    public class BlokController : Controller
    {
        private readonly BlokService _service;
        private readonly ILookupService _lookup;
        private readonly DbContext _db;

        public BlokController(BlokService service, ILookupService lookup, DbContext db)
        {
            _service = service;
            _lookup = lookup;
            _db = db;
        }

        private int CurrentUserId =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        private async Task<List<BlokTuruOption>> LoadBlokTuruAsync()
        {
            var values = await _lookup.GetValuesAsync("blokTuru");
            return values.Select(v => new BlokTuruOption { Id = v.Id, Label = v.Label }).ToList();
        }

        public async Task<IActionResult> Index(bool? bekleyen)
        {
            var bloklar = await _service.ListAsync(bekleyen);
            ViewBag.Bekleyen = bekleyen;
            ViewBag.BlokTuruMap = (await _lookup.GetValuesAsync("blokTuru"))
                .ToDictionary(v => v.Id, v => v.Label);
            return View(bloklar);
        }

        public async Task<IActionResult> Details(int id)
        {
            var blok = await _service.GetAsync(id);
            if (blok == null) return NotFound();
            ViewBag.BlokTuruMap = (await _lookup.GetValuesAsync("blokTuru"))
                .ToDictionary(v => v.Id, v => v.Label);
            return View(blok);
        }

        public async Task<IActionResult> Create()
        {
            var form = new BlokFormViewModel
            {
                BlokTuruSecenekleri = await LoadBlokTuruAsync()
            };
            return View(form);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BlokFormViewModel form)
        {
            if (!ModelState.IsValid)
            {
                form.BlokTuruSecenekleri = await LoadBlokTuruAsync();
                form.Message = "Form gecersiz.";
                form.MessageType = "error";
                return View(form);
            }

            var result = await _service.CreateAsync(
                CurrentUserId, form.DepartmanAdi, form.Konu, form.Aciklama,
                form.BlokTuruId, form.Acil);

            if (!result.IsSuccess)
            {
                form.BlokTuruSecenekleri = await LoadBlokTuruAsync();
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
            var blok = await _service.GetAsync(id);
            if (blok == null) return NotFound();
            if (blok.TamimId.HasValue)
            {
                TempData["Message"] = "Yayinlanmis blok duzenlenemez.";
                TempData["MessageType"] = "error";
                return RedirectToAction(nameof(Details), new { id });
            }

            var form = new BlokFormViewModel
            {
                Id = blok.Id,
                BlokNo = blok.BlokNo,
                DepartmanAdi = blok.DepartmanAdi,
                Konu = blok.Konu,
                Aciklama = blok.Aciklama,
                BlokTuruId = blok.BlokTuruId,
                Acil = blok.Acil,
                BlokTarihi = blok.BlokTarihi,
                BlokTuruSecenekleri = await LoadBlokTuruAsync()
            };
            return View(form);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BlokFormViewModel form)
        {
            if (!ModelState.IsValid)
            {
                form.Id = id;
                form.BlokTuruSecenekleri = await LoadBlokTuruAsync();
                form.Message = "Form gecersiz.";
                form.MessageType = "error";
                return View(form);
            }

            var result = await _service.UpdateAsync(
                id, form.Konu, form.Aciklama, form.BlokTuruId, form.Acil, CurrentUserId);

            if (!result.IsSuccess)
            {
                form.Id = id;
                form.BlokTuruSecenekleri = await LoadBlokTuruAsync();
                form.Message = result.Message;
                form.MessageType = "error";
                return View(form);
            }

            TempData["Message"] = result.Message;
            TempData["MessageType"] = "success";
            return RedirectToAction(nameof(Details), new { id });
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
    }
}
