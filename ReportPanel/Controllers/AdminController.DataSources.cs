using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReportPanel.Models;
using ReportPanel.Services;
using ReportPanel.ViewModels;

namespace ReportPanel.Controllers
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
        public async Task<IActionResult> CreateDataSource(DataSource dataSource)
        {
            try
            {
                // Debug: Form değerlerini kontrol et
                var isActiveFormValue = Request.Form["IsActive"].ToString();
                Console.WriteLine($"Form IsActive değeri: '{isActiveFormValue}'");
                Console.WriteLine($"Model IsActive değeri: {dataSource.IsActive}");

                // Manuel olarak IsActive değerini set et
                dataSource.IsActive = ReadFormBool("IsActive");

                Console.WriteLine($"Final IsActive değeri: {dataSource.IsActive}");

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
                // M-02: CreateDataSource (EF SaveChangesAsync fail path).
                _ = ex;
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
        public async Task<IActionResult> EditDataSource(DataSource dataSource)
        {
            try
            {
                // Manuel olarak IsActive değerini set et
                dataSource.IsActive = ReadFormBool("IsActive");

                _context.DataSources.Update(dataSource);
                await _context.SaveChangesAsync();
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
                        dataSource.ConnString,
                        dataSource.IsActive
                    })
                });
                TempData["Message"] = "Veri kaynağı başarıyla güncellendi";
                TempData["MessageType"] = "success";
                return RedirectToAction("Index", new { tab = "datasources" });
            }
            catch (Exception ex)
            {
                // M-02: EditDataSource.
                _ = ex;
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
