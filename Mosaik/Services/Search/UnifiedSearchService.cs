using Mosaik.Core.Module.Capabilities;

namespace Mosaik.Services.Search;

// M3 Cross-module Search — tüm ISearchProvider'ları toplayan scoped aggregator.
// Provider'lar DbContext kullandığı için Scoped (DI'da AddScoped).
public sealed class UnifiedSearchService(
    IEnumerable<ISearchProvider> providers,
    ILogger<UnifiedSearchService> logger)
{
    private const int MinQueryLength = 2;
    private const int OverallCap = 50;
    private const int PerProviderTake = 20;

    private readonly IEnumerable<ISearchProvider> _providers = providers;
    private readonly ILogger<UnifiedSearchService> _logger = logger;

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        string? query,
        int userId,
        ISet<string> roles,
        IReadOnlyList<int> firmaIds,
        CancellationToken ct)
    {
        var q = (query ?? string.Empty).Trim();
        if (q.Length < MinQueryLength)
        {
            return Array.Empty<SearchResult>();
        }

        // Her provider'ı ayrı sar — biri patlarsa diğerlerinin sonuçları gelsin (silent değil, LogError).
        var tasks = _providers.Select(p => SearchSafelyAsync(p, q, userId, roles, firmaIds, ct));
        var results = await Task.WhenAll(tasks);

        return results
            .SelectMany(r => r)
            .OrderByDescending(r => r.Date ?? DateTime.MinValue)
            .Take(OverallCap)
            .ToList();
    }

    private async Task<IReadOnlyList<SearchResult>> SearchSafelyAsync(
        ISearchProvider provider,
        string query,
        int userId,
        ISet<string> roles,
        IReadOnlyList<int> firmaIds,
        CancellationToken ct)
    {
        try
        {
            return await provider.SearchAsync(query, userId, roles, firmaIds, PerProviderTake, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Error (Warning değil): bozuk provider sessizce sonuç düşürür, kullanıcı eksik arama görür.
            // Alerting eşiğinin altında kalmasın.
            _logger.LogError(ex, "Search provider başarısız (sonuçlar eksik gösterilebilir): {ProviderKey}", provider.ProviderKey);
            return Array.Empty<SearchResult>();
        }
    }
}
