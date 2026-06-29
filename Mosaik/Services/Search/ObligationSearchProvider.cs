using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Module.Capabilities;
using Mosaik.Models;

namespace Mosaik.Services.Search;

// M3 — ContractObligation (yükümlülük) araması.
// GÜVENLİK: ObligationsController ile AYNI kapsam — firmaIds.Contains(FirmaId) (multi-tenant sınırı).
public sealed class ObligationSearchProvider(MosaikContext db) : ISearchProvider
{
    private readonly MosaikContext _db = db;

    public string ProviderKey => "obligation";

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        string query,
        int userId,
        ISet<string> roles,
        IReadOnlyList<int> firmaIds,
        int take,
        CancellationToken ct)
    {
        if (firmaIds.Count == 0) return Array.Empty<SearchResult>();

        var like = $"%{query}%";

        var rows = await _db.ContractObligations.AsNoTracking()
            .Where(o => firmaIds.Contains(o.FirmaId))
            .Where(o => EF.Functions.Like(o.Title, like))
            .OrderByDescending(o => o.DueDate)
            .Take(take)
            .Select(o => new { o.Title, o.DueDate })
            .ToListAsync(ct);

        return rows.Select(o => new SearchResult
        {
            ProviderKey = "obligation",
            EntityLabel = "Yükümlülük",
            Title = o.Title,
            Snippet = $"Vade: {o.DueDate:dd.MM.yyyy}",
            Url = "/Obligations",
            Date = o.DueDate.ToDateTime(TimeOnly.MinValue)
        }).ToList();
    }
}
