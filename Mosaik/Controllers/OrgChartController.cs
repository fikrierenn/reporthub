using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mosaik.Services;
using Mosaik.ViewModels;

namespace Mosaik.Controllers
{
    // Plan 20 Faz C — Organizasyon şeması public görüntüleme.
    // Tüm aktif kullanıcılara açık (admin değil). Read-only — düzenleme /Admin/OrgChart'ta.
    // Zirve canlı incumbent dağılımı (cache 5dk TTL).
    [Authorize]
    public class OrgChartController : Controller
    {
        private readonly IOrgChartService _orgChart;
        private readonly ILogger<OrgChartController> _logger;

        public OrgChartController(IOrgChartService orgChart, ILogger<OrgChartController> logger)
        {
            _orgChart = orgChart;
            _logger = logger;
        }

        [HttpGet("/OrgChart")]
        public async Task<IActionResult> Index(string? view)
        {
            var allowedViews = new[] { "tree", "horizontal", "list" };
            var currentView = !string.IsNullOrWhiteSpace(view) && allowedViews.Contains(view) ? view! : "tree";

            try
            {
                var data = await _orgChart.GetChartWithIncumbentsAsync();
                return View(new OrgChartPublicViewModel
                {
                    Positions = data.Positions,
                    IncumbentsByCode = data.IncumbentsByCode,
                    UnmatchedIncumbents = data.UnmatchedIncumbents,
                    ZirveError = data.Error,
                    FetchedAtUtc = data.FetchedAtUtc,
                    CurrentView = currentView
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OrgChartController.Index failed");
                return View(new OrgChartPublicViewModel
                {
                    ZirveError = "Organizasyon şeması yüklenemedi."
                });
            }
        }
    }
}
