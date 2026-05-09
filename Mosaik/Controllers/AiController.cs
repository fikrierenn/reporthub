using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mosaik.Models;
using Mosaik.Services;
using Mosaik.Services.Ai;
using System.Text.Json;
using static Mosaik.Services.AuditLogService;

namespace Mosaik.Controllers
{
    [Authorize]
    public class AiController : Controller
    {
        private readonly MosaikContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly AiPipelineQueue _queue;
        private readonly AuditLogService _auditLog;
        private readonly ILogger<AiController> _logger;

        public AiController(
            MosaikContext db,
            ICurrentUserService currentUser,
            AiPipelineQueue queue,
            AuditLogService auditLog,
            ILogger<AiController> logger)
        {
            _db = db;
            _currentUser = currentUser;
            _queue = queue;
            _auditLog = auditLog;
            _logger = logger;
        }

        private IReadOnlyList<int> FirmaIds => _currentUser.FirmaIds;

        // GET /Ai — Tüm çıkarma işlemleri listesi
        public async Task<IActionResult> Index(ExtractionStatus? status)
        {
            var firmas = FirmaIds;
            if (firmas.Count == 0)
            {
                TempData["Warning"] = "Hesabınıza henüz firma erişimi tanımlı değil.";
                return View(new List<ContractAiExtraction>());
            }

            var query = _db.ContractAiExtractions
                .AsNoTracking()
                .Include(x => x.ContractFile)
                .Include(x => x.Contract)
                .Where(x => firmas.Contains(x.FirmaId));

            if (status.HasValue)
                query = query.Where(x => x.Status == status);

            var list = await query
                .OrderByDescending(x => x.CreatedAt)
                .Take(200)
                .ToListAsync();

            // Öneri sayılarını yükle
            var ids = list.Select(x => x.Id).ToList();
            var pendingCounts = await _db.AiSuggestions
                .Where(s => ids.Contains(s.ExtractionId) && s.Status == SuggestionStatus.Pending)
                .GroupBy(s => s.ExtractionId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Key, g => g.Count);

            ViewBag.PendingCounts = pendingCounts;
            ViewBag.StatusFilter = status;
            return View(list);
        }

        // GET /Ai/Review/{id}
        public async Task<IActionResult> Review(int id)
        {
            var firmas = FirmaIds;
            var extraction = await _db.ContractAiExtractions
                .Include(x => x.ContractFile)
                .Include(x => x.Contract)
                .FirstOrDefaultAsync(x => x.Id == id && firmas.Contains(x.FirmaId));

            if (extraction is null) return NotFound();

            var suggestions = await _db.AiSuggestions
                .AsNoTracking()
                .Where(s => s.ExtractionId == id)
                .OrderBy(s => s.SuggestionType)
                .ThenByDescending(s => s.Confidence)
                .ToListAsync();

            // ExtractionResultJson'dan özet parse et
            string? summary = null, counterparty = null, category = null;
            if (!string.IsNullOrWhiteSpace(extraction.ExtractionResultJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(extraction.ExtractionResultJson);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("summary",     out var s)) summary     = s.GetString();
                    if (root.TryGetProperty("counterparty",out var cp))counterparty= cp.GetString();
                    if (root.TryGetProperty("category",    out var cat))category   = cat.GetString();
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "AI extraction JSON parse hatası: {Id}", id);
                }
            }

            ViewBag.Summary     = summary;
            ViewBag.Counterparty= counterparty;
            ViewBag.Category    = category;
            ViewBag.Suggestions = suggestions;
            return View(extraction);
        }

        // POST /Ai/ApproveSuggestion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveSuggestion(int id)
        {
            var firmas = FirmaIds;
            var suggestion = await _db.AiSuggestions
                .Include(s => s.Extraction)
                .FirstOrDefaultAsync(s => s.Id == id && firmas.Contains(s.FirmaId));

            if (suggestion is null) return NotFound();
            if (suggestion.Status != SuggestionStatus.Pending)
                return BadRequest("Öneri zaten işleme alınmış.");

            suggestion.Status     = SuggestionStatus.Approved;
            suggestion.ApprovedBy = _currentUser.Username;
            suggestion.ApprovedAt = DateTime.UtcNow;
            suggestion.UpdatedAt  = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            await _auditLog.LogAsync(new AuditLogEntry
            {
                Username    = _currentUser.Username ?? "",
                EventType   = "ai_suggestion_approved",
                TargetType  = "ai_suggestion",
                TargetKey   = suggestion.Id.ToString(),
                Description = suggestion.Title
            });

            return RedirectToAction(nameof(Review), new { id = suggestion.ExtractionId });
        }

        // POST /Ai/RejectSuggestion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectSuggestion(int id)
        {
            var firmas = FirmaIds;
            var suggestion = await _db.AiSuggestions
                .Include(s => s.Extraction)
                .FirstOrDefaultAsync(s => s.Id == id && firmas.Contains(s.FirmaId));

            if (suggestion is null) return NotFound();
            if (suggestion.Status != SuggestionStatus.Pending)
                return BadRequest("Öneri zaten işleme alınmış.");

            suggestion.Status    = SuggestionStatus.Rejected;
            suggestion.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Review), new { id = suggestion.ExtractionId });
        }

        // POST /Ai/ApproveAll/{extractionId}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveAll(int id)
        {
            var firmas = FirmaIds;
            var extraction = await _db.ContractAiExtractions
                .FirstOrDefaultAsync(x => x.Id == id && firmas.Contains(x.FirmaId));
            if (extraction is null) return NotFound();

            var pending = await _db.AiSuggestions
                .Where(s => s.ExtractionId == id && s.Status == SuggestionStatus.Pending)
                .ToListAsync();

            var now = DateTime.UtcNow;
            foreach (var s in pending)
            {
                s.Status     = SuggestionStatus.Approved;
                s.ApprovedBy = _currentUser.Username;
                s.ApprovedAt = now;
                s.UpdatedAt  = now;
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = $"{pending.Count} öneri onaylandı.";
            return RedirectToAction(nameof(Review), new { id });
        }

        // POST /Ai/Retry/{extractionId}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Retry(int id)
        {
            var firmas = FirmaIds;
            var extraction = await _db.ContractAiExtractions
                .FirstOrDefaultAsync(x => x.Id == id && firmas.Contains(x.FirmaId));
            if (extraction is null) return NotFound();

            extraction.Status       = ExtractionStatus.Processing;
            extraction.ErrorMessage = null;
            extraction.UpdatedAt    = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await _queue.EnqueueAsync(extraction.Id);
            TempData["Success"] = "AI analizi yeniden kuyruğa alındı.";
            return RedirectToAction(nameof(Index));
        }

        // GET /Ai/Status/{id} — polling için JSON
        [HttpGet]
        public async Task<IActionResult> Status(int id)
        {
            var firmas = FirmaIds;
            var x = await _db.ContractAiExtractions
                .AsNoTracking()
                .Select(e => new { e.Id, e.FirmaId, e.Status, e.ProgressStep, e.ErrorMessage })
                .FirstOrDefaultAsync(e => e.Id == id && firmas.Contains(e.FirmaId));

            if (x is null) return NotFound();
            return Json(new { x.Status, statusLabel = StatusLabel(x.Status), x.ProgressStep, x.ErrorMessage });
        }

        private static string StatusLabel(ExtractionStatus s) => s switch
        {
            ExtractionStatus.Processing     => "İşleniyor",
            ExtractionStatus.AwaitingReview => "İnceleme Bekliyor",
            ExtractionStatus.Approved       => "Onaylandı",
            ExtractionStatus.Rejected       => "Reddedildi",
            ExtractionStatus.Failed         => "Hata",
            _                               => s.ToString()
        };
    }
}
