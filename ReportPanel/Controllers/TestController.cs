using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReportPanel.Models;
using ReportPanel.ViewModels;

namespace ReportPanel.Controllers
{
#if DEBUG
    // G-06: DEBUG olsa bile admin-only + CSRF koruma. Prod'da #if DEBUG ile butun sinif excluded.
    [Authorize(Roles = "admin")]
    public class TestController : Controller
    {
        private readonly ReportPanelContext _context;
        private readonly IConfiguration _configuration;

        public TestController(ReportPanelContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            var model = new TestViewModel();

            try
            {
                model.CanConnect = await _context.Database.CanConnectAsync();

                model.DataSourceCount = await _context.DataSources.CountAsync();
                model.ReportCount = await _context.ReportCatalog.CountAsync();
                model.LogCount = await _context.AuditLogs.CountAsync();

                model.DataSources = await _context.DataSources.ToListAsync();

                return View(model);
            }
            catch (Exception ex)
            {
                // M-02: DEBUG-only controller olsa bile disiplin icin generic mesaj.
                _ = ex;
                model.Error = "Veritabanı bağlantısı kurulamadı.";
                model.CanConnect = false;
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSampleData()
        {
            try
            {
                var sampleDataSource = new DataSource
                {
                    DataSourceKey = "MAIN",
                    Title = "Ana Veritabani",
                    ConnString = _configuration.GetConnectionString("DefaultConnection") ?? string.Empty,
                    IsActive = true
                };

                var existing = await _context.DataSources.FindAsync("MAIN");
                if (existing == null)
                {
                    _context.DataSources.Add(sampleDataSource);
                    await _context.SaveChangesAsync();
                }

                return Json(new { success = true, message = "Ornek veri eklendi" });
            }
            catch (Exception ex)
            {
                // M-02: generic mesaj.
                _ = ex;
                return Json(new { success = false, message = "Örnek veri eklenirken hata oluştu." });
            }
        }
    }
#endif
}
