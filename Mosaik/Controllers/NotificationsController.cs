using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Notification;
using Mosaik.Models;

namespace Mosaik.Controllers
{
    // Plan 17 Faz H — bildirim listesi + okundu işaretleme + sidebar badge endpoint'i.
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly INotificationService _notifications;
        private readonly MosaikContext _context;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(INotificationService notifications, MosaikContext context, ILogger<NotificationsController> logger)
        {
            _notifications = notifications;
            _context = context;
            _logger = logger;
        }

        private async Task<int?> CurrentUserIdAsync()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return null;
            return await _context.Users.AsNoTracking()
                .Where(u => u.Username == username)
                .Select(u => (int?)u.UserId)
                .FirstOrDefaultAsync();
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? filter = null, bool unreadOnly = false)
        {
            var uid = await CurrentUserIdAsync();
            if (uid == null) return Unauthorized();

            // Backward compat: ?unreadOnly=true → filter=unread.
            var f = (filter ?? (unreadOnly ? "unread" : "all"))?.ToLowerInvariant() ?? "all";

            // Read filter için tüm liste çek + bellekte filtrele (read endpoint yok service'te).
            var raw = await _notifications.GetRecentAsync(uid.Value, take: 200, unreadOnly: false);
            var items = f switch
            {
                "unread" => raw.Where(n => !n.IsRead).ToList(),
                "read" => raw.Where(n => n.IsRead).ToList(),
                _ => raw.ToList()
            };

            ViewData["Filter"] = f;
            return View(items);
        }

        // Sidebar badge polling endpoint — okunmamış sayısı.
        [HttpGet("/Notifications/UnreadCount")]
        public async Task<IActionResult> UnreadCount()
        {
            var uid = await CurrentUserIdAsync();
            if (uid == null) return Json(new { count = 0 });
            var count = await _notifications.GetUnreadCountAsync(uid.Value);
            return Json(new { count });
        }

        // Sidebar dropdown için JSON — son 10 bildirim.
        [HttpGet("/Notifications/Recent")]
        public async Task<IActionResult> Recent()
        {
            var uid = await CurrentUserIdAsync();
            if (uid == null) return Unauthorized();
            var items = await _notifications.GetRecentAsync(uid.Value, take: 10);
            return Json(items.Select(n => new
            {
                id = n.Id,
                title = n.Title,
                message = n.Message,
                targetUrl = n.TargetUrl,
                isRead = n.IsRead,
                createdAt = n.CreatedAt
            }));
        }

        // GET /Notifications/Open/{id} — tıklama: okundu işaretle + hedef URL'e redirect.
        // Idempotent. Bildirim sahibi olmayan → AccessDenied. TargetUrl yoksa Index'e.
        // Open redirect koruması: yalnız local URL (Url.IsLocalUrl + // prefix block).
        [HttpGet("/Notifications/Open/{id:int}")]
        public async Task<IActionResult> Open(int id)
        {
            var uid = await CurrentUserIdAsync();
            if (uid == null) return Unauthorized();

            var n = await _context.Notifications.AsNoTracking()
                .Where(x => x.Id == id && x.UserId == uid.Value)
                .Select(x => new { x.Id, x.TargetUrl })
                .FirstOrDefaultAsync();
            if (n is null)
            {
                _logger.LogWarning("NotificationsController.Open: not found or not owned id={Id} userId={UserId}", id, uid);
                return NotFound();
            }

            // markAsRead — fail olsa bile devam et (best-effort).
            try { await _notifications.MarkAsReadAsync(id, uid.Value); }
            catch (Exception ex) { _logger.LogWarning(ex, "NotificationsController.Open: markAsRead failed id={Id}", id); }

            var target = n.TargetUrl;
            if (string.IsNullOrWhiteSpace(target)
                || !Url.IsLocalUrl(target)
                || !target.StartsWith("/")
                || target.StartsWith("//"))
            {
                return RedirectToAction(nameof(Index));
            }
            return Redirect(target);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var uid = await CurrentUserIdAsync();
            if (uid == null) return Unauthorized();
            var ok = await _notifications.MarkAsReadAsync(id, uid.Value);
            if (!ok)
            {
                // Bildirim ya yok ya da başka kullanıcıya ait — IDOR probe sinyali.
                _logger.LogWarning("NotificationsController.MarkAsRead: notification not found or not owned id={Id} userId={UserId}", id, uid);
                if (Request.Headers.TryGetValue("X-Requested-With", out var v0) && v0 == "fetch")
                    return NotFound(new { success = false, message = "Bildirim bulunamadı." });
                TempData["Message"] = "Bildirim bulunamadı.";
                TempData["MessageType"] = "error";
                return RedirectToAction(nameof(Index));
            }
            if (Request.Headers.TryGetValue("X-Requested-With", out var v) && v == "fetch")
                return Ok(new { success = true });
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var uid = await CurrentUserIdAsync();
            if (uid == null) return Unauthorized();
            var n = await _notifications.MarkAllAsReadAsync(uid.Value);
            TempData["Message"] = $"{n} bildirim okundu olarak işaretlendi.";
            TempData["MessageType"] = "success";
            return RedirectToAction(nameof(Index));
        }
    }
}
