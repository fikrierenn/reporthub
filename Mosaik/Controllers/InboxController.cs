using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mosaik.Services;
using Mosaik.Services.Inbox;

namespace Mosaik.Controllers
{
    // M2 Unified Inbox — tüm modüllerin "yapılacak iş" kalemlerini tek sayfada toplar.
    [Authorize]
    public class InboxController : Controller
    {
        private readonly UnifiedInboxService _inbox;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<InboxController> _logger;

        public InboxController(UnifiedInboxService inbox, ICurrentUserService currentUser, ILogger<InboxController> logger)
        {
            _inbox = inbox;
            _currentUser = currentUser;
            _logger = logger;
        }

        // GET /Inbox
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            // [Authorize] var → UserId claim'i olmalı. Null = invariant ihlali (auth/claim defekti), boş listeye coalesce edip maskeleme.
            if (_currentUser.UserId is not int userId)
            {
                _logger.LogWarning("Inbox: authenticated istek ama UserId claim yok.");
                return View(Array.Empty<Mosaik.Core.Module.Capabilities.InboxItem>());
            }
            var firmaIds = _currentUser.FirmaIds;
            var roles = User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var items = await _inbox.GetAllAsync(userId, roles, firmaIds, ct);
            return View(items);
        }
    }
}
