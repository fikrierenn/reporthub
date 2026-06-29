using Mosaik.Core.Module.Capabilities;

namespace Mosaik.Services.Inbox;

// M2 Unified Inbox — tüm IInboxProvider'ları toplayan scoped aggregator.
// Provider'lar DbContext kullandığı için Scoped (DI'da AddScoped).
public sealed class UnifiedInboxService(
    IEnumerable<IInboxProvider> providers,
    ILogger<UnifiedInboxService> logger)
{
    private readonly IEnumerable<IInboxProvider> _providers = providers;
    private readonly ILogger<UnifiedInboxService> _logger = logger;

    public async Task<IReadOnlyList<InboxItem>> GetAllAsync(
        int userId,
        ISet<string> roles,
        IReadOnlyList<int> firmaIds,
        CancellationToken ct)
    {
        // Her provider'ı ayrı sar — biri patlarsa diğerleri gelsin (silent değil, LogWarning).
        var tasks = _providers.Select(p => GetSafelyAsync(p, userId, roles, firmaIds, ct));
        var results = await Task.WhenAll(tasks);

        return results
            .SelectMany(r => r)
            .OrderByDescending(i => i.IsOverdue)
            .ThenByDescending(i => i.CreatedAt)
            .ToList();
    }

    private async Task<IReadOnlyList<InboxItem>> GetSafelyAsync(
        IInboxProvider provider,
        int userId,
        ISet<string> roles,
        IReadOnlyList<int> firmaIds,
        CancellationToken ct)
    {
        try
        {
            return await provider.GetItemsForUserAsync(userId, roles, firmaIds, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Error (Warning değil): kalıcı bozuk provider sessizce inbox kalemi düşürür,
            // kullanıcı eksik onay/vade görür. Alerting eşiğinin altında kalmasın.
            _logger.LogError(ex, "Inbox provider başarısız (kalemler eksik gösterilebilir): {ProviderKey}", provider.ProviderKey);
            return Array.Empty<InboxItem>();
        }
    }
}
