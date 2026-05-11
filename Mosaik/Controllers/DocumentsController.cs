using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        private readonly ILogger<DocumentsController> _logger;

        private static readonly byte[] PdfMagic = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        private static readonly byte[] OfficeMagic = new byte[] { 0x50, 0x4B, 0x03, 0x04 };

        public DocumentsController(
            MosaikContext db,
            ICurrentUserService currentUser,
            IWebHostEnvironment env,
            AiPipelineQueue queue,
            IServiceScopeFactory scopeFactory,
            ILogger<DocumentsController> logger)
        {
            _db = db;
            _currentUser = currentUser;
            _env = env;
            _queue = queue;
            _scopeFactory = scopeFactory;
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
                query = query.Where(f => f.FileName.Contains(q));

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
        public async Task<IActionResult> Upload(IFormFile? file, int? contractId, int? obligationId)
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

            var ext      = Path.GetExtension(file.FileName).ToLowerInvariant();
            var safeName = $"{Guid.NewGuid():N}{ext}";
            var rel      = Path.Combine("uploads", "contracts", firmaId.ToString(), safeName);
            var abs      = Path.Combine(_env.WebRootPath, rel);

            Directory.CreateDirectory(Path.GetDirectoryName(abs)!);
            using (var fs = System.IO.File.Create(abs))
                await file.CopyToAsync(fs);

            var cf = new ContractFile
            {
                FirmaId     = firmaId,
                ContractId  = contractId,
                ObligationId= obligationId,
                FileName    = file.FileName,
                FilePath    = rel.Replace('\\', '/'),
                FileSize    = file.Length,
                MimeType    = file.ContentType,
                Version     = 1
            };

            _db.ContractFiles.Add(cf);
            await _db.SaveChangesAsync();

            // PDF ise AI extraction kaydı oluştur ve pipeline'a gönder
            if (isPdf)
            {
                var extraction = new ContractAiExtraction
                {
                    FirmaId       = firmaId,
                    ContractFileId= cf.Id,
                    ContractId    = contractId,
                    Status        = ExtractionStatus.Processing
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

            // Plan 27 Faz B-02 — auto-classify + summary fire-and-forget (PDF/DOCX dahil).
            // Sözleşme extraction'ı zaten Stage 1 özetini üretiyor; bu sadece liste tooltip için
            // hızlı bir özet + tag üretir. Hata fırlatmaz, kullanıcıyı bekletmez.
            _ = Task.Run(async () => await RunDocumentInsightAsync(cf.Id));

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

            var abs = Path.Combine(_env.WebRootPath, file.FilePath.Replace('/', Path.DirectorySeparatorChar));
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

                var result = await insight.AnalyzeAsync(text, cf.FileName, CancellationToken.None);
                if (result is null) return;

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
