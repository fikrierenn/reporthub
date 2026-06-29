using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Module.Capabilities;
using Mosaik.Models;

namespace Mosaik.Services.Search;

// M3 — ReportCatalog (rapor) araması.
// GÜVENLİK: ReportsController.BuildReportsContext ile AYNI erişim kapsamı:
//   1) Aktif rapor + aktif veri kaynağı,
//   2) Kullanıcının UserRole junction rolleri ∩ ReportAllowedRoles (rol yoksa hiç sonuç),
//   3) "raporGrubu" FilterDefinition aktifse kullanıcının atanmış grup ID'lerine kısıt ('*' = tümü, 0 = deny).
// Kullanıcı aramada normal rapor listesinde göremeyeceği bir raporu asla görmemeli.
public sealed class ReportSearchProvider(MosaikContext db) : ISearchProvider
{
    private readonly MosaikContext _db = db;

    public string ProviderKey => "report";

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        string query,
        int userId,
        ISet<string> roles,
        IReadOnlyList<int> firmaIds,
        int take,
        CancellationToken ct)
    {
        var userRoleIds = await _db.UserRoles.AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync(ct);

        // Rol yoksa hiçbir rapora erişim yok (deny-by-default, BuildReportsContext ile aynı).
        if (userRoleIds.Count == 0)
        {
            return Array.Empty<SearchResult>();
        }

        var like = $"%{query}%";

        var q = _db.ReportCatalog.AsNoTracking()
            .Where(r => r.IsActive && r.DataSource != null && r.DataSource.IsActive)
            .Where(r => r.ReportAllowedRoles.Any(ar => userRoleIds.Contains(ar.RoleId)))
            .Where(r => EF.Functions.Like(r.Title, like)
                     || (r.Description != null && EF.Functions.Like(r.Description, like)));

        // raporGrubu (rapor grubu) erişim filtresi — BuildReportsContext ile aynı mantık.
        var raporGrubuActive = await _db.FilterDefinitions.AsNoTracking()
            .AnyAsync(fd => fd.IsActive && fd.FilterKey == "raporGrubu" && fd.DataSourceKey == null, ct);

        if (raporGrubuActive)
        {
            var raporGrubuFilters = await _db.UserDataFilters.AsNoTracking()
                .Where(f => f.UserId == userId && f.FilterKey == "raporGrubu" && f.DataSourceKey == null)
                .Select(f => f.FilterValue)
                .ToListAsync(ct);

            var hasStar = raporGrubuFilters.Contains("*");
            if (!hasStar)
            {
                var allowedGroupIds = raporGrubuFilters
                    .Select(v => int.TryParse(v, out var i) ? (int?)i : null)
                    .Where(x => x.HasValue)
                    .Select(x => x!.Value)
                    .ToHashSet();

                q = allowedGroupIds.Count == 0
                    ? q.Where(r => false)
                    : q.Where(r => r.ReportGroups.Any(rg => allowedGroupIds.Contains(rg.GroupId)));
            }
        }

        var rows = await q
            .OrderBy(r => r.Title)
            .Take(take)
            .Select(r => new { r.ReportId, r.Title, r.Description })
            .ToListAsync(ct);

        return rows.Select(r => new SearchResult
        {
            ProviderKey = "report",
            EntityLabel = "Rapor",
            Title = r.Title,
            Snippet = r.Description,
            Url = $"/Reports/Run/{r.ReportId}"
        }).ToList();
    }
}
