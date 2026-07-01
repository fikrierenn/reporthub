using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mosaik.Modules.Kvkk.Areas.Kvkk.ViewModels;
using Mosaik.Modules.Kvkk.Entities;
using Mosaik.Modules.Kvkk.Services;

namespace Mosaik.Modules.Kvkk.Areas.Kvkk.Controllers
{
    // Plan 54 M6 / Plan 40 Faz 7 — Risk Dashboard. Modül-içi EF-native (advisor kararı: B —
    // Reports/DashboardRenderer'a bağlanmaz, ADR-001/002 mixed-access + cross-modül kuplaj riski).
    [Area("Kvkk")]
    [Authorize(Roles = "admin")]
    public class DashboardController : Controller
    {
        private readonly DbContext _db;
        private readonly KvkkIntegrityFindingService _findings;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(DbContext db, KvkkIntegrityFindingService findings, ILogger<DashboardController> logger)
        {
            _db = db;
            _findings = findings;
            _logger = logger;
        }

        private int CurrentFirmaId =>
            int.TryParse(User.FindFirstValue("FirmaId"), out var id) ? id : 0;

        [HttpGet]
        public async Task<IActionResult> Risk(CancellationToken ct)
        {
            var firmaId = CurrentFirmaId;
            // silent-failure-hunter Faz 7: firmaId==0 fail-safe (boş sonuç) ama sessiz — claim
            // eksikliği/bozuk auth pipeline "0 süreç var" gibi görünüp fark edilmeyebilirdi.
            if (firmaId == 0)
                _logger.LogWarning("Kvkk Risk Dashboard: FirmaId claim yok/geçersiz, UserId={UserId}",
                    User.FindFirstValue(ClaimTypes.NameIdentifier));

            var processes = await _db.Set<KvkkProcess>().AsNoTracking()
                .Where(p => p.FirmaId == firmaId && p.IsActive)
                .Select(p => new { p.Department, p.RiskLevel })
                .ToListAsync(ct);

            var deptRisk = processes.GroupBy(p => p.Department)
                .Select(g => new DepartmentRiskRow(
                    g.Key,
                    g.Count(x => x.RiskLevel == 0),
                    g.Count(x => x.RiskLevel == 1),
                    g.Count(x => x.RiskLevel == 2)))
                .OrderByDescending(r => r.High).ThenByDescending(r => r.Mid).ThenBy(r => r.Department)
                .ToList();

            // Join zinciri (GroupBy+nested-nav yerine) — SQL çevirisi güvenilir (code-reviewer Faz 3 dersi).
            var topElements = await _db.Set<ProcessDataLink>().AsNoTracking()
                .Join(_db.Set<KvkkProcess>().Where(p => p.FirmaId == firmaId && p.IsActive),
                    l => l.ProcessId, p => p.Id, (l, p) => l.DataElementId)
                .Join(_db.Set<DataElement>(), id => id, e => e.Id, (id, e) => e.DisplayName)
                .GroupBy(name => name)
                .Select(g => new { DisplayName = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync(ct);

            var openFindings = await _findings.ListAsync(firmaId, includeDismissed: false, ct);
            var bySeverity = openFindings.GroupBy(f => f.Severity)
                .Select(g => new SeverityCount(g.Key, g.Count()))
                .OrderByDescending(s => s.Severity)
                .ToList();

            return View(new KvkkRiskDashboardViewModel(
                DepartmentRisk: deptRisk,
                TopDataElements: topElements.Select(x => new TopDataElementRow(x.DisplayName, x.Count)).ToList(),
                OpenFindingsBySeverity: bySeverity,
                TotalProcesses: processes.Count,
                TotalOpenFindings: openFindings.Count));
        }
    }
}
