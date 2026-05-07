using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Lookup;
using Mosaik.Modules.Circular.Models;
using Mosaik.Modules.Circular.Services;
using Mosaik.Modules.Circular.ViewModels;

namespace Mosaik.Modules.Circular.Areas.Circular.Controllers
{
    [Area("Circular")]
    [Authorize]
    public class BlockController : Controller
    {
        private readonly BlockService _service;
        private readonly ILookupService _lookup;
        private readonly DbContext _db;
        private readonly BlockFileService _dosya;

        public BlockController(BlockService service, ILookupService lookup, DbContext db, BlockFileService file)
        {
            _service = service;
            _lookup = lookup;
            _db = db;
            _dosya = file;
        }

        private int CurrentUserId =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        private async Task<List<BlockTypeOption>> LoadBlokTuruAsync()
        {
            var values = await _lookup.GetValuesAsync("blockType");
            return values.Select(v => new BlockTypeOption { Id = v.Id, Label = v.Label }).ToList();
        }

        public async Task<IActionResult> Index(bool? bekleyen)
        {
            // Admin/yetkili → tüm blocks.
            // Diğer kullanıcılar → sadece kendi yazdıkları.
            // (Faz: ileride departman/rol bazlı granular yetkilendirme — Plan 17.1)
            var isAdmin = User.IsInRole("admin");
            int? olusturanFiltresi = isAdmin ? null : CurrentUserId;
            var blocks = await _service.ListAsync(bekleyen, createdById: olusturanFiltresi);
            ViewBag.Bekleyen = bekleyen;
            ViewBag.IsAdminView = isAdmin;
            ViewBag.BlockTypeMap = (await _lookup.GetValuesAsync("blockType"))
                .ToDictionary(v => v.Id, v => v.Label);
            return View(blocks);
        }

        // Sahiplik kontrolü — admin değilse sadece kendi bloklarına erişebilir.
        private bool CanAccess(DailyBlock block) =>
            User.IsInRole("admin") || block.CreatedById == CurrentUserId;

        public async Task<IActionResult> Details(int id)
        {
            var block = await _service.GetAsync(id);
            if (block == null) return NotFound();
            if (!CanAccess(block)) return Forbid();
            ViewBag.BlockTypeMap = (await _lookup.GetValuesAsync("blockType"))
                .ToDictionary(v => v.Id, v => v.Label);
            ViewBag.IsAdminView = User.IsInRole("admin");
            ViewBag.Files = await _dosya.ListByBlockAsync(id);
            return View(block);
        }

        public async Task<IActionResult> Create()
        {
            var form = new BlockFormViewModel
            {
                BlockTypeOptions = await LoadBlokTuruAsync()
            };
            return View(form);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(120_000_000)] // ~12 file × 10 MB tampon
        public async Task<IActionResult> Create(BlockFormViewModel form, List<IFormFile>? files)
        {
            if (!ModelState.IsValid)
            {
                form.BlockTypeOptions = await LoadBlokTuruAsync();
                form.Message = "Form gecersiz.";
                form.MessageType = "error";
                return View(form);
            }

            var result = await _service.CreateAsync(
                CurrentUserId, form.Department, form.Subject, form.Content,
                form.BlockTypeId, form.IsUrgent);

            if (!result.IsSuccess)
            {
                form.BlockTypeOptions = await LoadBlokTuruAsync();
                form.Message = result.Message;
                form.MessageType = "error";
                return View(form);
            }

            // Dosyaları yükle (varsa) — başarısız olanları kullanıcıya bildir
            var newBlokId = result.Data!.Id;
            var uploadFails = await UploadFilesAsync(newBlokId, files);

            TempData["Message"] = string.IsNullOrEmpty(uploadFails)
                ? result.Message
                : result.Message + " " + uploadFails;
            TempData["MessageType"] = string.IsNullOrEmpty(uploadFails) ? "success" : "warn";
            return RedirectToAction(nameof(Details), new { id = newBlokId });
        }

        private async Task<string> UploadFilesAsync(int blockId, List<IFormFile>? files)
        {
            if (files == null || files.Count == 0) return "";
            var fails = new List<string>();
            foreach (var f in files)
            {
                if (f == null || f.Length == 0) continue;
                var r = await _dosya.UploadAsync(blockId, f, CurrentUserId);
                if (!r.IsSuccess) fails.Add($"{f.FileName}: {r.Message}");
            }
            return fails.Count == 0 ? "" : $"({fails.Count} file yüklenemedi: {string.Join("; ", fails)})";
        }

        public async Task<IActionResult> Edit(int id)
        {
            var block = await _service.GetAsync(id);
            if (block == null) return NotFound();
            if (!CanAccess(block)) return Forbid();
            if (block.CircularId.HasValue)
            {
                TempData["Message"] = "Yayinlanmis blok duzenlenemez.";
                TempData["MessageType"] = "error";
                return RedirectToAction(nameof(Details), new { id });
            }

            var form = new BlockFormViewModel
            {
                Id = block.Id,
                BlockNumber = block.BlockNumber,
                Department = block.Department,
                Subject = block.Subject,
                Content = block.Content,
                BlockTypeId = block.BlockTypeId,
                IsUrgent = block.IsUrgent,
                BlockDate = block.BlockDate,
                BlockTypeOptions = await LoadBlokTuruAsync()
            };
            ViewBag.Files = await _dosya.ListByBlockAsync(id);
            return View(form);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(120_000_000)]
        public async Task<IActionResult> Edit(int id, BlockFormViewModel form, List<IFormFile>? files)
        {
            var existing = await _service.GetAsync(id);
            if (existing == null) return NotFound();
            if (!CanAccess(existing)) return Forbid();

            if (!ModelState.IsValid)
            {
                form.Id = id;
                form.BlockTypeOptions = await LoadBlokTuruAsync();
                form.Message = "Form gecersiz.";
                form.MessageType = "error";
                ViewBag.Files = await _dosya.ListByBlockAsync(id);
                return View(form);
            }

            var result = await _service.UpdateAsync(
                id, form.Subject, form.Content, form.BlockTypeId, form.IsUrgent, CurrentUserId);

            if (!result.IsSuccess)
            {
                form.Id = id;
                form.BlockTypeOptions = await LoadBlokTuruAsync();
                form.Message = result.Message;
                form.MessageType = "error";
                ViewBag.Files = await _dosya.ListByBlockAsync(id);
                return View(form);
            }

            var uploadFails = await UploadFilesAsync(id, files);

            TempData["Message"] = string.IsNullOrEmpty(uploadFails)
                ? result.Message
                : result.Message + " " + uploadFails;
            TempData["MessageType"] = string.IsNullOrEmpty(uploadFails) ? "success" : "warn";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _service.GetAsync(id);
            if (existing == null) return NotFound();
            if (!CanAccess(existing)) return Forbid();

            var result = await _service.DeleteAsync(id, CurrentUserId);
            TempData["Message"] = result.Message;
            TempData["MessageType"] = result.IsSuccess ? "success" : "error";
            return RedirectToAction(nameof(Index));
        }

        // ---- Dosya ekleme (Plan 17 Faz E) ----

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(15_000_000)] // 15 MB request limit (10 MB file + form overhead)
        public async Task<IActionResult> UploadFile(int blockId, IFormFile file)
        {
            var block = await _service.GetAsync(blockId);
            if (block == null) return NotFound();
            if (!CanAccess(block)) return Forbid();
            if (block.CircularId.HasValue)
            {
                TempData["Message"] = "Yayınlanmış bloka file eklenemez.";
                TempData["MessageType"] = "error";
                return RedirectToAction(nameof(Edit), new { id = blockId });
            }

            var result = await _dosya.UploadAsync(blockId, file, CurrentUserId);
            TempData["Message"] = result.Message;
            TempData["MessageType"] = result.IsSuccess ? "success" : "error";
            return RedirectToAction(nameof(Edit), new { id = blockId });
        }

        [HttpGet]
        public async Task<IActionResult> DownloadFile(int id)
        {
            var file = await _dosya.GetAsync(id);
            if (file == null) return NotFound();

            // Yetki: dosyanın bağlı olduğu blok sahibi/admin VEYA blok yayında ise tüm okuyucular
            var block = await _service.GetAsync(file.BlockId);
            if (block == null) return NotFound();
            var isAdmin = User.IsInRole("admin");
            var isOwner = block.CreatedById == CurrentUserId;
            var isPublished = block.CircularId.HasValue;
            if (!isAdmin && !isOwner && !isPublished) return Forbid();

            var path = _dosya.GetAbsolutePath(file);
            if (!System.IO.File.Exists(path)) return NotFound();

            return PhysicalFile(path, file.MimeType ?? "application/octet-stream", file.FileName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFile(int id)
        {
            var file = await _dosya.GetAsync(id);
            if (file == null) return NotFound();

            var block = await _service.GetAsync(file.BlockId);
            if (block == null) return NotFound();
            if (!CanAccess(block)) return Forbid();
            if (block.CircularId.HasValue)
            {
                TempData["Message"] = "Yayınlanmış bloktan file silinemez.";
                TempData["MessageType"] = "error";
                return RedirectToAction(nameof(Edit), new { id = file.BlockId });
            }

            var result = await _dosya.DeleteAsync(id, CurrentUserId);
            TempData["Message"] = result.Message;
            TempData["MessageType"] = result.IsSuccess ? "success" : "error";
            return RedirectToAction(nameof(Edit), new { id = file.BlockId });
        }
    }
}
