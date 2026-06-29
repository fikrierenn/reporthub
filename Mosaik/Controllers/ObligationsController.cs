using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Mosaik.Models;
using Mosaik.Services;
using Mosaik.ViewModels;

namespace Mosaik.Controllers
{
    [Authorize]
    public class ObligationsController : Controller
    {
        private readonly MosaikContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly AuditLogService _auditLog;
        private readonly IModuleService _modules;
        private readonly Mosaik.Core.Lookup.ILookupService _lookup;

        public ObligationsController(MosaikContext db, ICurrentUserService currentUser, AuditLogService auditLog, IModuleService modules, Mosaik.Core.Lookup.ILookupService lookup)
        {
            _db = db;
            _currentUser = currentUser;
            _auditLog = auditLog;
            _modules = modules;
            _lookup = lookup;
        }

        // N-2: DB-driven modül yetki kontrolü
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            await _modules.GetAllAsync();
            var role = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value ?? "";
            if (!_modules.IsAccessibleForRole("obligations", role))
            {
                context.Result = Forbid();
                return;
            }
            await next();
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
            ViewBag.ObligationCategories = await _lookup.GetValuesAsync("obligationCategory");
            ViewBag.ObligationTypes = await _lookup.GetValuesAsync("obligationType");

            return View(new ObligationCreateViewModel
            {
                FirmaId = defaultFirmaId,
                ContractId = contractId,
                DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30))
            });
        }

        // POST /Obligations/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ObligationCreateViewModel model)
        {
            if (!HasAccess(model.FirmaId)) return Forbid();

            if (!ModelState.IsValid)
            {
                ViewBag.ContractId = model.ContractId;
                ViewBag.ObligationCategories = await _lookup.GetValuesAsync("obligationCategory");
                ViewBag.ObligationTypes = await _lookup.GetValuesAsync("obligationType");
                return View(model);
            }

            // Sözleşmeye bağlıysa firma uyumu kontrolü
            if (model.ContractId.HasValue)
            {
                var owner = await _db.Contracts.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == model.ContractId.Value);
                if (owner is null || owner.FirmaId != model.FirmaId)
                    return Forbid();
            }

            var obligation = new ContractObligation
            {
                FirmaId = model.FirmaId,
                ContractId = model.ContractId,
                Title = model.Title,
                Category = model.Category,
                Type = model.Type,
                DueDate = model.DueDate == default ? DateOnly.FromDateTime(DateTime.Today.AddDays(30)) : model.DueDate,
                ReminderDays = model.ReminderDays,
                Amount = model.Amount,
                Currency = model.Currency,
                Notes = model.Notes,
                Status = ObligationStatus.Pending,
                Source = ObligationSource.Manual,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _currentUser.Username
            };

            _db.ContractObligations.Add(obligation);
            await _db.SaveChangesAsync();

            await _auditLog.LogAsync(new AuditLogEntry
            {
                EventType = "obligation_create",
                TargetType = "contract_obligation",
                TargetKey = obligation.Id.ToString(),
                Description = $"Yükümlülük eklendi: {obligation.Title}",
                IsSuccess = true,
                NewValuesJson = AuditLogService.ToJson(new { obligation.Id, obligation.FirmaId, obligation.ContractId, obligation.Title, obligation.DueDate, obligation.Amount, obligation.Currency })
            });

            TempData["Message"] = "Yükümlülük eklendi.";
            return obligation.ContractId.HasValue
                ? RedirectToAction("Details", "Contracts", new { id = obligation.ContractId })
                : RedirectToAction(nameof(Index));
        }

        // GET /Obligations/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var firmas = AccessibleFirmaIds;
            if (firmas.Count == 0) return Forbid();

            var obligation = await _db.ContractObligations
                .AsNoTracking()
                .Include(o => o.Contract)
                .FirstOrDefaultAsync(o => o.Id == id && firmas.Contains(o.FirmaId));

            if (obligation is null) return NotFound();

            var model = new ObligationEditViewModel
            {
                Id              = obligation.Id,
                FirmaId         = obligation.FirmaId,
                ContractId      = obligation.ContractId,
                ContractTitle   = obligation.Contract?.Title,
                CurrentStatus   = obligation.Status,
                Title           = obligation.Title,
                Category        = obligation.Category,
                Type            = obligation.Type,
                DueDate         = obligation.DueDate,
                ReminderDays    = obligation.ReminderDays ?? 0,
                Amount          = obligation.Amount,
                Currency        = obligation.Currency ?? "TRY",
                Notes           = obligation.Notes
            };

            ViewBag.AccessibleFirmas = await GetAccessibleFirmasAsync();
            ViewBag.ObligationCategories = await _lookup.GetValuesAsync("obligationCategory");
            ViewBag.ObligationTypes = await _lookup.GetValuesAsync("obligationType");
            return View(model);
        }

        // POST /Obligations/Edit/5
        [HttpPost]
        [Route("Obligations/Edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ObligationEditViewModel model)
        {
            var firmas = AccessibleFirmaIds;
            if (firmas.Count == 0) return Forbid();

            var obligation = await _db.ContractObligations
                .FirstOrDefaultAsync(o => o.Id == id && firmas.Contains(o.FirmaId));

            if (obligation is null) return NotFound();

            if (!ModelState.IsValid)
            {
                model.Id = id;
                model.CurrentStatus = obligation.Status;
                model.ContractTitle = obligation.Contract?.Title;
                ViewBag.AccessibleFirmas = await GetAccessibleFirmasAsync();
                ViewBag.ObligationCategories = await _lookup.GetValuesAsync("obligationCategory");
                ViewBag.ObligationTypes = await _lookup.GetValuesAsync("obligationType");
                return View(model);
            }

            obligation.Title        = model.Title;
            obligation.Category     = model.Category;
            obligation.Type         = model.Type;
            obligation.DueDate      = model.DueDate;
            obligation.ReminderDays = model.ReminderDays;
            obligation.Amount       = model.Amount;
            obligation.Currency     = model.Currency;
            obligation.Notes        = model.Notes;
            obligation.UpdatedAt    = DateTime.UtcNow;
            // ReminderSentAt sıfırla — tarih değiştiyse yeniden bildirim gönderilebilsin.
            if (obligation.DueDate != model.DueDate)
                obligation.ReminderSentAt = null;

            await _db.SaveChangesAsync();

            await _auditLog.LogAsync(new AuditLogEntry
            {
                EventType = "obligation_edit",
                TargetType = "contract_obligation",
                TargetKey = obligation.Id.ToString(),
                Description = $"Yükümlülük düzenlendi: {obligation.Title}",
                IsSuccess = true,
                NewValuesJson = AuditLogService.ToJson(new { obligation.Id, obligation.Title, obligation.DueDate, obligation.Amount, obligation.Currency })
            });

            TempData["Message"] = "Yükümlülük güncellendi.";
            return obligation.ContractId.HasValue
                ? RedirectToAction("Details", "Contracts", new { id = obligation.ContractId })
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
                .OrderBy(f => f.Name)
                .ToListAsync();
        }
    }
}

