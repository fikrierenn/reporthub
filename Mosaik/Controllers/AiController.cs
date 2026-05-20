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

        // POST /Ai/AskDocument — Plan 27 Faz B-05: single-doc chat.
        // Body: { fileId: int, question: string } → JSON cevap.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AskDocument(int fileId, string question)
        {
            if (string.IsNullOrWhiteSpace(question))
                return Json(new { ok = false, error = "Soru boş." });

            var firmas = FirmaIds;
            var file = await _db.ContractFiles
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == fileId && firmas.Contains(f.FirmaId));
            if (file is null) return NotFound();

            // RawText önceliği: bu dosyaya bağlı en son AwaitingReview/Approved extraction'da varsa onu kullan.
            // Yoksa PdfPig ile fresh extract et.
            var extraction = await _db.ContractAiExtractions
                .AsNoTracking()
                .Where(x => x.ContractFileId == file.Id && !string.IsNullOrEmpty(x.RawText))
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            string? text = extraction?.RawText;
            if (string.IsNullOrWhiteSpace(text))
            {
                if (!file.MimeType.Contains("pdf", StringComparison.OrdinalIgnoreCase))
                    return Json(new { ok = false, error = "Bu dosya tipinden henüz metin çıkarılamadı." });

                var pdfX = HttpContext.RequestServices.GetRequiredService<Mosaik.Services.Ai.IPdfTextExtractor>();
                text = await pdfX.ExtractAsync(file.FilePath, CancellationToken.None);
            }
            if (string.IsNullOrWhiteSpace(text))
                return Json(new { ok = false, error = "Dokümandan metin çıkarılamadı." });

            var chat = HttpContext.RequestServices.GetRequiredService<Mosaik.Services.Ai.DocumentChatService>();
            var result = await chat.AskAsync(text!, file.FileName, question, HttpContext.RequestAborted);
            if (!result.IsSuccess)
                return Json(new { ok = false, error = result.Error });

            return Json(new { ok = true, answer = result.Answer, tokensIn = result.InputTokens, tokensOut = result.OutputTokens });
        }

        // POST /Ai/CreateContract/{id} — Plan 27 Faz A-08:
        // Extraction'daki AI çıkarımından Contract + Obligation taslakları oluştur.
        // Redirect: kullanıcı /Contracts/Edit/{newId} üzerinde düzeltir ve finalize eder.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateContract(int id)
        {
            var firmas = FirmaIds;
            var extraction = await _db.ContractAiExtractions
                .Include(x => x.ContractFile)
                .FirstOrDefaultAsync(x => x.Id == id && firmas.Contains(x.FirmaId));
            if (extraction is null) return NotFound();
            if (extraction.ContractId.HasValue)
            {
                TempData["Warning"] = "Bu çıkarım zaten bir sözleşmeye bağlı.";
                return RedirectToAction(nameof(Review), new { id });
            }
            if (string.IsNullOrWhiteSpace(extraction.ExtractionResultJson))
            {
                TempData["Warning"] = "AI çıkarımı henüz tamamlanmadı.";
                return RedirectToAction(nameof(Review), new { id });
            }

            // JSON'dan Contract alanlarını oku.
            string? title = null, counterparty = null, summary = null, category = null;
            string? startDate = null, endDate = null;
            var obligationDrafts = new List<(string Title, string? Type, decimal? Amount, string? Currency, string? DueDate, bool IsRecurring, string? RecurrenceType)>();
            try
            {
                using var doc = JsonDocument.Parse(extraction.ExtractionResultJson);
                var root = doc.RootElement;
                title = StrOrNull(root, "subject") ?? StrOrNull(root, "title");
                counterparty = StrOrNull(root, "counterparty");
                summary = StrOrNull(root, "summary");
                category = StrOrNull(root, "contractCategory");
                startDate = StrOrNull(root, "startDate");
                endDate = StrOrNull(root, "endDate");

                if (root.TryGetProperty("obligations", out var oArr) && oArr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var o in oArr.EnumerateArray())
                    {
                        var oTitle = StrOrNull(o, "title");
                        if (string.IsNullOrWhiteSpace(oTitle)) continue;
                        decimal? amount = o.TryGetProperty("amount", out var a) && a.ValueKind == JsonValueKind.Number && a.TryGetDecimal(out var d) ? d : null;
                        bool isRecurring = o.TryGetProperty("isRecurring", out var r) && r.ValueKind == JsonValueKind.True;
                        obligationDrafts.Add((oTitle!, StrOrNull(o, "type"), amount, StrOrNull(o, "currency"), StrOrNull(o, "dueDate"), isRecurring, StrOrNull(o, "recurrenceType")));
                    }
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "AI çıkarım JSON parse hatası: {Id}", id);
                TempData["Warning"] = "AI çıkarımı düzgün okunamadı.";
                return RedirectToAction(nameof(Review), new { id });
            }

            // Contract entity oluştur. Status=Draft, kullanıcı edit'te finalize eder.
            var contract = new Contract
            {
                FirmaId = extraction.FirmaId,
                Title = !string.IsNullOrWhiteSpace(title) ? Truncate(title!, 200) : (extraction.ContractFile?.FileName ?? "Yeni Sözleşme"),
                Counterparty = !string.IsNullOrWhiteSpace(counterparty) ? Truncate(counterparty!, 200) : null,
                Category = ParseCategory(category),
                Status = ContractStatus.Draft,
                StartDate = ParseDateOnly(startDate),
                EndDate = ParseDateOnly(endDate),
                Notes = !string.IsNullOrWhiteSpace(summary) ? Truncate(summary!, 2000) : null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = _currentUser.Username,
                UpdatedBy = _currentUser.Username
            };
            _db.Contracts.Add(contract);
            await _db.SaveChangesAsync();

            // ContractFile ile ilişkilendir + extraction'a bağla.
            if (extraction.ContractFile != null)
            {
                extraction.ContractFile.ContractId = contract.Id;
                extraction.ContractFile.UpdatedAt = DateTime.UtcNow;
            }
            extraction.ContractId = contract.Id;
            extraction.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            // Approved Obligation suggestion'larından gerçek ContractObligation oluştur.
            var approvedObligations = await _db.AiSuggestions
                .Where(s => s.ExtractionId == id
                            && s.SuggestionType == SuggestionType.Obligation
                            && s.Status == SuggestionStatus.Approved)
                .ToListAsync();

            var fallback = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));
            // Suggestion ↔ Obligation pair list — SaveChanges sonra cross-link kurulur.
            // Plan 33 BUGFIX-1 (2026-05-14): önceki kod `s.CreatedObligationId = -1` placeholder
            // bırakıyordu, gerçek Id asla yazılmıyordu — data integrity bug.
            var pairs = new List<(AiSuggestion Suggestion, ContractObligation Obligation)>(approvedObligations.Count);
            foreach (var s in approvedObligations)
            {
                // Suggestion DataJson'dan amount/dueDate/recurrence parse et
                decimal? amount = null;
                string currency = "TRY";
                DateOnly dueDate = fallback;
                if (!string.IsNullOrWhiteSpace(s.SuggestionDataJson))
                {
                    try
                    {
                        using var sd = JsonDocument.Parse(s.SuggestionDataJson);
                        var sr = sd.RootElement;
                        if (sr.TryGetProperty("amount", out var a) && a.ValueKind == JsonValueKind.Number && a.TryGetDecimal(out var dv)) amount = dv;
                        currency = StrOrNull(sr, "currency") ?? "TRY";
                        var ds = StrOrNull(sr, "dueDate");
                        var parsedDue = ParseDateOnly(ds);
                        if (parsedDue.HasValue) dueDate = parsedDue.Value;
                    }
                    catch (JsonException jex)
                    {
                        _logger.LogWarning(jex,
                            "ApplySuggestions: SuggestionId={Id} JSON parse fail — default değerlerle obligation oluşturuluyor.",
                            s.Id);
                    }
                }
                var ob = new ContractObligation
                {
                    FirmaId = extraction.FirmaId,
                    ContractId = contract.Id,
                    Title = Truncate(s.Title, 200),
                    Amount = amount,
                    Currency = currency,
                    DueDate = dueDate,
                    Status = ObligationStatus.Pending,
                    Source = ObligationSource.AiSuggested,
                    Notes = s.Description,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = _currentUser.Username,
                    UpdatedBy = _currentUser.Username
                };
                _db.ContractObligations.Add(ob);
                pairs.Add((s, ob));
            }
            // İlk SaveChanges: Obligation kayıtları yazılır, EF identity ile Id atanır.
            await _db.SaveChangesAsync();

            // İkinci SaveChanges: gerçek Obligation.Id'leri Suggestion'a cross-link et.
            foreach (var (suggestion, obligation) in pairs)
            {
                suggestion.CreatedObligationId = obligation.Id;
            }
            await _db.SaveChangesAsync();

            // Audit
            await _auditLog.LogAsync(new AuditLogEntry
            {
                Username = _currentUser.Username ?? "",
                EventType = "contract_created_from_ai",
                TargetType = "contract",
                TargetKey = contract.Id.ToString(),
                Description = $"AI çıkarımdan sözleşme taslağı oluşturuldu (Extraction #{id}, {approvedObligations.Count} yükümlülük)"
            });

            TempData["Success"] = $"Sözleşme taslağı oluşturuldu. Lütfen alanları kontrol edip kaydedin. {approvedObligations.Count} yükümlülük eklendi.";
            return RedirectToAction("Edit", "Contracts", new { id = contract.Id });
        }

        private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];

        private static DateOnly? ParseDateOnly(string? iso)
        {
            if (string.IsNullOrWhiteSpace(iso)) return null;
            return DateOnly.TryParse(iso, out var d) ? d : null;
        }

        private static ContractCategory ParseCategory(string? code) => code?.ToLowerInvariant() switch
        {
            "lease"      => ContractCategory.Lease,
            "service"    => ContractCategory.Service,
            "supply"     => ContractCategory.Supply,
            "employment" => ContractCategory.Employment,
            "license"    => ContractCategory.License,
            "insurance"  => ContractCategory.Insurance,
            _            => ContractCategory.Other
        };

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
