using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mosaik.Models;

namespace Mosaik.Services
{
    // Singleton — DB'ye her request'te gitmesin. Invalidate() admin kaydettiğinde çağrılır.
    public class BrandSettingsService : IBrandService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<BrandSettingsService> _logger;
        private BrandSettings? _cache;
        private DateTime _cacheExpiry = DateTime.MinValue;
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

        private static readonly BrandSettings DefaultBrand = new()
        {
            Id = 1,
            SiteTitle = "Mosaik",
            PrimaryColor = "#6366f1"
        };

        public BrandSettingsService(IServiceScopeFactory scopeFactory, ILogger<BrandSettingsService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task<BrandSettings> GetAsync()
        {
            if (_cache != null && DateTime.UtcNow < _cacheExpiry)
                return _cache;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<MosaikContext>();
                _cache = await db.BrandSettings.AsNoTracking().FirstOrDefaultAsync() ?? DefaultBrand;
                _cacheExpiry = DateTime.UtcNow.Add(CacheTtl);
                return _cache;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "BrandSettings read failed, falling back to default/cache");
                return _cache ?? DefaultBrand;
            }
        }

        public void Invalidate() => _cache = null;
    }
}
