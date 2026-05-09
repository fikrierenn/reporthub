using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mosaik.Models;
using Mosaik.Services;

namespace Mosaik.Controllers
{
    [Authorize]
    public class ObligationsController : Controller
    {
        private readonly MosaikContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly AuditLogService _auditLog;

        public ObligationsController(MosaikContext db, ICurrentUserService currentUser, AuditLogService auditLog)
        {
            _db = db;
            _currentUser = currentUser;
            _auditLog = auditLog;
        }

        private IReadOnlyList<int> AccessibleFirmaIds => _currentUser.FirmaIds;
        private bool HasAccess(int firmaId) => AccessibleFirmaIds.Contains(firmaId);

        // GET /Obligations — vadesi yaklaşan tüm yükümlülükler (firma bazlı)
        public async Task<IActionResult> Index(ObligationStatus? status, int? firmaId)
        {
            var firmas = AccessibleFirmaIds;
            if (firmas.Count == 0)
            {
                TempData["Warning"] = "Hesabınıza henüz firma erişimi tanımlı değil.";
                return View(new List<ContractObligation>());
            }

            var query = _db.ContractObligations
                .AsNoTracking()
                .Include(o => o.Contract)
                .Where(o => firmas.Contains(o.FirmaId));

            if (firmaId.HasValue && firmas.Contains(firmaId.Value))
                query = query.Where(o => o.FirmaId == firmaId.Value);

            if (status.HasValue)
                query = query.Where(o => o.Status == status.Value);
            else
                query = query.Where(o => o.Status == ObligationStatus.Pending || o.Status == ObligationStatus.Overdue);

            var obligations = await query
                .OrderBy(o => o.DueDate)
                .ToListAsync();

            ViewBag.StatusFilter = status;
            ViewBag.FirmaFilter = firmaId;
            ViewBag.AccessibleFirmas = await GetAccessibleFirmasAsync();
            return View(obligations);
        }

        // GET /Obligations/Create?contractId=5
        public async Task<IActionResult> Create(int? contractId)
        {
            var firmas = AccessibleFirmaIds;
            if (firmas.Count == 0) return Forbid();

            int defaultFirmaId = firmas[0];
            string? contractTitle = null;

            if (contractId.HasValue)
            {
                var contract = await _db.Contracts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == contractId && firmas.Contains(c.FirmaId));
                if (contract is null) return NotFound();
                defaultFirmaId = contract.FirmaId;
                contractTitle = contract.Title;
            }

            ViewBag.ContractId = contractId;
            ViewBag.ContractTitle = contractTitle;
            ViewBag.AccessibleFirmas = await GetAccessibleFirmasAsync();

            return View(new ContractObligation
            {
                FirmaId = defaultFirmaId,
                ContractId = contractId,
                DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30))
            });
        }

        // POST /Obligations/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ContractObligation model)
        {
            if (!HasAccess(model.FirmaId)) return Forbid();

            // Sözleşmeye bağlıysa firma uyumu kontrolü
            if (model.ContractId.HasValue)
            {
                var owner = await _db.Contracts.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == model.ContractId.Value);
                if (owner is null || owner.FirmaId != model.FirmaId)
                    return Forbid();
            }

            if (model.DueDate == default)
                model.DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30));

            model.CreatedAt = DateTime.UtcNow;
            model.CreatedBy = _currentUser.Username;
            _db.ContractObligations.Add(model);
            await _db.SaveChangesAsync();

            await _auditLog.LogAsync(new AuditLogEntry
            {
                EventType = "obligation_create",
                TargetType = "contract_obligation",
                TargetKey = model.Id.ToString(),
                Description = $"Yükümlülük eklendi: {model.Title}",
                IsSuccess = true,
                NewValuesJson = AuditLogService.ToJson(new { model.Id, model.FirmaId, model.ContractId, model.Title, model.DueDate, model.Amount, model.Currency })
            });

            TempData["Message"] = "Yükümlülük eklendi.";
            return model.ContractId.HasValue
                ? RedirectToAction("Details", "Contracts", new { id = model.ContractId })
                : RedirectToAction(nameof(Index));
        }

        // POST /Obligations/Complete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id, string? returnUrl)
        {
            var firmas = AccessibleFirmaIds;
            if (firmas.Count == 0) return Forbid();

            var obligation = await _db.ContractObligations
                .FirstOrDefaultAsync(o => o.Id == id && firmas.Contains(o.FirmaId));

            if (obligation is null) return NotFound();

            obligation.Status = ObligationStatus.Completed;
            obligation.CompletedAt = DateTime.UtcNow;
            obligation.CompletedBy = _currentUser.Username;
            obligation.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await _auditLog.LogAsync(new AuditLogEntry
            {
                EventType = "obligation_complete",
                TargetType = "contract_obligation",
                TargetKey = obligation.Id.ToString(),
                Description = $"Yükümlülük tamamlandı: {obligation.Title}",
                IsSuccess = true
            });

            TempData["Message"] = "Yükümlülük tamamlandı olarak işaretlendi.";
            return RedirectToLocalSafe(returnUrl);
        }

        // POST /Obligations/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string? returnUrl)
        {
            var firmas = AccessibleFirmaIds;
            if (firmas.Count == 0) return Forbid();

            var obligation = await _db.ContractObligations
                .FirstOrDefaultAsync(o => o.Id == id && firmas.Contains(o.FirmaId));

            if (obligation is null) return NotFound();

            obligation.Status = ObligationStatus.Cancelled;
            obligation.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await _auditLog.LogAsync(new AuditLogEntry
            {
                EventType = "obligation_cancel",
                TargetType = "contract_obligation",
                TargetKey = obligation.Id.ToString(),
                Description = $"Yükümlülük iptal edildi: {obligation.Title}",
                IsSuccess = true
            });

            TempData["Message"] = "Yükümlülük iptal edildi.";
            return RedirectToLocalSafe(returnUrl);
        }

        // Open redirect guard — AuthController.RedirectToLocal pattern'i (security-principles.md kural #4).
        // Url.IsLocalUrl yetmez; protocol-relative ("//evil.com") ve "/\evil.com" varyantları block.
        private IActionResult RedirectToLocalSafe(string? returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl)
                && Url.IsLocalUrl(returnUrl)
                && returnUrl.StartsWith("/", StringComparison.Ordinal)
                && !returnUrl.StartsWith("//", StringComparison.Ordinal)
                && !returnUrl.StartsWith("/\\", StringComparison.Ordinal))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task<List<Firma>> GetAccessibleFirmasAsync()
        {
            var firmas = AccessibleFirmaIds;
            if (firmas.Count == 0) return new List<Firma>();
            return await _db.Firmas
                .AsNoTracking()
                .Where(f => firmas.Contains(f.FirmaId))
                .OrderBy(f => f.Ad)
                .ToListAsync();
        }
    }
}
