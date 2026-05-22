using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Logging;
using Mosaik.Models;
using Mosaik.Services;
using Mosaik.Services.Ai;

namespace Mosaik.Controllers
{
    [Authorize]
    public class DocumentsController : Controller
    {
        private readonly MosaikContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly IWebHostEnvironment _env;
        private readonly AiPipelineQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHostApplicationLifetime _lifetime;
        private readonly IAuditLog _auditLog;
        private readonly ILogger<DocumentsController> _logger;

        private static readonly byte[] PdfMagic = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        private static readonly byte[] OfficeMagic = new byte[] { 0x50, 0x4B, 0x03, 0x04 };

        public DocumentsController(
            MosaikContext db,
            ICurrentUserService currentUser,
            IWebHostEnvironment env,
            AiPipelineQueue queue,
            IServiceScopeFactory scopeFactory,
            IHostApplicationLifetime lifetime,
            IAuditLog auditLog,
            ILogger<DocumentsController> logger)
        {
            _db = db;
            _currentUser = currentUser;
            _env = env;
            _queue = queue;
            _scopeFactory = scopeFactory;
            _lifetime = lifetime;
            _auditLog = auditLog;
            _logger = logger;
        }

        private IReadOnlyList<int> FirmaIds => _currentUser.FirmaIds;

        public async Task<IActionResult> Index(int? contractId, string? q)
        {
            var firmas = FirmaIds;
            if (firmas.Count == 0)
            {
                TempData["Warning"] = "Hesabınıza henüz firma erişimi tanımlı değil.";
                return View(new List<ContractFile>());
            }

            var query = _db.ContractFiles
                .AsNoTracking()
                .Include(f => f.Contract)
                .Include(f => f.Firma)
                .Where(f => firmas.Contains(f.FirmaId));

            if (contractId.HasValue)
                query = query.Where(f => f.ContractId == contractId);

            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(f => f.FileName.Contains(q) ||
                                        (f.ContentText != null && f.ContentText.Contains(q)));

            var files = await query.OrderByDescending(f => f.CreatedAt).Take(200).ToListAsync();

            ViewBag.Contracts = await _db.Contracts
                .AsNoTracking()
                .Where(c => firmas.Contains(c.FirmaId))
                .OrderBy(c => c.Title)
                .Select(c => new ValueTuple<int, string>(c.Id, c.Title))
                .ToListAsync();

            ViewBag.ContractFilter = contractId;
            ViewBag.Q = q;
            return View(files);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile? file, int? contractId, int? obligationId, int? replaceFileId)
        {
            var firmas = FirmaIds;
            if (firmas.Count == 0) return Forbid();

            if (file is null || file.Length == 0)
            {
                TempData["Error"] = "Dosya seçilmedi.";
                return RedirectToAction(nameof(Index));
            }

            if (file.Length > 50 * 1024 * 1024)
            {
                TempData["Error"] = "Dosya boyutu 50 MB sınırını aşıyor.";
                return RedirectToAction(nameof(Index));
            }

            var firmaId = firmas[0];

            // Sözleşme erişim kontrolü
            if (contractId.HasValue)
            {
                var ok = await _db.Contracts.AnyAsync(c => c.Id == contractId && firmas.Contains(c.FirmaId));
                if (!ok) return Forbid();
            }

            // Magic byte kontrolü
            byte[] header = new byte[4];
            await file.OpenReadStream().ReadExactlyAsync(header);
            bool isPdf    = header.Take(4).SequenceEqual(PdfMagic);
            bool isOffice = header.Take(4).SequenceEqual(OfficeMagic);
            if (!isPdf && !isOffice)
            {
                TempData["Error"] = "Yalnızca PDF ve Office belgeleri (.pdf, .docx, .xlsx) yüklenebilir.";
                return RedirectToAction(nameof(Index));
            }

            // Plan 33 BUGFIX-3 (2026-05-15): wwwroot/uploads/contracts → ContentRoot/App_Data/contracts.
            var ext       = Path.GetExtension(file.FileName).ToLowerInvariant();
            var safeName  = $"{Guid.NewGuid():N}{ext}";
            var uploadDir = Path.Combine(_env.ContentRootPath, "App_Data", "contracts", firmaId.ToString());
            Directory.CreateDirectory(uploadDir);

            var diskPath     = Path.Combine(uploadDir, safeName);
            var resolvedPath = Path.GetFullPath(diskPath);
            var rootPrefix   = Path.GetFullPath(uploadDir);
            if (!resolvedPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Document upload path traversal attempt blocked: {Resolved}", resolvedPath);
                TempData["Error"] = "Dosya adı geçersiz.";
                return RedirectToAction(nameof(Index));
            }

            using (var fs = System.IO.File.Create(diskPath))
                await file.CopyToAsync(fs);

            var newRelPath = Path.GetRelativePath(_env.ContentRootPath, diskPath).Replace('\\', '/');

            // Plan 27 Faz C — versiyonlama: mevcut dosyanın üzerine yazma.
            if (replaceFileId.HasValue)
            {
                var existing = await _db.ContractFiles
                    .FirstOrDefaultAsync(f => f.Id == replaceFileId && firmas.Contains(f.FirmaId));
                if (existing is null)
                {
                    TempData["Error"] = "Güncellenecek belge bulunamadı.";
                    return RedirectToAction(nameof(Index));
                }

                // Mevcut versiyonu arşivle
                var archived = new DocumentVersion
                {
                    ContractFileId = existing.Id,
                    VersionNumber  = existing.Version,
                    FilePath       = existing.FilePath,
                    FileName       = existing.FileName,
                    FileSize       = existing.FileSize,
                    MimeType       = existing.MimeType,
                    ArchivedAt     = DateTime.UtcNow,
                    ArchivedById   = _currentUser.UserId ?? 0
                };
                _db.DocumentVersions.Add(archived);

                // Yeni dosya bilgilerini güncelle
                existing.FileName        = file.FileName;
                existing.FilePath        = newRelPath;
                existing.FileSize        = file.Length;
                existing.MimeType        = file.ContentType;
                existing.Version         += 1;
                existing.ContentText     = null;  // AI pipeline yeniden dolduracak
                existing.AiSummary       = null;
                existing.AiTagsJson      = null;
                existing.AiClassifiedAt  = null;
                existing.UpdatedAt       = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                await _auditLog.LogAsync(
                    eventType:    "doc_version_upload",
                    targetType:   "contract_file",
                    targetKey:    existing.Id.ToString(),
                    description:  $"Belge versiyonlandı: {existing.FileName} → v{existing.Version}",
                    newValuesJson: System.Text.Json.JsonSerializer.Serialize(new
                    {
                        existing.Id,
                        existing.FileName,
                        Version = existing.Version,
                        ArchivedVersion = archived.VersionNumber
                    })
                );

                // PDF ise yeni versiyona extraction kuyruğa al
                if (isPdf)
                {
                    var extraction = new ContractAiExtraction
                    {
                        FirmaId        = firmaId,
                        ContractFileId = existing.Id,
                        ContractId     = existing.ContractId,
                        Status         = ExtractionStatus.Processing
                    };
                    _db.ContractAiExtractions.Add(extraction);
                    await _db.SaveChangesAsync();
                    await _queue.EnqueueAsync(extraction.Id);
                    TempData["Success"] = $"'{file.FileName}' v{existing.Version} yüklendi, AI analizi kuyruğa alındı.";
                }
                else
                {
                    TempData["Success"] = $"'{file.FileName}' v{existing.Version} yüklendi.";
                }

                _ = Task.Run(async () => await RunDocumentInsightAsync(existing.Id), _lifetime.ApplicationStopping);
                return RedirectToAction(nameof(Index), new { contractId = existing.ContractId });
            }

            // Normal yeni yükleme
            var cf = new ContractFile
            {
                FirmaId      = firmaId,
                ContractId   = contractId,
                ObligationId = obligationId,
                FileName     = file.FileName,
                FilePath     = newRelPath,
                FileSize     = file.Length,
                MimeType     = file.ContentType,
                Version      = 1
            };

            _db.ContractFiles.Add(cf);
            await _db.SaveChangesAsync();

            if (isPdf)
            {
                var extraction = new ContractAiExtraction
                {
                    FirmaId        = firmaId,
                    ContractFileId = cf.Id,
                    ContractId     = contractId,
                    Status         = ExtractionStatus.Processing
                };
                _db.ContractAiExtractions.Add(extraction);
                await _db.SaveChangesAsync();

                await _queue.EnqueueAsync(extraction.Id);
                TempData["Success"] = $"'{file.FileName}' yüklendi ve AI analizi kuyruğa alındı.";
            }
            else
            {
                TempData["Success"] = $"'{file.FileName}' başarıyla yüklendi.";
            }

            _ = Task.Run(async () => await RunDocumentInsightAsync(cf.Id), _lifetime.ApplicationStopping);

            return RedirectToAction(nameof(Index), new { contractId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var firmas = FirmaIds;
            var file = await _db.ContractFiles.FirstOrDefaultAsync(f => f.Id == id && firmas.Contains(f.FirmaId));
            if (file is null) return NotFound();

            // FK_ContractAiExt_Files: önce bağlı AI extraction'ları + suggestion'ları temizle.
            var extractions = await _db.ContractAiExtractions
                .Where(x => x.ContractFileId == file.Id)
                .ToListAsync();
            if (extractions.Count > 0)
            {
                var extIds = extractions.Select(e => e.Id).ToList();
                var suggestions = await _db.AiSuggestions.Where(s => extIds.Contains(s.ExtractionId)).ToListAsync();
                if (suggestions.Count > 0) _db.AiSuggestions.RemoveRange(suggestions);
                _db.ContractAiExtractions.RemoveRange(extractions);
            }

            // Plan 33 BUGFIX-3 (2026-05-15): WebRoot → ContentRoot (App_Data altı).
            var abs = Path.Combine(_env.ContentRootPath, file.FilePath.Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(abs))
            {
                try { System.IO.File.Delete(abs); }
                catch (Exception ex) { _logger.LogWarning(ex, "Dosya silinemedi: {Path}", abs); }
            }

            _db.ContractFiles.Remove(file);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Dosya silindi.";
            return RedirectToAction(nameof(Index));
        }

        // Plan 33 BUGFIX-3 (2026-05-15): App_Data altındaki private storage için
        // yetkili download endpoint. ContractsController.Download pattern reuse —
        // auth + firma check + path traversal guard. wwwroot statik servisi
        // dosyaları açığa çıkarmaz.
        public async Task<IActionResult> Download(int id)
        {
            var firmas = FirmaIds;
            if (firmas.Count == 0) return Forbid();

            var contractFile = await _db.ContractFiles
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == id && firmas.Contains(f.FirmaId));

            if (contractFile is null) return NotFound();

            var fullPath = Path.GetFullPath(Path.Combine(_env.ContentRootPath,
                contractFile.FilePath.Replace('/', Path.DirectorySeparatorChar)));
            var allowedRoot = Path.GetFullPath(Path.Combine(_env.ContentRootPath, "App_Data", "contracts"));

            // Defense-in-depth: path App_Data/contracts altında olmak zorunda.
            if (!fullPath.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("Document FilePath outside allowed root: {FilePath}", contractFile.FilePath);
                return NotFound();
            }

            if (!System.IO.File.Exists(fullPath))
            {
                _logger.LogWarning("Document {Id} disk path missing: {Path}", id, fullPath);
                return NotFound();
            }

            // Plan 27 Faz C — indirme audit logu
            await _auditLog.LogAsync(
                eventType:    "doc_download",
                targetType:   "contract_file",
                targetKey:    contractFile.Id.ToString(),
                description:  $"Belge indirildi: {contractFile.FileName}",
                newValuesJson: System.Text.Json.JsonSerializer.Serialize(new
                {
                    contractFile.Id,
                    contractFile.FileName,
                    contractFile.Version
                })
            );

            return PhysicalFile(fullPath, contractFile.MimeType, contractFile.FileName);
        }

        // Plan 27 Faz B-02 — fire-and-forget background AI insight.
        // PdfPig ile text extract → DocumentInsightService → DB'ye AiSummary + AiTagsJson yaz.
        // DI scope manuel oluşturulur (controller scope upload sonrası dispose olur).
        private async Task RunDocumentInsightAsync(int contractFileId)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var sp = scope.ServiceProvider;
                var db = sp.GetRequiredService<MosaikContext>();
                var insight = sp.GetRequiredService<DocumentInsightService>();
                var pdfX = sp.GetRequiredService<IPdfTextExtractor>();
                var env = sp.GetRequiredService<IWebHostEnvironment>();

                var cf = await db.ContractFiles.FirstOrDefaultAsync(f => f.Id == contractFileId);
                if (cf is null) return;

                // Sadece PDF için PdfPig text extract; diğer formatlar şimdilik atlanıyor.
                if (!cf.MimeType.Contains("pdf", StringComparison.OrdinalIgnoreCase)) return;

                var text = await pdfX.ExtractAsync(cf.FilePath, CancellationToken.None);
                if (string.IsNullOrWhiteSpace(text) || text.Length < 100)
                {
                    _logger.LogInformation("Insight: PDF text yetersiz ({Len} char), atlanıyor. FileId={Id}", text?.Length ?? 0, contractFileId);
                    return;
                }

                // Plan 27 Faz C — ContentText FTS için PDF metnini sakla (maks 50K karakter).
                cf.ContentText = text.Length > 50_000 ? text[..50_000] : text;

                var result = await insight.AnalyzeAsync(text, cf.FileName, CancellationToken.None);
                if (result is null)
                {
                    cf.UpdatedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync();
                    return;
                }

                cf.AiSummary = result.Summary;
                cf.AiTagsJson = result.TagsJson;
                cf.AiClassifiedAt = DateTime.UtcNow;
                cf.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
                _logger.LogInformation("Insight tamam: FileId={Id}, summary={SL} char, tokens={In}+{Out}",
                    contractFileId, result.Summary?.Length ?? 0, result.InputTokens, result.OutputTokens);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RunDocumentInsightAsync hata. FileId={Id}", contractFileId);
            }
        }
    }
}
