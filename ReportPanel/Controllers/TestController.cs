using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReportPanel.Models;
using ReportPanel.ViewModels;

namespace ReportPanel.Controllers
{
    public class TestController : Controller
    {
        private readonly ReportPanelContext _context;

        public TestController(ReportPanelContext context)
        {
            _context = context;
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
                model.Error = ex.Message;
                model.CanConnect = false;
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddSampleData()
        {
            try
            {
                var sampleDataSource = new DataSource
                {
                    DataSourceKey = "MAIN",
                    Title = "Ana Veritabani",
                    ConnString = "Server=localhost\\SQLEXPRESS;Database=PortalHUB;Integrated Security=true;TrustServerCertificate=true;",
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
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
