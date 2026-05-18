using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Domain;
using Mosaik.Models;
using Mosaik.Services;
using Mosaik.Services.Compliance;
using static Mosaik.Services.AuditLogService;

namespace Mosaik.Controllers
{
    [Authorize]
    public class ComplianceController : Controller
    {
        private readonly MosaikContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly AuditLogService _auditLog;
        private readonly IBusinessClock _clock;

        public ComplianceController(MosaikContext db, ICurrentUserService currentUser, AuditLogService auditLog, IBusinessClock clock)
        {
            _db = db;
            _currentUser = currentUser;
            _clock = clock;
            _auditLog = auditLog;
        }

        private IReadOnlyList<int> FirmaIds => _currentUser.FirmaIds;

        // GET /Compliance — Paket listesi (kart görünümü)
        public async Task<IActionResult> Index()
        {
            var packages = await _db.ComplianceTemplates
                .AsNoTracking()
                .Where(t => t.IsActive)
                .Select(t => t.PackageName)
                .Distinct()
                .OrderBy(p => p)
                .ToListAsync();

            return View(packages);
        }

        // GET /Compliance/Preview/{package}
        [HttpGet("Compliance/Preview/{package}")]
        public async Task<IActionResult> Preview(string package)
        {
            if (string.IsNullOrWhiteSpace(package)) return BadRequest();

            var templates = await _db.ComplianceTemplates
                .AsNoTracking()
                .Where(t => t.PackageName == package && t.IsActive)
                .OrderBy(t => t.Category)
                .ThenBy(t => t.Title)
                .ToListAsync();

            if (!templates.Any()) return NotFound();

            var firmas = FirmaIds;
            ViewBag.AccessibleFirmas = firmas.Count > 0
                ? await _db.Firmas.AsNoTracking().Where(f => firmas.Contains(f.FirmaId)).ToListAsync()
                : new List<Firma>();

            ViewBag.Package = package;
            return View(templates);
        }

        // POST /Compliance/Import — Şablonları seçilen firmaya yükümlülük olarak aktar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(string package, int firmaId, int[] templateIds)
        {
            var firmas = FirmaIds;
            if (!firmas.Contains(firmaId)) return Forbid();

            if (templateIds is null || templateIds.Length == 0)
            {
                TempData["Error"] = "En az bir şablon seçin.";
                return RedirectToAction(nameof(Preview), new { package });
            }

            var templates = await _db.ComplianceTemplates
                .AsNoTracking()
                .Where(t => templateIds.Contains(t.Id) && t.PackageName == package && t.IsActive)
                .ToListAsync();

            if (!templates.Any()) return BadRequest();

            // İlk vade hesabı Türkiye yerel takvimine göre — UTC kullanırsak
            // gece geç saatlerde "bugün" yanlış güne kayardı.
            var today = _clock.Today;
            var obligations = templates.Select(t => new ContractObligation
            {
                FirmaId      = firmaId,
                Title        = t.Title,
                Category     = t.Category,
                Type         = t.Type,
                DueDate      = ComplianceDueCalculator.ComputeFirstDue(t, today),
                Status       = ObligationStatus.Pending,
                Source       = ObligationSource.Manual,
                ReminderDays = t.ReminderDays,
                Notes        = t.Description
            }).ToList();

            _db.ContractObligations.AddRange(obligations);
            await _db.SaveChangesAsync();

            await _auditLog.LogAsync(new AuditLogEntry
            {
                Username    = _currentUser.Username ?? "",
                EventType   = "compliance_import",
                TargetType  = "compliance_package",
                TargetKey   = package,
                Description = $"{obligations.Count} şablon yükümlülük olarak eklendi"
            });

            TempData["Success"] = $"{obligations.Count} yükümlülük eklendi.";
            return RedirectToAction("Index", "Obligations");
        }
    }
}
