using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ReportPanel.Models;
using ReportPanel.Services;
using ReportPanel.ViewModels;

namespace ReportPanel.Controllers
{
    // Plan 07 Faz 6 — FilterDefinition CRUD detay sayfaları (Create/Edit).
    // Delete: AdminController.HandlePostAction → "delete_filter".
    public partial class AdminController
    {
        [Route("Admin/CreateFilter")]
        public async Task<IActionResult> CreateFilter()
        {
            var dataSources = await _context.DataSources.AsNoTracking()
                .Where(ds => ds.IsActive)
                .OrderBy(ds => ds.Title)
                .ToListAsync();
            return View(new AdminFilterDefinitionFormViewModel
            {
                Definition = new FilterDefinition
                {
                    IsActive = true,
                    Scope = FilterDefinition.ScopeSpInjection,
                    DisplayOrder = 100
                },
                DataSources = dataSources
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/CreateFilter")]
        public async Task<IActionResult> CreateFilter(FilterDefinition definition)
        {
            var result = await _filterDefService.CreateAsync(
                definition.FilterKey,
                definition.Label,
                definition.Scope,
                definition.DataSourceKey,
                definition.OptionsQuery,
                ReadFormBool("IsActive"),
                definition.DisplayOrder);
            if (result.Success)
            {
                TempData["Message"] = result.Message;
                TempData["MessageType"] = "success";
                return RedirectToAction("Index", new { tab = "filters" });
            }
            var dataSources = await _context.DataSources.AsNoTracking()
                .Where(ds => ds.IsActive).OrderBy(ds => ds.Title).ToListAsync();
            return View(new AdminFilterDefinitionFormViewModel
            {
                Definition = definition,
                DataSources = dataSources,
                Message = result.Message,
                MessageType = "error"
            });
        }

        [Route("Admin/EditFilter/{id}")]
        public async Task<IActionResult> EditFilter(int id)
        {
            var def = await _context.FilterDefinitions.FindAsync(id);
            if (def == null)
            {
                TempData["Message"] = "Filtre tanimi bulunamadi.";
                TempData["MessageType"] = "error";
                return RedirectToAction("Index", new { tab = "filters" });
            }
            var dataSources = await _context.DataSources.AsNoTracking()
                .Where(ds => ds.IsActive)
                .OrderBy(ds => ds.Title)
                .ToListAsync();
            return View(new AdminFilterDefinitionFormViewModel
            {
                Definition = def,
                DataSources = dataSources
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/EditFilter/{id}")]
        public async Task<IActionResult> EditFilter(int id, FilterDefinition definition)
        {
            // FilterDefinition.FilterDefinitionId [BindNever] — route'tan id explicit al.
            var result = await _filterDefService.UpdateAsync(
                id,
                definition.FilterKey,
                definition.Label,
                definition.Scope,
                definition.DataSourceKey,
                definition.OptionsQuery,
                ReadFormBool("IsActive"),
                definition.DisplayOrder);
            if (result.Success)
            {
                TempData["Message"] = result.Message;
                TempData["MessageType"] = "success";
                return RedirectToAction("Index", new { tab = "filters" });
            }
            definition.FilterDefinitionId = id;
            var dataSources = await _context.DataSources.AsNoTracking()
                .Where(ds => ds.IsActive).OrderBy(ds => ds.Title).ToListAsync();
            return View(new AdminFilterDefinitionFormViewModel
            {
                Definition = definition,
                DataSources = dataSources,
                Message = result.Message,
                MessageType = "error"
            });
        }

        // Plan 07 Faz 6 — Admin form'undan AJAX: OptionsQuery'yi DataSource üzerinde
        // exec etmeden önce 10 satıra kadar ön izleme. Kayıt etmeden test imkanı.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/TestFilterOptionsQuery")]
        public async Task<IActionResult> TestFilterOptionsQuery(
            [FromForm] string? dataSourceKey,
            [FromForm] string? optionsQuery)
        {
            if (string.IsNullOrWhiteSpace(dataSourceKey))
                return Json(new { success = false, error = "DataSource seçilmeli." });
            if (string.IsNullOrWhiteSpace(optionsQuery))
                return Json(new { success = false, error = "OptionsQuery boş." });
            if (!FilterDefinitionService.IsSafeOptionsQuery(optionsQuery, out var unsafeReason))
                return Json(new { success = false, error = $"Güvenlik: {unsafeReason}" });

            var ds = await _context.DataSources.AsNoTracking()
                .FirstOrDefaultAsync(d => d.DataSourceKey == dataSourceKey && d.IsActive);
            if (ds == null)
                return Json(new { success = false, error = "DataSource bulunamadı veya pasif." });

            var rows = new List<object>();
            try
            {
                await using var conn = new SqlConnection(ds.ConnString);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand(optionsQuery, conn) { CommandTimeout = 15 };
                await using var reader = await cmd.ExecuteReaderAsync();
                int count = 0;
                while (await reader.ReadAsync() && count < 10)
                {
                    rows.Add(new
                    {
                        Value = reader["Value"]?.ToString() ?? "",
                        Label = reader["Label"]?.ToString() ?? ""
                    });
                    count++;
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = $"SQL hatası: {ex.Message}" });
            }

            return Json(new { success = true, rows, total = rows.Count });
        }
    }
}
