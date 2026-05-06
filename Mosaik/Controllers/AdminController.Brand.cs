using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mosaik.Models;
using Mosaik.Services;

namespace Mosaik.Controllers
{
    public partial class AdminController
    {
        [Route("Admin/BrandSettings")]
        public async Task<IActionResult> BrandSettings()
        {
            var brand = await _context.BrandSettings.AsNoTracking().FirstOrDefaultAsync()
                        ?? new BrandSettings();
            return View(brand);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/BrandSettings")]
        public async Task<IActionResult> BrandSettings(BrandSettings model, IFormFile? logoFile)
        {
            if (!ModelState.IsValid)
                return View(model);

            var brand = await _context.BrandSettings.FirstOrDefaultAsync();
            bool isNew = brand == null;
            brand ??= new BrandSettings { Id = 1 };

            brand.SiteTitle = model.SiteTitle.Trim();
            brand.Slogan = string.IsNullOrWhiteSpace(model.Slogan) ? null : model.Slogan.Trim();
            brand.PrimaryColor = model.PrimaryColor;
            brand.UpdatedAt = DateTime.UtcNow;

            if (logoFile?.Length > 0)
            {
                var ext = Path.GetExtension(logoFile.FileName).ToLowerInvariant();
                if (ext is not (".png" or ".jpg" or ".jpeg" or ".svg" or ".webp"))
                {
                    TempData["Message"] = "Geçersiz logo formatı. PNG, JPG, SVG veya WebP yükleyin.";
                    TempData["MessageType"] = "error";
                    return View(model);
                }

                var dir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "assets", "brand");
                Directory.CreateDirectory(dir);
                var fileName = $"logo{ext}";
                var path = Path.Combine(dir, fileName);
                using var stream = new FileStream(path, FileMode.Create);
                await logoFile.CopyToAsync(stream);
                brand.LogoPath = $"/assets/brand/{fileName}";
            }

            if (isNew)
                _context.BrandSettings.Add(brand);
            else
                _context.BrandSettings.Update(brand);

            await _context.SaveChangesAsync();

            _brandService.Invalidate();

            await _auditLog.LogAsync(new AuditLogEntry
            {
                EventType = "brand_update",
                TargetType = "brand",
                TargetKey = "1",
                Description = "Marka ayarları güncellendi",
                NewValuesJson = AuditLogService.ToJson(new { brand.SiteTitle, brand.Slogan, brand.PrimaryColor, brand.LogoPath })
            });

            TempData["Message"] = "Marka ayarları kaydedildi.";
            TempData["MessageType"] = "success";
            return RedirectToAction(nameof(BrandSettings));
        }
    }
}
