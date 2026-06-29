using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mosaik.Core.Module.Capabilities;
using Mosaik.Services;
using Mosaik.Services.Search;
using Mosaik.ViewModels.Search;

namespace Mosaik.Controllers
{
    // M3 Cross-module Search — tüm modüllerin aranabilir kayıtlarını tek sayfada toplar.
    [Authorize]
    public class SearchController : Controller
    {
        private readonly UnifiedSearchService _search;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<SearchController> _logger;

        public SearchController(UnifiedSearchService search, ICurrentUserService currentUser, ILogger<SearchController> logger)
        {
            _search = search;
            _currentUser = currentUser;
            _logger = logger;
        }

        // GET /Search?q=...
        public async Task<IActionResult> Index(string? q, CancellationToken ct)
        {
            var query = (q ?? string.Empty).Trim();

            // [Authorize] var → UserId claim'i olmalı. Null = invariant ihlali (auth/claim defekti), boş sonuca coalesce.
            if (_currentUser.UserId is not int userId)
            {
                _logger.LogWarning("Search: authenticated istek ama UserId claim yok.");
                return View(new SearchResultsViewModel { Query = query });
            }

            // Boş/kısa sorgu → arama yapma, boş-durum prompt'u göster.
            if (query.Length < 2)
            {
                return View(new SearchResultsViewModel { Query = query });
            }

            var firmaIds = _currentUser.FirmaIds;
            var roles = User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var results = await _search.SearchAsync(query, userId, roles, firmaIds, ct);
            return View(new SearchResultsViewModel { Query = query, Results = results });
        }
    }
}
