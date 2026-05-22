using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mosaik.Models;
using Mosaik.Services;
using Mosaik.ViewModels;

namespace Mosaik.Controllers
{
    // Partial split (csharp-conventions hard-limit). DataSource CRUD action'lari.
    public partial class AdminController
    {
        // Ayrı sayfa action'ları
        [Route("Admin/CreateDataSource")]
        public IActionResult CreateDataSource()
        {
            return View(new AdminDataSourceFormViewModel
            {
                DataSource = new DataSource { IsActive = true },
                TemplateConnString = ""
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/CreateDataSource")]
        public async Task<IActionResult> CreateDataSource(
            [Bind("DataSourceKey,Title,ConnString")] DataSource dataSource)
        {
            try
            {
                dataSource.IsActive = ReadFormBool("IsActive");
                dataSource.DataSourceKey = dataSource.DataSourceKey.ToUpper();
                _context.DataSources.Add(dataSource);
                await _context.SaveChangesAsync();
                await _auditLog.LogAsync(new AuditLogEntry
                {
                    EventType = "datasource_create",
                    TargetType = "datasource",
                    TargetKey = dataSource.DataSourceKey,
                    Description = "Data source created",
                    NewValuesJson = AuditLogService.ToJson(new
                    {
                        dataSource.DataSourceKey,
                        dataSource.Title,
                        dataSource.IsActive
                    })
                });
                TempData["Message"] = "Veri kaynağı başarıyla eklendi";
                TempData["MessageType"] = "success";
                return RedirectToAction("Index", new { tab = "datasources" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AdminController.CreateDataSource failed key={Key}", dataSource.DataSourceKey);
                TempData["Message"] = "Veri kaynağı oluşturulurken hata oluştu.";
                TempData["MessageType"] = "error";
                return View(new AdminDataSourceFormViewModel
                {
                    DataSource = dataSource,
                    TemplateConnString = ""
                });
            }
        }

        [Route("Admin/EditDataSource/{key}")]
        public async Task<IActionResult> EditDataSource(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                TempData["Message"] = "Veri kaynağı anahtarı belirtilmedi";
                TempData["MessageType"] = "error";
                return RedirectToAction("Index", new { tab = "datasources" });
            }

            var dataSource = await _context.DataSources
                .Where(d => d.DataSourceKey == key)
                .FirstOrDefaultAsync();
            if (dataSource == null)
            {
                TempData["Message"] = $"Veri kaynağı bulunamadı: '{key}'";
                TempData["MessageType"] = "error";
                return RedirectToAction("Index", new { tab = "datasources" });
            }
            return View(new AdminDataSourceFormViewModel
            {
                DataSource = dataSource,
                TemplateConnString = ""
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/EditDataSource/{key}")]
        public async Task<IActionResult> EditDataSource(
            string key,
            [Bind("Title,ConnString")] DataSource dataSource)
        {
            try
            {
                // IDOR guard: key route'tan, form'dan değil. Mevcut entity'yi route key ile çek, alanları aktar.
                var existing = await _context.DataSources.FirstOrDefaultAsync(d => d.DataSourceKey == key);
                if (existing == null)
                {
                    TempData["Message"] = $"Veri kaynağı bulunamadı: '{key}'";
                    TempData["MessageType"] = "error";
                    return RedirectToAction("Index", new { tab = "datasources" });
                }

                existing.Title = dataSource.Title;
                existing.ConnString = dataSource.ConnString;
                existing.IsActive = ReadFormBool("IsActive");

                await _context.SaveChangesAsync();
                dataSource = existing;
                await _auditLog.LogAsync(new AuditLogEntry
                {
                    EventType = "datasource_update",
                    TargetType = "datasource",
                    TargetKey = dataSource.DataSourceKey,
                    Description = "Data source updated",
                    NewValuesJson = AuditLogService.ToJson(new
                    {
                        dataSource.DataSourceKey,
                        dataSource.Title,
                        ConnStringChanged = true, // Güvenlik: bağlantı dizesi log'a yazılmaz
                        dataSource.IsActive
                    })
                });
                TempData["Message"] = "Veri kaynağı başarıyla güncellendi";
                TempData["MessageType"] = "success";
                return RedirectToAction("Index", new { tab = "datasources" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AdminController.EditDataSource failed key={Key}", key);
                TempData["Message"] = "Veri kaynağı güncellenirken hata oluştu.";
                TempData["MessageType"] = "error";
                return View(new AdminDataSourceFormViewModel
                {
                    DataSource = dataSource,
                    TemplateConnString = ""
                });
            }
        }
    }
}
