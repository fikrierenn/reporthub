using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mosaik.Models;

namespace Mosaik.Services
{
    // Singleton cache — modül listesi sık değişmez. Invalidate() admin toggle'da çağrılır.
    public class ModuleService : IModuleService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ModuleService> _logger;
        private List<AppModule>? _cache;
        // N-2: moduleKey → izinli roller. Boş set yoksa (key yok) = herkese açık.
        private Dictionary<string, HashSet<string>>? _roleCache;
        private DateTime _cacheExpiry = DateTime.MinValue;
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

        // Tablo boşsa veya DB erişilemezse bu default'lar gösterilir.
        private static readonly List<AppModule> DefaultModules =
        [
            new() { ModuleKey = "reports",    DisplayName = "Raporlar", IsEnabled = true, SortOrder = 10 },
            new() { ModuleKey = "dashboards", DisplayName = "Panolar",  IsEnabled = true, SortOrder = 20 }
        ];

        public ModuleService(IServiceScopeFactory scopeFactory, ILogger<ModuleService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task<IReadOnlyList<AppModule>> GetEnabledAsync()
        {
            var all = await GetAllAsync();
            return all.Where(m => m.IsEnabled).ToList();
        }

        public async Task<IReadOnlyList<AppModule>> GetAllAsync()
        {
            if (_cache != null && DateTime.UtcNow < _cacheExpiry)
                return _cache;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<MosaikContext>();
                var modules = await db.AppModules.AsNoTracking().OrderBy(m => m.SortOrder).ToListAsync();
                _cache = modules.Count > 0 ? modules : DefaultModules;

                // N-2: ModuleRoleAccess cache — moduleKey → izinli roller kümesi
                var roleRows = await db.ModuleRoleAccess.AsNoTracking()
                    .Select(r => new { r.ModuleId, r.RoleName })
                    .ToListAsync();
                var keyById = _cache.ToDictionary(m => m.ModuleId, m => m.ModuleKey);
                _roleCache = roleRows
                    .Where(r => keyById.ContainsKey(r.ModuleId))
                    .GroupBy(r => keyById[r.ModuleId])
                    .ToDictionary(g => g.Key, g => g.Select(r => r.RoleName).ToHashSet(StringComparer.OrdinalIgnoreCase));

                _cacheExpiry = DateTime.UtcNow.Add(CacheTtl);
                return _cache;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AppModules read failed, falling back to default/cache");
                return _cache ?? DefaultModules;
            }
        }

        public bool IsEnabled(string moduleKey)
        {
            if (_cache == null) return true; // cache yoksa göster (güvenli taraf)
            return _cache.Any(m => m.ModuleKey == moduleKey && m.IsEnabled);
        }

        // N-2: Kayıt yoksa (herkese açık modül) → true. Kayıt varsa → role listede mi?
        public bool IsAccessibleForRole(string moduleKey, string roleName)
        {
            if (_roleCache == null) return true; // cache yüklenmemiş → deny-safe olmaması için açık bırak
            if (!_roleCache.TryGetValue(moduleKey, out var roles)) return true; // kısıtlama yok
            return roles.Contains(roleName);
        }

        public void Invalidate()
        {
            _cache = null;
            _roleCache = null;
        }
    }
}
