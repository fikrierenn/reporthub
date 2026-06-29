using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Module.Capabilities;
using Mosaik.Modules.Circular.Models;

namespace Mosaik.Modules.Circular.Services;

// M3 — Circular (tamim) araması.
// GÜVENLİK: CircularController.Index ile AYNI kapsam — tüm authenticated kullanıcılar yayınlanmış
// tamimleri görür (Circular firma sınırı YOKtur; şirket geneli günlük tamim zarfı). Sadece YAYINLANMIŞ
// tamim/blok aranır: bir blok ancak CircularId set edildiğinde yayına girer (DailyBlock.cs yorum).
// Bekleyen (CircularId IS NULL) bloklar aramaya sızmaz.
public sealed class CircularSearchProvider(DbContext db) : ISearchProvider
{
    private readonly DbContext _db = db;

    public string ProviderKey => "circular";

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        string query,
        int userId,
        ISet<string> roles,
        IReadOnlyList<int> firmaIds,
        int take,
        CancellationToken ct)
    {
        var like = $"%{query}%";

        // Zarf başlığında eşleşen + yayınlanmış bir bloğun konusunda eşleşen tamimleri topla.
        var byTitle = _db.Set<Models.Circular>().AsNoTracking()
            .Where(c => EF.Functions.Like(c.Title, like))
            .Select(c => c.Id);

        var byBlockSubject = _db.Set<DailyBlock>().AsNoTracking()
            .Where(b => b.CircularId != null && EF.Functions.Like(b.Subject, like))
            .Select(b => b.CircularId!.Value);

        var matchedIds = byTitle.Union(byBlockSubject);

        var rows = await _db.Set<Models.Circular>().AsNoTracking()
            .Where(c => matchedIds.Contains(c.Id))
            .OrderByDescending(c => c.PublishedAt)
            .Take(take)
            .Select(c => new { c.Id, c.Title, c.CircularNumber, c.PublishedAt })
            .ToListAsync(ct);

        return rows.Select(c => new SearchResult
        {
            ProviderKey = "circular",
            EntityLabel = "Tamim",
            Title = c.Title,
            Snippet = c.CircularNumber,
            Url = $"/Circular/Circular/Details/{c.Id}",
            Date = c.PublishedAt
        }).ToList();
    }
}
