using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mosaik.Modules.SOP.Entities;
using Mosaik.Modules.SOP.Services;
using Mosaik.Modules.SOP.ViewModels;

namespace Mosaik.Modules.SOP.Areas.SOP.Controllers
{
    // Plan 34.1 Faz 4 A-20 — Cross-SOP RAG advisor controller.
    // GET /SOP/Advisor → Index (son 10 history + form + KVKK banner)
    // POST /SOP/Advisor/Ask → JSON cevap (Alpine fetch)
    [Area("SOP")]
    [Authorize]
    public class AdvisorController : Controller
    {
        private readonly SopRagAdvisorService _advisor;
        private readonly SopRateLimitGuard _rateLimit;
        private readonly DbContext _db;

        public AdvisorController(SopRagAdvisorService advisor, SopRateLimitGuard rateLimit, DbContext db)
        {
            _advisor = advisor;
            _rateLimit = rateLimit;
            _db = db;
        }

        private int CurrentUserId =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        private int CurrentFirmaId =>
            int.TryParse(User.FindFirstValue("FirmaId"), out var id) ? id : 0;

        private bool IsAdmin => User.IsInRole("admin");

        [HttpGet]
        public async Task<IActionResult> Index(int? sopId = null)
        {
            var userId = CurrentUserId;

            var history = await _db.Set<SopAiConversation>()
                .AsNoTracking()
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .Take(10)
                .ToListAsync();

            var quota = await _rateLimit.CheckAsync(userId, IsAdmin);

            // SOP detayından gelen context: başlık preselect (kullanıcı sorgu yazarken hint).
            string? sopHint = null;
            if (sopId.HasValue)
            {
                sopHint = await _db.Set<SopDocument>()
                    .AsNoTracking()
                    .Where(d => d.Id == sopId.Value)
                    .Select(d => d.Title)
                    .FirstOrDefaultAsync();
            }

            return View(new SopAdvisorViewModel
            {
                History = history.Select(SopAdvisorHistoryItem.From).ToList(),
                RemainingQuota = quota.RemainingQuota,
                IsAdmin = IsAdmin,
                SopHint = sopHint
            });
        }

        // Plan 34.1 Faz 5 A-27 — Admin thumbs-down dashboard.
        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> FeedbackReview(string? type = "down")
        {
            // Lookup sopAiFeedback: 1 ThumbsUp | 2 ThumbsDown
            byte target = string.Equals(type, "up", StringComparison.OrdinalIgnoreCase) ? (byte)1 : (byte)2;

            var rows = await _db.Set<SopAiConversation>()
                .AsNoTracking()
                .Where(c => c.UserFeedback == target)
                .OrderByDescending(c => c.FeedbackAt ?? c.CreatedAt)
                .Take(100)
                .Select(c => new SopAdvisorFeedbackRow
                {
                    Id = c.Id,
                    UserId = c.UserId,
                    Question = c.Question,
                    Answer = c.Answer,
                    FeedbackNote = c.FeedbackNote,
                    SourceVersionIds = c.SourceSopVersionIds,
                    CreatedAt = c.CreatedAt,
                    FeedbackAt = c.FeedbackAt
                })
                .ToListAsync();

            ViewBag.Type = target;
            return View(rows);
        }

        // Plan 34.1 Faz 5 A-25 — Thumbs-up/down feedback.
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Feedback(int conversationId, [FromForm] string feedback, [FromForm] string? note = null)
        {
            // Lookup sopAiFeedback: 1 ThumbsUp | 2 ThumbsDown
            byte parsed;
            if (string.Equals(feedback, "up", StringComparison.OrdinalIgnoreCase))
                parsed = 1;
            else if (string.Equals(feedback, "down", StringComparison.OrdinalIgnoreCase))
                parsed = 2;
            else
                return Json(new { success = false, error = "Geçersiz feedback değeri." });

            var result = await _advisor.SubmitFeedbackAsync(conversationId, CurrentUserId, parsed, note);
            return Json(new { success = result.IsSuccess, error = result.IsSuccess ? null : result.Message });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Ask([FromForm] string question)
        {
            if (string.IsNullOrWhiteSpace(question))
                return Json(new { success = false, error = "Soru boş olamaz." });

            var result = await _advisor.AskAsync(
                userId: CurrentUserId,
                firmaId: CurrentFirmaId == 0 ? null : CurrentFirmaId,
                isAdmin: IsAdmin,
                question: question.Trim());

            if (!result.IsSuccess)
                return Json(new { success = false, error = result.Message });

            var d = result.Data!;

            // Yeni eklenen conversation kaydını al (feedback için id lazım).
            var conversationId = await _db.Set<SopAiConversation>()
                .Where(c => c.UserId == CurrentUserId)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => c.Id)
                .FirstOrDefaultAsync();

            return Json(new
            {
                success = true,
                conversationId,
                answer = d.Answer,
                noHits = d.NoHits,
                sources = d.Sources.Select(s => new
                {
                    sopDocumentId = s.SopDocumentId,
                    sopVersionId = s.SopVersionId,
                    title = s.Title,
                    versionNumber = s.VersionNumber,
                    score = Math.Round(s.Score, 3)
                }).ToArray()
            });
        }
    }
}
