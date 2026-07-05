using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mosaik.Models;
using Mosaik.Services;
using Mosaik.ViewModels;

namespace Mosaik.Controllers
{
    // Partial split (csharp-conventions hard-limit). Rapor CRUD + V2 builder redirects +
    // BuildReportFormViewModel.
    public partial class AdminController
    {
        // V1 view (CreateReport.cshtml) silindi — V2 builder canonical. Bu GET method
        // V2 CreateReportV2 action'ından data-load için çağrılır. `private` = convention
        // route'la çakışmaz; eski URL CreateReportLegacyRedirect üzerinden V2'ye gider.
        private async Task<IActionResult> CreateReport()
        {
            try
            {
                // Tum veri kaynaklarini kontrol et
                var allDataSources = await _context.DataSources.AsNoTracking().ToListAsync();
                var activeDataSources = allDataSources.Where(d => d.IsActive).ToList();
                var roles = await _context.Roles
                    .AsNoTracking()
                    .Where(r => r.IsActive)
                    .OrderBy(r => r.Name)
                    .ToListAsync();
                var groups = await _context.ReportGroups
                    .AsNoTracking()
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                var model = new AdminReportFormViewModel
                {
                    Report = new ReportCatalog { IsActive = true, AllowedRoles = "admin" },
                    DataSources = activeDataSources,
                    AvailableRoles = roles,
                    SelectedRoleIds = new HashSet<int>(roles
                        .Where(r => string.Equals(r.Name, "admin", StringComparison.OrdinalIgnoreCase))
                        .Select(r => r.RoleId)),
                    AvailableGroups = groups,
                    SelectedGroupIds = new HashSet<int>()
                };

                _logger.LogDebug(
                    "AdminController.CreateReport: total={Total} active={Active}",
                    allDataSources.Count, activeDataSources.Count);

                if (!activeDataSources.Any())
                {
                    if (allDataSources.Any())
                    {
                        TempData["Message"] = $"Toplam {allDataSources.Count} veri kaynağı var ama hiçbiri aktif değil. Veri kaynaklarını aktif hale getirin.";
                        TempData["MessageType"] = "warning";
                    }
                    else
                    {
                        TempData["Message"] = "Hiç veri kaynağı bulunamadı. Önce veri kaynağı eklemeniz gerekiyor.";
                        TempData["MessageType"] = "warning";
                    }
                }

                return View(model);
            }
            catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
            {
                throw;
            }
            catch (Microsoft.Data.SqlClient.SqlException sex)
            {
                _logger.LogError(sex, "AdminController.CreateReport: SQL hatası");
                TempData["Message"] = "Veritabanı işleminde hata oluştu.";
                TempData["MessageType"] = "error";
                return View(new AdminReportFormViewModel
                {
                    Report = new ReportCatalog { IsActive = true, AllowedRoles = "admin" },
                    DataSources = new List<DataSource>(),
                    AvailableRoles = new List<Role>(),
                    SelectedRoleIds = new HashSet<int>(),
                    AvailableGroups = new List<ReportGroup>(),
                    SelectedGroupIds = new HashSet<int>()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AdminController.CreateReport: beklenmedik hata");
                TempData["Message"] = "Veri kaynakları yüklenirken hata oluştu.";
                TempData["MessageType"] = "error";
                return View(new AdminReportFormViewModel
                {
                    Report = new ReportCatalog { IsActive = true, AllowedRoles = "admin" },
                    DataSources = new List<DataSource>(),
                    AvailableRoles = new List<Role>(),
                    SelectedRoleIds = new HashSet<int>(),
                    AvailableGroups = new List<ReportGroup>(),
                    SelectedGroupIds = new HashSet<int>()
                });
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/CreateReport")]
        public async Task<IActionResult> CreateReport(ReportCatalog report)
        {
            var input = BuildReportFormInput();
            var result = await _reportService.CreateAsync(input);
            if (result.Success)
            {
                TempData["Message"] = result.Message;
                TempData["MessageType"] = "success";
                return RedirectToAction("Index", new { tab = "reports" });
            }

            return View(await BuildReportFormViewModel(report, input, result.Message));
        }
        // V2 — canonical Dashboard Builder. V1 view'ları silindi (CreateReport.cshtml,
        // EditReport.cshtml). V2 actions data load için private V1 GET method'larını
        // çağırır + view name'i V2'ye set eder.
        [Route("Admin/CreateReportV2")]
        public async Task<IActionResult> CreateReportV2()
        {
            var result = await CreateReport();
            if (result is ViewResult vr) vr.ViewName = "CreateReportV2";
            return result;
        }

        [Route("Admin/EditReportV2/{id}")]
        public async Task<IActionResult> EditReportV2(int id)
        {
            var result = await EditReport(id);
            if (result is ViewResult vr) vr.ViewName = "EditReportV2";
            return result;
        }

        // Eski V1 URL'lerine doğrudan girenleri V2'ye yönlendir (bookmarks vs)
        [Route("Admin/CreateReport")]
        [HttpGet]
        public IActionResult CreateReportLegacyRedirect() =>
            RedirectToAction(nameof(CreateReportV2));

        [Route("Admin/EditReport/{id:int}")]
        [HttpGet]
        public async Task<IActionResult> EditReportLegacyRedirect(int id)
        {
            try
            {
                var exists = await _context.ReportCatalog
                    .AsNoTracking()
                    .AnyAsync(r => r.ReportId == id);

                if (!exists)
                {
                    TempData["Message"] = "Aradığınız rapor bulunamadı (eski URL).";
                    TempData["MessageType"] = "warning";
                    return RedirectToAction("Index", new { tab = "reports" });
                }

                return RedirectToAction(nameof(EditReportV2), new { id });
            }
            catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
            {
                throw;
            }
            catch (Microsoft.Data.SqlClient.SqlException sex)
            {
                _logger.LogError(sex, "EditReport legacy redirect DB error for ReportId={Id}", id);
                TempData["Message"] = "Veritabanı erişiminde sorun var. Lütfen tekrar deneyin.";
                TempData["MessageType"] = "error";
                return RedirectToAction("Index", new { tab = "reports" });
            }
        }

        // V1 view (EditReport.cshtml) silindi — V2 canonical. Bu GET method V2'den
        // data-load için çağrılır. `private` = convention route'la çakışmaz; eski URL
        // EditReportLegacyRedirect üzerinden V2'ye gider.
        private async Task<IActionResult> EditReport(int id)
        {
            var report = await _context.ReportCatalog.FindAsync(id);
            if (report == null)
            {
                TempData["Message"] = "Rapor bulunamadı";
                TempData["MessageType"] = "error";
                return RedirectToAction("Index", new { tab = "reports" });
            }

            var dataSources = await _context.DataSources.AsNoTracking().Where(d => d.IsActive).ToListAsync();
            var roles = await _context.Roles
                .AsNoTracking()
                .Where(r => r.IsActive)
                .OrderBy(r => r.Name)
                .ToListAsync();
            var groups = await _context.ReportGroups
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
            var selectedRoleIds = await _context.ReportAllowedRoles
                .Where(ar => ar.ReportId == report.ReportId)
                .Select(ar => ar.RoleId)
                .ToListAsync();
            var selectedGroupIds = await _context.ReportGroupLinks
                .Where(rg => rg.ReportId == report.ReportId)
                .Select(rg => rg.GroupId)
                .ToListAsync();
            var model = new AdminReportFormViewModel
            {
                Report = report,
                DataSources = dataSources,
                AvailableRoles = roles,
                SelectedRoleIds = selectedRoleIds.ToHashSet(),
                AvailableGroups = groups,
                SelectedGroupIds = selectedGroupIds.ToHashSet()
            };

            // Debug icin
            if (!dataSources.Any())
            {
                TempData["Message"] = "Aktif veri kaynağı bulunamadı. Önce veri kaynağı eklemeniz gerekiyor.";
                TempData["MessageType"] = "warning";
            }

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/EditReport/{id}")]
        public async Task<IActionResult> EditReport(int id, ReportCatalog report)
        {
            var input = BuildReportFormInput();
            var result = await _reportService.UpdateAsync(id, input);
            if (result.Success)
            {
                TempData["Message"] = result.Message;
                TempData["MessageType"] = "success";
                return RedirectToAction("Index", new { tab = "reports" });
            }

            report.ReportId = id;
            return View(await BuildReportFormViewModel(report, input, result.Message));
        }

        // Referans placeholder — asagidaki bloklar ayri actions'in bitimini mulayim tutmak icin.
        private async Task<AdminReportFormViewModel> BuildReportFormViewModel(ReportCatalog report, ReportFormInput input, string message)
        {
            var dataSources = await _context.DataSources.AsNoTracking().Where(d => d.IsActive).ToListAsync();
            var roles = await _context.Roles.AsNoTracking().Where(r => r.IsActive).OrderBy(r => r.Name).ToListAsync();
            var groups = await _context.ReportGroups.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
            return new AdminReportFormViewModel
            {
                Report = report,
                DataSources = dataSources,
                AvailableRoles = roles,
                SelectedRoleIds = input.SelectedRoleIds,
                AvailableGroups = groups,
                SelectedGroupIds = input.SelectedGroupIds,
                Message = message,
                MessageType = "error"
            };
        }
    }
}
