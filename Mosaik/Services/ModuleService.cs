using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mosaik.Models;

namespace Mosaik.Services
{
    // Singleton cache — modül listesi sık değişmez. Invalidate() admin toggle'da çağrılır.
    public class ModuleService : IModuleService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private List<AppModule>? _cache;
        private DateTime _cacheExpiry = DateTime.MinValue;
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

        // Tablo boşsa veya DB erişilemezse bu default'lar gösterilir.
        private static readonly List<AppModule> DefaultModules =
        [
            new() { ModuleKey = "reports",    DisplayName = "Raporlar", IsEnabled = true, SortOrder = 10 },
            new() { ModuleKey = "dashboards", DisplayName = "Panolar",  IsEnabled = true, SortOrder = 20 }
        ];

        public ModuleService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
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
                _cacheExpiry = DateTime.UtcNow.Add(CacheTtl);
                return _cache;
            }
            catch
            {
                return _cache ?? DefaultModules;
            }
        }

        public bool IsEnabled(string moduleKey)
        {
            if (_cache == null) return true; // cache yoksa göster (güvenli taraf)
            return _cache.Any(m => m.ModuleKey == moduleKey && m.IsEnabled);
        }

        public void Invalidate() => _cache = null;
    }
}
