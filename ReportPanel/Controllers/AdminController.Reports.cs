using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReportPanel.Models;
using ReportPanel.Services;
using ReportPanel.ViewModels;

namespace ReportPanel.Controllers
{
    // Partial split (csharp-conventions hard-limit). Rapor CRUD + V2 builder redirects +
    // BuildReportFormViewModel.
    public partial class AdminController
    {
        [Route("Admin/CreateReport")]
        public async Task<IActionResult> CreateReport()
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
                var categories = await _context.ReportCategories
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
                    AvailableCategories = categories,
                    SelectedCategoryIds = new HashSet<int>()
                };

                // Debug bilgisi
                Console.WriteLine($"Toplam veri kaynagi: {allDataSources.Count}");
                Console.WriteLine($"Aktif veri kaynagi: {activeDataSources.Count}");

                foreach (var ds in allDataSources)
                {
                    Console.WriteLine($"- {ds.DataSourceKey}: {ds.Title} (Aktif: {ds.IsActive})");
                }

                if (!activeDataSources.Any())
                {
                    if (allDataSources.Any())
                    {
                        TempData["Message"] = $"Toplam {allDataSources.Count} veri kaynagi var ama hicbiri aktif degil. Veri kaynaklarini aktif hale getirin.";
                        TempData["MessageType"] = "warning";
                    }
                    else
                    {
                        TempData["Message"] = "Hic veri kaynagi bulunamadi. Once veri kaynagi eklemeniz gerekiyor.";
                        TempData["MessageType"] = "warning";
                    }
                }

                return View(model);
            }
            catch (Exception ex)
            {
                // M-02: generic mesaj, detay log'a.
                _ = ex;
                TempData["Message"] = "Veri kaynakları yüklenirken hata oluştu.";
                TempData["MessageType"] = "error";
                return View(new AdminReportFormViewModel
                {
                    Report = new ReportCatalog { IsActive = true, AllowedRoles = "admin" },
                    DataSources = new List<DataSource>(),
                    AvailableRoles = new List<Role>(),
                    SelectedRoleIds = new HashSet<int>(),
                    AvailableCategories = new List<ReportCategory>(),
                    SelectedCategoryIds = new HashSet<int>()
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
        // V2 — mockup app-shell-builder-v1.html'in birebir portu. V1 (EditReport/CreateReport) el değmez.
        // Aynı ViewModel, aynı POST endpoint'leri. Sadece view + assets farklı.
        [Route("Admin/CreateReportV2")]
        public Task<IActionResult> CreateReportV2() => CreateReport().ContinueWith(t =>
        {
            if (t.Result is ViewResult vr) vr.ViewName = "CreateReportV2";
            return t.Result;
        });

        [Route("Admin/EditReportV2/{id}")]
        public Task<IActionResult> EditReportV2(int id) => EditReport(id).ContinueWith(t =>
        {
            if (t.Result is ViewResult vr) vr.ViewName = "EditReportV2";
            return t.Result;
        });

        [Route("Admin/EditReport/{id}")]
        public async Task<IActionResult> EditReport(int id)
        {
            var report = await _context.ReportCatalog.FindAsync(id);
            if (report == null)
            {
                TempData["Message"] = "Rapor bulunamadi";
                TempData["MessageType"] = "error";
                return RedirectToAction("Index", new { tab = "reports" });
            }

            var dataSources = await _context.DataSources.AsNoTracking().Where(d => d.IsActive).ToListAsync();
            var roles = await _context.Roles
                .AsNoTracking()
                .Where(r => r.IsActive)
                .OrderBy(r => r.Name)
                .ToListAsync();
            var categories = await _context.ReportCategories
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
            var selectedRoleIds = await _context.ReportAllowedRoles
                .Where(ar => ar.ReportId == report.ReportId)
                .Select(ar => ar.RoleId)
                .ToListAsync();
            var selectedCategoryIds = await _context.ReportCategoryLinks
                .Where(rc => rc.ReportId == report.ReportId)
                .Select(rc => rc.CategoryId)
                .ToListAsync();
            var model = new AdminReportFormViewModel
            {
                Report = report,
                DataSources = dataSources,
                AvailableRoles = roles,
                SelectedRoleIds = selectedRoleIds.ToHashSet(),
                AvailableCategories = categories,
                SelectedCategoryIds = selectedCategoryIds.ToHashSet()
            };

            // Debug icin
            if (!dataSources.Any())
            {
                TempData["Message"] = "Aktif veri kaynagi bulunamadi. Once veri kaynagi eklemeniz gerekiyor.";
                TempData["MessageType"] = "warning";
            }

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/EditReport/{id}")]
        public async Task<IActionResult> EditReport(ReportCatalog report)
        {
            var input = BuildReportFormInput();
            var result = await _reportService.UpdateAsync(report.ReportId, input);
            if (result.Success)
            {
                TempData["Message"] = result.Message;
                TempData["MessageType"] = "success";
                return RedirectToAction("Index", new { tab = "reports" });
            }

            return View(await BuildReportFormViewModel(report, input, result.Message));
        }

        // Referans placeholder — asagidaki bloklar ayri actions'in bitimini mulayim tutmak icin.
        private async Task<AdminReportFormViewModel> BuildReportFormViewModel(ReportCatalog report, ReportFormInput input, string message)
        {
            var dataSources = await _context.DataSources.AsNoTracking().Where(d => d.IsActive).ToListAsync();
            var roles = await _context.Roles.AsNoTracking().Where(r => r.IsActive).OrderBy(r => r.Name).ToListAsync();
            var categories = await _context.ReportCategories.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
            return new AdminReportFormViewModel
            {
                Report = report,
                DataSources = dataSources,
                AvailableRoles = roles,
                SelectedRoleIds = input.SelectedRoleIds,
                AvailableCategories = categories,
                SelectedCategoryIds = input.SelectedCategoryIds,
                Message = message,
                MessageType = "error"
            };
        }
    }
}
