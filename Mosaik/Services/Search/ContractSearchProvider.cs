using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Module.Capabilities;
using Mosaik.Models;

namespace Mosaik.Services.Search;

// M3 — Contract (sözleşme) araması.
// GÜVENLİK: ContractsController ile AYNI kapsam — firmaIds.Contains(FirmaId) (multi-tenant sınırı).
// Kullanıcının firmaId claim'i yoksa hiç sonuç döner.
public sealed class ContractSearchProvider(MosaikContext db) : ISearchProvider
{
    private readonly MosaikContext _db = db;

    public string ProviderKey => "contract";

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

        var rows = await _db.Contracts.AsNoTracking()
            .Where(c => firmaIds.Contains(c.FirmaId))
            .Where(c => EF.Functions.Like(c.Title, like)
                     || (c.Counterparty != null && EF.Functions.Like(c.Counterparty, like)))
            .OrderByDescending(c => c.Id)
            .Take(take)
            .Select(c => new { c.Id, c.Title, c.Counterparty, c.StartDate })
            .ToListAsync(ct);

        return rows.Select(c => new SearchResult
        {
            ProviderKey = "contract",
            EntityLabel = "Sözleşme",
            Title = c.Title,
            Snippet = c.Counterparty,
            Url = $"/Contracts/Details/{c.Id}",
            Date = c.StartDate?.ToDateTime(TimeOnly.MinValue)
        }).ToList();
    }
}
