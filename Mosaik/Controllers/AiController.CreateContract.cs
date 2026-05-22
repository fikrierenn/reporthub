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
    // Plan 39 Faz C-2 — AiController partial split.
    // AI çıkarımdan Contract+Obligation taslakları üretme + DocumentChat ask.
    // Plan 27 Faz A-08 (CreateContract) + Plan 27 Faz B-05 (AskDocument).
    [Authorize]
    public partial class AiController
    {
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

                var pdfX = HttpContext.RequestServices.GetRequiredService<IPdfTextExtractor>();
                text = await pdfX.ExtractAsync(file.FilePath, CancellationToken.None);
            }
            if (string.IsNullOrWhiteSpace(text))
                return Json(new { ok = false, error = "Dokümandan metin çıkarılamadı." });

            var chat = HttpContext.RequestServices.GetRequiredService<DocumentChatService>();
            var result = await chat.AskAsync(text!, file.FileName, question, HttpContext.RequestAborted);
            if (!result.IsSuccess)
                return Json(new { ok = false, error = result.Error });

            // D-02-3 (2026-05-22): AI belge chat audit logu
            await _auditLog.LogAsync(
                eventType:    "ai_doc_chat",
                targetType:   "contract_file",
                targetKey:    fileId.ToString(),
                description:  $"AI belge sorusu: {file.FileName}",
                newValuesJson: System.Text.Json.JsonSerializer.Serialize(new
                {
                    fileId,
                    file.FileName,
                    QuestionLength = question.Length,
                    result.InputTokens,
                    result.OutputTokens
                })
            );

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
    }
}
