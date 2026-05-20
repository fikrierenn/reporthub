using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mosaik.Models;
using Mosaik.Services;
using static Mosaik.Services.AuditLogService;

namespace Mosaik.Controllers
{
    // Plan 39 Faz C-2 — AiController partial split.
    // Suggestion CRUD: kullanıcı AI önerilerini tek tek veya toplu onaylar/reddeder.
    [Authorize]
    public partial class AiController
    {
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
    }
}
