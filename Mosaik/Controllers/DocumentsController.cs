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
        private readonly ILogger<DocumentsController> _logger;

        private static readonly byte[] PdfMagic = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        private static readonly byte[] OfficeMagic = new byte[] { 0x50, 0x4B, 0x03, 0x04 };

        public DocumentsController(
            MosaikContext db,
            ICurrentUserService currentUser,
            IWebHostEnvironment env,
            AiPipelineQueue queue,
            ILogger<DocumentsController> logger)
        {
            _db = db;
            _currentUser = currentUser;
            _env = env;
            _queue = queue;
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
                .Select(c => new { c.Id, c.Title })
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

            return RedirectToAction(nameof(Index), new { contractId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var firmas = FirmaIds;
            var file = await _db.ContractFiles.FirstOrDefaultAsync(f => f.Id == id && firmas.Contains(f.FirmaId));
            if (file is null) return NotFound();

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
    }
}
