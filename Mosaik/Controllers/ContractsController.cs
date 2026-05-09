using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mosaik.Models;
using Mosaik.Services;
using Mosaik.Services.Ai;
using Mosaik.Services.Contracts;
using Mosaik.ViewModels;

namespace Mosaik.Controllers
{
    [Authorize]
    public class ContractsController : Controller
    {
        private readonly MosaikContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly AiPipelineQueue _queue;
        private readonly IWebHostEnvironment _env;
        private readonly AuditLogService _auditLog;
        private readonly ILogger<ContractsController> _logger;

        // Magic byte signatures for upload validation (H-3 hardening)
        private static readonly byte[] PdfMagic = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // %PDF
        private static readonly byte[] OfficeOpenMagic = new byte[] { 0x50, 0x4B, 0x03, 0x04 }; // PK ZIP (docx)
        private static readonly byte[] OldOfficeMagic = new byte[] { 0xD0, 0xCF, 0x11, 0xE0 }; // OLE compound (.doc)

        public ContractsController(
            MosaikContext db,
            ICurrentUserService currentUser,
            AiPipelineQueue queue,
            IWebHostEnvironment env,
            AuditLogService auditLog,
            ILogger<ContractsController> logger)
        {
            _db = db;
            _currentUser = currentUser;
            _queue = queue;
            _env = env;
            _auditLog = auditLog;
            _logger = logger;
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

        // POST /Contracts/UploadFile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadFile(int contractId, IFormFile file)
        {
            var firmas = AccessibleFirmaIds;
            if (firmas.Count == 0) return Forbid();

            var contract = await _db.Contracts
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == contractId && firmas.Contains(c.FirmaId));

            if (contract is null) return NotFound();

            if (file is null || file.Length == 0)
            {
                TempData["Error"] = "Dosya seçilmedi.";
                return RedirectToAction(nameof(Details), new { id = contractId });
            }

            if (file.Length > 20 * 1024 * 1024)
            {
                TempData["Error"] = "Dosya 20 MB sınırını aşıyor.";
                return RedirectToAction(nameof(Details), new { id = contractId });
            }

            var allowedMime = new[] { "application/pdf", "application/msword",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document" };

            if (!allowedMime.Contains(file.ContentType))
            {
                TempData["Error"] = "Yalnızca PDF ve Word dosyaları kabul edilir.";
                return RedirectToAction(nameof(Details), new { id = contractId });
            }

            // Magic byte verification — extension/MIME spoof'a karşı (H-3 hardening)
            if (!await VerifyMagicBytesAsync(file))
            {
                TempData["Error"] = "Dosya içeriği bildirilen tipiyle eşleşmiyor.";
                return RedirectToAction(nameof(Details), new { id = contractId });
            }

            // App_Data/contracts/{firmaId}/{contractId}/ — wwwroot DEĞİL (UseStaticFiles ile direkt erişim engelli).
            // Dosyaya erişim için Download endpoint scope check yapar.
            var uploadDir = Path.Combine(_env.ContentRootPath, "App_Data", "contracts",
                contract.FirmaId.ToString(), contractId.ToString());
            Directory.CreateDirectory(uploadDir);

            var safeName = SanitizeFileName(file.FileName);
            var diskPath = Path.Combine(uploadDir, safeName);
            // Path.Combine sonra GetFullPath ile root prefix kontrolü — defense-in-depth
            var resolvedPath = Path.GetFullPath(diskPath);
            var rootPrefix = Path.GetFullPath(uploadDir);
            if (!resolvedPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Contract upload path traversal attempt blocked: {Resolved}", resolvedPath);
                TempData["Error"] = "Dosya adı geçersiz.";
                return RedirectToAction(nameof(Details), new { id = contractId });
            }

            // Disk'e yaz + DB'ye kaydet — fail durumunda orphan dosyayı sil.
            try
            {
                await using (var fs = new FileStream(diskPath, FileMode.Create))
                    await file.CopyToAsync(fs);
            }
            catch (IOException ioex)
            {
                _logger.LogError(ioex, "Contract file upload disk write failed: {Path}", diskPath);
                TempData["Error"] = "Dosya kaydedilemedi (disk hatası). Lütfen tekrar deneyin.";
                return RedirectToAction(nameof(Details), new { id = contractId });
            }

            var contractFile = new ContractFile
            {
                FirmaId = contract.FirmaId,
                ContractId = contractId,
                FileName = file.FileName,
                FilePath = Path.GetRelativePath(_env.ContentRootPath, diskPath).Replace('\\', '/'),
                FileSize = file.Length,
                MimeType = file.ContentType,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _currentUser.Username
            };

            _db.ContractFiles.Add(contractFile);
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // DB'ye kayıt fail oldu → orphan dosyayı temizle (DB'siz dosya = data leak risk).
                _logger.LogError(ex, "Contract file DB save failed; cleaning orphan: {Path}", diskPath);
                try { System.IO.File.Delete(diskPath); }
                catch (IOException cleanupEx) { _logger.LogWarning(cleanupEx, "Orphan cleanup failed: {Path}", diskPath); }
                TempData["Error"] = "Dosya kaydı tamamlanamadı. Lütfen tekrar deneyin.";
                return RedirectToAction(nameof(Details), new { id = contractId });
            }

            await _auditLog.LogAsync(new AuditLogEntry
            {
                EventType = "contract_file_upload",
                TargetType = "contract_file",
                TargetKey = contractFile.Id.ToString(),
                Description = $"Sözleşme dosyası yüklendi: {file.FileName} ({file.Length} bayt)",
                IsSuccess = true,
                NewValuesJson = AuditLogService.ToJson(new { contractFile.Id, contractFile.FirmaId, contractFile.ContractId, contractFile.FileSize, contractFile.MimeType })
            });

            TempData["Message"] = "Dosya yüklendi.";
            return RedirectToAction(nameof(Details), new { id = contractId });
        }

        // GET /Contracts/Download/5 — yetkili kullanıcı için dosya indir (App_Data altındaki private storage).
        // wwwroot statik servisi dosyaları açığa çıkarmaz (H-3 hardening).
        public async Task<IActionResult> Download(int id)
        {
            var firmas = AccessibleFirmaIds;
            if (firmas.Count == 0) return Forbid();

            var contractFile = await _db.ContractFiles
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == id && firmas.Contains(f.FirmaId));

            if (contractFile is null) return NotFound();

            var fullPath = Path.GetFullPath(Path.Combine(_env.ContentRootPath, contractFile.FilePath.Replace('/', Path.DirectorySeparatorChar)));
            var allowedRoot = Path.GetFullPath(Path.Combine(_env.ContentRootPath, "App_Data", "contracts"));

            // Defense-in-depth: depolanan path'in App_Data/contracts altında olduğunu doğrula
            if (!fullPath.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("ContractFile.FilePath outside allowed root: {FilePath}", contractFile.FilePath);
                return NotFound();
            }

            if (!System.IO.File.Exists(fullPath))
            {
                _logger.LogWarning("ContractFile {Id} disk path missing: {Path}", id, fullPath);
                return NotFound();
            }

            return PhysicalFile(fullPath, contractFile.MimeType, contractFile.FileName);
        }

        // PDF / docx / doc magic byte check
        private static async Task<bool> VerifyMagicBytesAsync(IFormFile file)
        {
            using var stream = file.OpenReadStream();
            var header = new byte[4];
            var read = await stream.ReadAsync(header.AsMemory(0, 4));
            if (read < 4) return false;
            return StartsWith(header, PdfMagic) || StartsWith(header, OfficeOpenMagic) || StartsWith(header, OldOfficeMagic);
        }

        private static bool StartsWith(byte[] data, byte[] prefix)
        {
            if (data.Length < prefix.Length) return false;
            for (int i = 0; i < prefix.Length; i++)
                if (data[i] != prefix[i]) return false;
            return true;
        }

        // Dosya adını disk için güvenli hale getir: invalid chars, path traversal, NULL byte
        private static string SanitizeFileName(string original)
        {
            if (string.IsNullOrWhiteSpace(original)) original = "file";
            var name = Path.GetFileName(original);    // path component'leri at
            foreach (var ch in Path.GetInvalidFileNameChars())
                name = name.Replace(ch, '_');
            name = name.Replace('\0', '_');
            if (name.Length > 100) name = name[..100];
            var ext = Path.GetExtension(name);
            var stem = Path.GetFileNameWithoutExtension(name);
            return stem + "_" + Guid.NewGuid().ToString("N")[..8] + ext;
        }

        // POST /Contracts/StartAiExtraction
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartAiExtraction(int contractFileId)
        {
            var firmas = AccessibleFirmaIds;
            if (firmas.Count == 0) return Forbid();

            var file = await _db.ContractFiles
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == contractFileId && firmas.Contains(f.FirmaId));

            if (file is null) return NotFound();

            var active = await _db.ContractAiExtractions.AnyAsync(e =>
                e.ContractFileId == contractFileId &&
                (e.Status == ExtractionStatus.Processing || e.Status == ExtractionStatus.AwaitingReview));

            if (active)
            {
                TempData["Warning"] = "Bu dosya için zaten devam eden veya inceleme bekleyen bir analiz var.";
                return RedirectToAction(nameof(Details), new { id = file.ContractId });
            }

            var extraction = new ContractAiExtraction
            {
                FirmaId = file.FirmaId,
                ContractFileId = contractFileId,
                ContractId = file.ContractId,
                Status = ExtractionStatus.Processing,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _currentUser.Username,
                ProgressStep = "queued"
            };

            _db.ContractAiExtractions.Add(extraction);
            await _db.SaveChangesAsync();

            await _queue.EnqueueAsync(extraction.Id);

            _logger.LogInformation("AI extraction kuyruğa eklendi. ExtractionId={Id}, FileId={FileId}",
                extraction.Id, contractFileId);

            await _auditLog.LogAsync(new AuditLogEntry
            {
                EventType = "contract_ai_extraction_start",
                TargetType = "contract_ai_extraction",
                TargetKey = extraction.Id.ToString(),
                Description = $"AI sözleşme analizi başlatıldı (FileId={contractFileId})",
                IsSuccess = true
            });

            TempData["Message"] = "AI analizi başlatıldı. Tamamlandığında sonuç burada görünecek.";
            return RedirectToAction(nameof(Details), new { id = file.ContractId });
        }

        // POST /Contracts/ReviewExtraction
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewExtraction(int extractionId, string action)
        {
            var firmas = AccessibleFirmaIds;
            if (firmas.Count == 0) return Forbid();

            var extraction = await _db.ContractAiExtractions
                .FirstOrDefaultAsync(e => e.Id == extractionId && firmas.Contains(e.FirmaId));

            if (extraction is null) return NotFound();

            extraction.ReviewedBy = _currentUser.Username;
            extraction.ReviewedAt = DateTime.UtcNow;
            extraction.Status = action == "approve" ? ExtractionStatus.Approved : ExtractionStatus.Rejected;
            extraction.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            await _auditLog.LogAsync(new AuditLogEntry
            {
                EventType = action == "approve" ? "contract_ai_extraction_approve" : "contract_ai_extraction_reject",
                TargetType = "contract_ai_extraction",
                TargetKey = extraction.Id.ToString(),
                Description = $"AI sözleşme analizi {(action == "approve" ? "onaylandı" : "reddedildi")}",
                IsSuccess = true
            });

            TempData["Message"] = action == "approve" ? "Analiz onaylandı." : "Analiz reddedildi.";
            return RedirectToAction(nameof(Details), new { id = extraction.ContractId });
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
