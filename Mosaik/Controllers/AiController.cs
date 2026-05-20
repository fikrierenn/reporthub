using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mosaik.Models;
using Mosaik.Services;
using Mosaik.Services.Ai;
using System.Text.Json;
using static Mosaik.Services.AuditLogService;

namespace Mosaik.Controllers
{
    // Plan 39 Faz C-2 — partial class split.
    //   AiController.Suggestions.cs    → ApproveSuggestion/RejectSuggestion/ApproveAll
    //   AiController.CreateContract.cs → AskDocument + CreateContract + helpers (Truncate/ParseDateOnly/ParseCategory)
    //   AiController.Wizard.cs         → wizard extraction trigger (önceki partial)
    // Bu ana dosya: ctor + DI + FirmaIds + extraction lifecycle (Index/Review/Detail/Cancel/Retry/Status)
    // + nested DTO (ExtractedFields/PartyInfo/KeyTerm) + StrOrNull helper (partial'lar arası paylaşılır).
    [Authorize]
    public partial class AiController : Controller
    {
        private readonly MosaikContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly AiPipelineQueue _queue;
        private readonly AuditLogService _auditLog;
        private readonly WizardExtractionService _wizard;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<AiController> _logger;

        public AiController(
            MosaikContext db,
            ICurrentUserService currentUser,
            AiPipelineQueue queue,
            AuditLogService auditLog,
            WizardExtractionService wizard,
            IWebHostEnvironment env,
            ILogger<AiController> logger)
        {
            _db = db;
            _currentUser = currentUser;
            _queue = queue;
            _auditLog = auditLog;
            _wizard = wizard;
            _env = env;
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

            // ExtractionResultJson'dan yapısal alanları parse et
            var fields = new ExtractedFields();
            if (!string.IsNullOrWhiteSpace(extraction.ExtractionResultJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(extraction.ExtractionResultJson);
                    var root = doc.RootElement;
                    fields.Summary         = StrOrNull(root, "summary");
                    fields.Subject         = StrOrNull(root, "subject");
                    fields.Counterparty    = StrOrNull(root, "counterparty");
                    fields.Category        = StrOrNull(root, "contractCategory") ?? StrOrNull(root, "category");
                    fields.ContractNumber  = StrOrNull(root, "contractNumber");
                    fields.SignedDate      = StrOrNull(root, "signedDate");
                    fields.StartDate       = StrOrNull(root, "startDate");
                    fields.EndDate         = StrOrNull(root, "endDate");
                    fields.TotalAmount     = root.TryGetProperty("totalAmount", out var ta) && ta.ValueKind == JsonValueKind.Number && ta.TryGetDecimal(out var d) ? d : null;
                    fields.Currency        = StrOrNull(root, "currency");
                    fields.PaymentTerms    = StrOrNull(root, "paymentTerms");
                    fields.Jurisdiction    = StrOrNull(root, "jurisdiction");

                    if (root.TryGetProperty("parties", out var parr) && parr.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var p in parr.EnumerateArray())
                        {
                            fields.Parties.Add(new PartyInfo(
                                Name: StrOrNull(p, "name") ?? "",
                                Role: StrOrNull(p, "role"),
                                TaxId: StrOrNull(p, "taxId"),
                                Address: StrOrNull(p, "address"),
                                Signatory: StrOrNull(p, "signatory"),
                                SignatoryTitle: StrOrNull(p, "signatoryTitle")));
                        }
                    }

                    if (root.TryGetProperty("keyTerms", out var karr) && karr.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var k in karr.EnumerateArray())
                        {
                            var t = StrOrNull(k, "title");
                            var det = StrOrNull(k, "detail");
                            if (!string.IsNullOrWhiteSpace(t))
                                fields.KeyTerms.Add(new KeyTerm(t!, det));
                        }
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "AI extraction JSON parse hatası: {Id}", id);
                }
            }

            ViewBag.Fields      = fields;
            ViewBag.Suggestions = suggestions;
            return View(extraction);
        }

        // Partial dosyalarda da kullanılır (CreateContract.cs JSON parse).
        private static string? StrOrNull(JsonElement el, string prop) =>
            el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(v.GetString())
                ? v.GetString()
                : null;

        public sealed class ExtractedFields
        {
            public string? Summary { get; set; }
            public string? Subject { get; set; }
            public string? Counterparty { get; set; }
            public string? Category { get; set; }
            public string? ContractNumber { get; set; }
            public string? SignedDate { get; set; }
            public string? StartDate { get; set; }
            public string? EndDate { get; set; }
            public decimal? TotalAmount { get; set; }
            public string? Currency { get; set; }
            public string? PaymentTerms { get; set; }
            public string? Jurisdiction { get; set; }
            public List<PartyInfo> Parties { get; } = new();
            public List<KeyTerm> KeyTerms { get; } = new();
        }

        public sealed record PartyInfo(string Name, string? Role, string? TaxId, string? Address, string? Signatory, string? SignatoryTitle);
        public sealed record KeyTerm(string Title, string? Detail);

        // GET /Ai/Detail/{id} — Plan 27 Faz A: ham metin + Stage 1 + Stage 2 görüntüle
        public async Task<IActionResult> Detail(int id)
        {
            var firmas = FirmaIds;
            var extraction = await _db.ContractAiExtractions
                .AsNoTracking()
                .Include(x => x.ContractFile)
                .Include(x => x.Contract)
                .FirstOrDefaultAsync(x => x.Id == id && firmas.Contains(x.FirmaId));
            if (extraction is null) return NotFound();
            return View(extraction);
        }

        // POST /Ai/Cancel/{id} — Plan 27 Faz A: takılan extraction'ı iptal et.
        // Worker'a doğrudan sinyal yok; sadece DB Status=Failed yap. Worker hâlâ
        // arka planda çalışıyorsa bittiğinde Status'u zaten over-write etmez (concurrency check).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var firmas = FirmaIds;
            var extraction = await _db.ContractAiExtractions
                .FirstOrDefaultAsync(x => x.Id == id && firmas.Contains(x.FirmaId));
            if (extraction is null) return NotFound();
            if (extraction.Status != ExtractionStatus.Processing)
            {
                TempData["Warning"] = "Sadece işlenmekte olan kayıtlar iptal edilebilir.";
                return RedirectToAction(nameof(Index));
            }

            extraction.Status = ExtractionStatus.Failed;
            extraction.ErrorMessage = $"Kullanıcı tarafından iptal edildi ({_currentUser.Username}).";
            extraction.ProgressStep = "cancelled";
            extraction.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await _auditLog.LogAsync(new AuditLogEntry
            {
                Username = _currentUser.Username ?? "",
                EventType = "ai_extraction_cancelled",
                TargetType = "ai_extraction",
                TargetKey = extraction.Id.ToString(),
                Description = "AI çıkarımı kullanıcı tarafından iptal edildi"
            });

            TempData["Success"] = "AI çıkarımı iptal edildi.";
            return RedirectToAction(nameof(Index));
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
                .Select(e => new { e.Id, e.FirmaId, e.Status, e.ProgressStep, e.ErrorMessage, e.ProgressTimestampsJson })
                .FirstOrDefaultAsync(e => e.Id == id && firmas.Contains(e.FirmaId));

            if (x is null) return NotFound();
            return Json(new
            {
                x.Status,
                statusLabel = StatusLabel(x.Status),
                x.ProgressStep,
                x.ErrorMessage,
                progressTimestamps = x.ProgressTimestampsJson
            });
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
