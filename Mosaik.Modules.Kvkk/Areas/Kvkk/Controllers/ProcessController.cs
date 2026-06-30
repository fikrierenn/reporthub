using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Logging;
using Mosaik.Modules.Kvkk.Areas.Kvkk.ViewModels;
using Mosaik.Modules.Kvkk.Entities;
using Mosaik.Modules.Kvkk.Services;

namespace Mosaik.Modules.Kvkk.Areas.Kvkk.Controllers
{
    // Plan 54 M6 / Plan 40 Faz 2 — KVİE CRUD. Read (2a) + write (2b: Create/Edit/Delete/ReviewStatus).
    // Firma sınırı session claim'den; her write firma-scoped + audit.
    [Area("Kvkk")]
    [Authorize(Roles = "admin")]
    public class ProcessController : Controller
    {
        private readonly DbContext _db;
        private readonly KvkkProcessService _processes;
        private readonly VerbisExporter _verbis;
        private readonly IAuditLog _audit;

        public ProcessController(DbContext db, KvkkProcessService processes, VerbisExporter verbis, IAuditLog audit)
        {
            _db = db;
            _processes = processes;
            _verbis = verbis;
            _audit = audit;
        }

        private int CurrentFirmaId =>
            int.TryParse(User.FindFirstValue("FirmaId"), out var id) ? id : 0;
        private int CurrentUserId =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        [HttpGet]
        public async Task<IActionResult> Index(string? department = null, byte? risk = null, string? q = null)
        {
            var firmaId = CurrentFirmaId;
            var all = await _processes.ListByFirmaAsync(firmaId);

            ViewBag.Departments = all.Select(p => p.Department).Distinct().OrderBy(d => d).ToList();
            ViewBag.Department = department;
            ViewBag.Risk = risk;
            ViewBag.Query = q;

            IEnumerable<KvkkProcess> filtered = all;
            if (!string.IsNullOrWhiteSpace(department))
                filtered = filtered.Where(p => p.Department == department);
            if (risk.HasValue)
                filtered = filtered.Where(p => p.RiskLevel == risk.Value);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                filtered = filtered.Where(p =>
                    p.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (p.Purpose?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (p.Owner?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            return View(filtered.ToList());
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var p = await _processes.GetAsync(id, CurrentFirmaId);
            if (p == null)
                return NotFound();
            return View(p);
        }

        // VERBİS / denetim-hazır envanter Excel indir.
        [HttpGet]
        public async Task<IActionResult> ExportVerbis(CancellationToken ct)
        {
            var firmaId = CurrentFirmaId;
            if (firmaId <= 0)
                return Forbid();
            var bytes = await _verbis.ExportAsync(firmaId, ct);
            await AuditAsync("kvkk_verbis_export", 0, "VERBİS envanteri Excel export");
            var fileName = $"KVKK_Envanteri_{DateTime.UtcNow:yyyyMMdd}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var vm = new KvkkProcessFormViewModel();
            await PopulateOptionsAsync(vm);
            return View("Edit", vm);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var p = await _processes.GetAsync(id, CurrentFirmaId);
            if (p == null)
                return NotFound();

            var vm = new KvkkProcessFormViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Department = p.Department,
                Unit = p.Unit,
                Owner = p.Owner,
                Purpose = p.Purpose,
                LegalBasisId = p.LegalBasisId,
                ProcessingPurposeId = p.ProcessingPurposeId,
                RetentionRuleId = p.RetentionRuleId,
                DisposalMethodId = p.DisposalMethodId,
                DataSource = p.DataSource,
                StorageMedium = p.StorageMedium,
                AccessAuthority = p.AccessAuthority,
                RecipientGroups = p.RecipientGroups,
                RiskLevel = p.RiskLevel
            };
            await PopulateOptionsAsync(vm);
            return View("Edit", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(KvkkProcessFormViewModel form, CancellationToken ct)
        {
            var firmaId = CurrentFirmaId;

            if (!await _db.Set<LegalBasis>().AnyAsync(l => l.Id == form.LegalBasisId, ct))
                ModelState.AddModelError(nameof(form.LegalBasisId), "Geçersiz hukuki sebep.");

            if (!ModelState.IsValid)
            {
                await PopulateOptionsAsync(form);
                return View("Edit", form);
            }

            if (form.Id == 0)
            {
                var p = new KvkkProcess
                {
                    FirmaId = firmaId,
                    ReviewStatus = 0
                };
                MapForm(form, p);
                var r = await _processes.CreateAsync(p, ct);
                if (!r.IsSuccess)
                {
                    TempData["Message"] = r.Message;
                    TempData["MessageType"] = "error";
                    await PopulateOptionsAsync(form);
                    return View("Edit", form);
                }
                await AuditAsync("kvkk_process_create", p.Id, p.Name);
                TempData["Message"] = "Süreç oluşturuldu.";
                TempData["MessageType"] = "success";
                return RedirectToAction(nameof(Details), new { id = p.Id });
            }
            else
            {
                var p = await _db.Set<KvkkProcess>()
                    .FirstOrDefaultAsync(x => x.Id == form.Id && x.FirmaId == firmaId, ct);
                if (p == null)
                    return NotFound();
                MapForm(form, p);
                p.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
                await AuditAsync("kvkk_process_update", p.Id, p.Name);
                TempData["Message"] = "Süreç güncellendi.";
                TempData["MessageType"] = "success";
                return RedirectToAction(nameof(Details), new { id = p.Id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var p = await _db.Set<KvkkProcess>()
                .FirstOrDefaultAsync(x => x.Id == id && x.FirmaId == CurrentFirmaId, ct);
            if (p == null)
                return NotFound();
            // Soft-delete: KVKK kaydı denetim/kurtarma için korunur; liste IsActive filtreler.
            // (Hard-delete EntityRelations orphan bırakır + KVİE kaydını yok eder.)
            p.IsActive = false;
            p.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            await AuditAsync("kvkk_process_delete", id, p.Name);
            TempData["Message"] = "Süreç pasife alındı.";
            TempData["MessageType"] = "success";
            return RedirectToAction(nameof(Index));
        }

        // Onay durumu geçişi (Taslak 0 → Birim 1 → KVKK 2 → VERBİS 3). Admin set, audit'li.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetReviewStatus(int id, byte status, CancellationToken ct)
        {
            if (status > 3)
                return BadRequest();
            var p = await _db.Set<KvkkProcess>()
                .FirstOrDefaultAsync(x => x.Id == id && x.FirmaId == CurrentFirmaId, ct);
            if (p == null)
                return NotFound();
            p.ReviewStatus = status;
            p.LastReviewedAt = DateTime.UtcNow;
            p.LastReviewedBy = CurrentUserId;
            p.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            await AuditAsync("kvkk_process_review_status", id, $"{p.Name} → {KvkkLabels.ReviewStatus(status)}");
            TempData["Message"] = $"Onay durumu: {KvkkLabels.ReviewStatus(status)}.";
            TempData["MessageType"] = "success";
            return RedirectToAction(nameof(Details), new { id });
        }

        private static void MapForm(KvkkProcessFormViewModel f, KvkkProcess p)
        {
            p.Name = f.Name.Trim();
            p.Department = f.Department.Trim();
            p.Unit = f.Unit?.Trim();
            p.Owner = f.Owner?.Trim();
            p.Purpose = f.Purpose;
            p.LegalBasisId = f.LegalBasisId;
            p.ProcessingPurposeId = f.ProcessingPurposeId;
            p.RetentionRuleId = f.RetentionRuleId;
            p.DisposalMethodId = f.DisposalMethodId;
            p.DataSource = f.DataSource;
            p.StorageMedium = f.StorageMedium;
            p.AccessAuthority = f.AccessAuthority;
            p.RecipientGroups = f.RecipientGroups;
            p.RiskLevel = f.RiskLevel;
        }

        private async Task PopulateOptionsAsync(KvkkProcessFormViewModel vm)
        {
            vm.LegalBases = await _db.Set<LegalBasis>().AsNoTracking()
                .Where(x => x.IsActive).OrderBy(x => x.SortOrder)
                .Select(x => new KvkkProcessFormViewModel.Opt(x.Id, $"{x.Article} — {x.Name}")).ToListAsync();
            vm.ProcessingPurposes = await _db.Set<ProcessingPurpose>().AsNoTracking()
                .Where(x => x.IsActive).OrderBy(x => x.SortOrder)
                .Select(x => new KvkkProcessFormViewModel.Opt(x.Id, x.Name)).ToListAsync();
            vm.RetentionRules = await _db.Set<RetentionRule>().AsNoTracking()
                .Where(x => x.IsActive).OrderBy(x => x.SortOrder)
                .Select(x => new KvkkProcessFormViewModel.Opt(x.Id, $"{x.Name} ({x.DurationText})")).ToListAsync();
            vm.DisposalMethods = await _db.Set<DisposalMethod>().AsNoTracking()
                .Where(x => x.IsActive).OrderBy(x => x.SortOrder)
                .Select(x => new KvkkProcessFormViewModel.Opt(x.Id, x.Name)).ToListAsync();
        }

        private Task AuditAsync(string eventType, int id, string desc) =>
            _audit.LogAsync(eventType, "kvkk_process", id.ToString(), desc);
    }
}
