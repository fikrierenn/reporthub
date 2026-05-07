using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Ai;
using Mosaik.Models;

namespace Mosaik.Services
{
    // Plan 17 Faz F — AiSettings multi-row provider.
    // Aktif config'leri sıralayıp döndürür: IsPrimary önce, sonra Priority asc.
    // 1 dakika in-memory cache (admin Save sonrası InvalidateCache).
    public class AiSettingsProvider : IAiSettingsProvider
    {
        private readonly MosaikContext _db;
        private static IReadOnlyList<AiConfig>? _cached;
        private static DateTime _cachedAt;
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(1);
        private static readonly SemaphoreSlim _lock = new(1, 1);

        public AiSettingsProvider(MosaikContext db) { _db = db; }

        public async Task<IReadOnlyList<AiConfig>> GetActiveOrderedAsync(CancellationToken ct = default)
        {
            if (_cached != null && DateTime.UtcNow - _cachedAt < CacheTtl) return _cached;

            await _lock.WaitAsync(ct);
            try
            {
                if (_cached != null && DateTime.UtcNow - _cachedAt < CacheTtl) return _cached;

                var rows = await _db.AiSettings.AsNoTracking()
                    .Where(x => x.IsEnabled && !string.IsNullOrEmpty(x.ApiKey))
                    .OrderByDescending(x => x.IsPrimary)
                    .ThenBy(x => x.Priority)
                    .ThenBy(x => x.Id)
                    .ToListAsync(ct);

                _cached = rows.Select(s => new AiConfig(
                    Provider: s.Provider,
                    ApiKey: s.ApiKey ?? "",
                    Model: s.Model,
                    MaxTokens: s.MaxTokens,
                    Temperature: s.Temperature,
                    BaseUrl: s.BaseUrl)).ToList();
                _cachedAt = DateTime.UtcNow;
                return _cached;
            }
            finally { _lock.Release(); }
        }

        public async Task<AiConfig?> GetActiveAsync(CancellationToken ct = default)
        {
            var list = await GetActiveOrderedAsync(ct);
            return list.Count > 0 ? list[0] : null;
        }

        public static void InvalidateCache()
        {
            _cached = null;
            _cachedAt = DateTime.MinValue;
        }
    }
}
