using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Mosaik.Models;
using Mosaik.Services;
using Mosaik.Services.Ai;
using Mosaik.Services.Contracts;
using Mosaik.Services.Workflow;
using Mosaik.ViewModels;

namespace Mosaik.Controllers
{
    [Authorize]
    public partial class ContractsController : Controller
    {
        private readonly MosaikContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly AiPipelineQueue _queue;
        private readonly IWebHostEnvironment _env;
        private readonly AuditLogService _auditLog;
        private readonly ILogger<ContractsController> _logger;
        private readonly IModuleService _modules;
        private readonly WorkflowInboxService _workflowInbox;

        public ContractsController(
            MosaikContext db,
            ICurrentUserService currentUser,
            AiPipelineQueue queue,
            IWebHostEnvironment env,
            AuditLogService auditLog,
            ILogger<ContractsController> logger,
            IModuleService modules,
            WorkflowInboxService workflowInbox)
        {
            _db = db;
            _currentUser = currentUser;
            _queue = queue;
            _env = env;
            _auditLog = auditLog;
            _logger = logger;
            _modules = modules;
            _workflowInbox = workflowInbox;
        }

        // N-2: DB-driven modül yetki kontrolü
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            await _modules.GetAllAsync();
            var role = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value ?? "";
            if (!_modules.IsAccessibleForRole("contracts", role))
            {
                context.Result = Forbid();
                return;
            }
            await next();
        }

        // Erişilebilir firma ID'leri. 0 öğe = modül kapalı.
        private IReadOnlyList<int> AccessibleFirmaIds => _currentUser.FirmaIds;
        private bool HasAccess(int firmaId) => AccessibleFirmaIds.Contains(firmaId);

        // GET /Contracts
        public async Task<IActionResult> Index(string? q, ContractStatus? status, int? firmaId)
        {
            var firmas = AccessibleFirmaIds;
            if (firmas.Count == 0)
            {
                TempData["Warning"] = "Hesabınıza henüz firma erişimi tanımlı değil. Yöneticiye başvurun.";
                return View(new List<Contract>());
            }

            var query = _db.Contracts
                .AsNoTracking()
                .Include(c => c.Firma)
                .Where(c => firmas.Contains(c.FirmaId));

            if (firmaId.HasValue && firmas.Contains(firmaId.Value))
                query = query.Where(c => c.FirmaId == firmaId.Value);

            if (status.HasValue)
                query = query.Where(c => c.Status == status.Value);

            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(c => c.Title.Contains(q) || (c.Counterparty != null && c.Counterparty.Contains(q)));

            var contracts = await query
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            ViewBag.StatusFilter = status;
            ViewBag.Q = q;
            ViewBag.FirmaFilter = firmaId;
            ViewBag.AccessibleFirmas = await GetAccessibleFirmasAsync();
            return View(contracts);
        }

        // GET /Contracts/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var firmas = AccessibleFirmaIds;
            if (firmas.Count == 0) return Forbid();

            var contract = await _db.Contracts
                .AsNoTracking()
                .Include(c => c.Firma)
                .Include(c => c.Obligations.OrderBy(o => o.DueDate))
                .Include(c => c.Files)
                .FirstOrDefaultAsync(c => c.Id == id && firmas.Contains(c.FirmaId));

            if (contract is null) return NotFound();

            var extractions = await _db.ContractAiExtractions
                .AsNoTracking()
                .Where(x => x.ContractId == id)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            ViewBag.Extractions = extractions;
            ViewBag.Workflows = await _workflowInbox.GetForEntityAsync("Contract", id);
            return View(contract);
        }

        // GET /Contracts/Create
        public async Task<IActionResult> Create()
        {
            var firmas = AccessibleFirmaIds;
            if (firmas.Count == 0) return Forbid();

            ViewBag.AccessibleFirmas = await GetAccessibleFirmasAsync();
            return View(new ContractCreateViewModel { FirmaId = firmas[0] });
        }

        // POST /Contracts/Create — sözleşme + (opsiyonel) periyodik yükümlülükler.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ContractCreateViewModel model)
        {
            if (!HasAccess(model.FirmaId)) return Forbid();

            if (!ModelState.IsValid)
            {
                ViewBag.AccessibleFirmas = await GetAccessibleFirmasAsync();
                return View(model);
            }

            var now = DateTime.UtcNow;
            var username = _currentUser.Username;

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var contract = new Contract
                {
                    FirmaId = model.FirmaId,
                    Title = model.Title,
                    Counterparty = model.Counterparty,
                    Category = model.Category,
                    Status = model.Status,
                    StartDate = model.StartDate,
                    EndDate = model.EndDate,
                    Notes = model.Notes,
                    ContractValue = model.ContractValue,
                    Currency = string.IsNullOrWhiteSpace(model.Currency) ? "TRY" : model.Currency,
                    GoverningLaw = model.GoverningLaw,
                    AutoRenewal = model.AutoRenewal,
                    KvkkInvolved = model.KvkkInvolved,
                    CreatedAt = now,
                    CreatedBy = username
                };
                _db.Contracts.Add(contract);
                await _db.SaveChangesAsync();

                int generatedObligationCount = 0;
                if (model.Recurrences.Count > 0)
                {
                    var contractStart = model.StartDate ?? DateOnly.FromDateTime(now);
                    var contractEnd = model.EndDate ?? contractStart.AddYears(1);

                    var sets = ContractObligationGenerator.Generate(
                        contractId: contract.Id,
                        firmaId: contract.FirmaId,
                        contractStart: contractStart,
                        contractEnd: contractEnd,
                        inputs: model.Recurrences,
                        createdBy: username);

                    foreach (var set in sets)
                    {
                        _db.ContractRecurrences.Add(set.Recurrence);
                        await _db.SaveChangesAsync();   // recurrence Id

                        foreach (var obl in set.Obligations)
                            obl.RecurrenceId = set.Recurrence.Id;

                        _db.ContractObligations.AddRange(set.Obligations);
                        generatedObligationCount += set.Obligations.Count;
                    }

                    if (generatedObligationCount > 0)
                        await _db.SaveChangesAsync();
                }

                await tx.CommitAsync();

                await _auditLog.LogAsync(new AuditLogEntry
                {
                    EventType = "contract_create",
                    TargetType = "contract",
                    TargetKey = contract.Id.ToString(),
                    Description = $"Sözleşme oluşturuldu: {contract.Title} ({generatedObligationCount} periyodik yükümlülük)",
                    IsSuccess = true,
                    NewValuesJson = AuditLogService.ToJson(new { contract.Id, contract.FirmaId, contract.Title, GeneratedObligations = generatedObligationCount })
                });

                TempData["Message"] = generatedObligationCount > 0
                    ? $"Sözleşme oluşturuldu. {generatedObligationCount} periyodik yükümlülük üretildi."
                    : "Sözleşme oluşturuldu.";

                return RedirectToAction(nameof(Details), new { id = contract.Id });
            }
            catch (ArgumentException ex)
            {
                await tx.RollbackAsync();
                _logger.LogWarning(ex, "Contract create domain validation fail. FirmaId={FirmaId}", model.FirmaId);
                ModelState.AddModelError(string.Empty, ex.Message);
                ViewBag.AccessibleFirmas = await GetAccessibleFirmasAsync();
                return View(model);
            }
            catch (DbUpdateException due) when (due.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx && sqlEx.Number == 547)
            {
                // FK violation — Firma silinmiş olabilir, kullanıcıya net mesaj.
                await tx.RollbackAsync();
                _logger.LogWarning(due, "Contract create FK fail for FirmaId={FirmaId}", model.FirmaId);
                ModelState.AddModelError(string.Empty,
                    "Seçtiğiniz firma artık erişilebilir değil. Sayfayı yenileyip tekrar deneyin.");
                ViewBag.AccessibleFirmas = await GetAccessibleFirmasAsync();
                return View(model);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Contract create failed for FirmaId={FirmaId}", model.FirmaId);
                TempData["Error"] = "Sözleşme kaydedilemedi. Lütfen tekrar deneyin.";
                ViewBag.AccessibleFirmas = await GetAccessibleFirmasAsync();
                return View(model);
            }
        }

        // GET /Contracts/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var firmas = AccessibleFirmaIds;
            if (firmas.Count == 0) return Forbid();

            var contract = await _db.Contracts.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id && firmas.Contains(c.FirmaId));

            if (contract is null) return NotFound();

            return View(new ContractEditViewModel
            {
                Id = contract.Id,
                Title = contract.Title,
                Counterparty = contract.Counterparty,
                Category = contract.Category,
                Status = contract.Status,
                StartDate = contract.StartDate,
                EndDate = contract.EndDate,
                Notes = contract.Notes
            });
        }

        // POST /Contracts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Contracts/Edit/{id}")]
        public async Task<IActionResult> Edit(int id, ContractEditViewModel model)
        {
            var firmas = AccessibleFirmaIds;
            if (firmas.Count == 0) return Forbid();

            if (!ModelState.IsValid)
            {
                model.Id = id;
                return View(model);
            }

            var contract = await _db.Contracts
                .FirstOrDefaultAsync(c => c.Id == id && firmas.Contains(c.FirmaId));

            if (contract is null) return NotFound();

            // FirmaId edit edilmez (güvenlik sınırı taşınmaz).
            contract.Title = model.Title;
            contract.Counterparty = model.Counterparty;
            contract.Category = model.Category;
            contract.Status = model.Status;
            contract.StartDate = model.StartDate;
            contract.EndDate = model.EndDate;
            contract.Notes = model.Notes;
            contract.UpdatedAt = DateTime.UtcNow;
            contract.UpdatedBy = _currentUser.Username;

            await _db.SaveChangesAsync();

            await _auditLog.LogAsync(new AuditLogEntry
            {
                EventType = "contract_update",
                TargetType = "contract",
                TargetKey = contract.Id.ToString(),
                Description = $"Sözleşme güncellendi: {contract.Title}",
                IsSuccess = true,
                NewValuesJson = AuditLogService.ToJson(new { contract.Id, contract.Title, contract.Category, contract.Status, contract.StartDate, contract.EndDate })
            });

            TempData["Message"] = "Sözleşme güncellendi.";
            return RedirectToAction(nameof(Details), new { id });
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

