using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mosaik.Models;
using Mosaik.Services;

namespace Mosaik.Controllers
{
    public partial class ContractsController
    {
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
    }
}
