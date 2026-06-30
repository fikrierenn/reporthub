using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mosaik.Core.Comments;
using Mosaik.Core.Users;
using Mosaik.Services;

namespace Mosaik.Controllers
{
    // Plan 54 M5 — yorum ekleme + @mention autocomplete.
    [Authorize]
    public class CommentsController : Controller
    {
        private readonly ICommentService _comments;
        private readonly IActiveUserDirectory _userDirectory;
        private readonly ICurrentUserService _currentUser;

        public CommentsController(
            ICommentService comments,
            IActiveUserDirectory userDirectory,
            ICurrentUserService currentUser)
        {
            _comments = comments;
            _userDirectory = userDirectory;
            _currentUser = currentUser;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(string entityType, int entityId, string body, CancellationToken ct)
        {
            var targetUrl = BuildTargetUrl(entityType, entityId);
            if (targetUrl == null)
                return BadRequest();

            var authorId = _currentUser.UserId ?? 0;
            var authorName = _currentUser.Username ?? "—";

            // Firma sınırı SERVİSTE: hedef varlığın gerçek firması kullanıcının erişimi
            // (FirmaIds) içinde mi — client'tan gelen firmaId'ye güvenilmez (multi-tenant).
            var result = await _comments.AddAsync(
                entityType, entityId, _currentUser.FirmaIds, authorId, authorName, body ?? "", targetUrl, ct);

            TempData["Message"] = result.IsSuccess ? "Yorum eklendi." : result.Message;
            TempData["MessageType"] = result.IsSuccess ? "success" : "error";
            return LocalRedirect(targetUrl + "#comments");
        }

        // @mention autocomplete — aktif kullanıcılar (kullanıcı adı + ad soyad), firma sınırlı.
        [HttpGet]
        public async Task<IActionResult> Users(string? q)
        {
            // Kullanıcı enumeration yüzeyini daralt: en az 2 karakterle ara.
            if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
                return Json(Array.Empty<object>());

            var firmaId = _currentUser.FirmaIds.FirstOrDefault();
            var users = await _userDirectory.GetActiveUsersAsync(firmaId == 0 ? null : firmaId);

            IEnumerable<ActiveUserInfo> filtered = users;
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                filtered = users.Where(u =>
                    u.Username.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    u.FullName.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            var list = filtered
                .Take(10)
                .Select(u => new { username = u.Username, fullName = u.FullName })
                .ToList();
            return Json(list);
        }

        // entityType → varlık detay rotası. User-input DEĞİL (sabit eşleme) → open-redirect yok.
        private static string? BuildTargetUrl(string entityType, int id) => entityType switch
        {
            "contract" => $"/Contracts/Details/{id}",
            "circular" => $"/Circular/Circular/Details/{id}",
            "sopDocument" => $"/SOP/Sop/Details/{id}",
            _ => null
        };
    }
}
