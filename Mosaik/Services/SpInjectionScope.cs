using Microsoft.EntityFrameworkCore;
using Mosaik.Core.DataScope;
using Mosaik.Models;

namespace Mosaik.Services
{
    // SP parametre enjeksiyonu için scope (mevcut UserDataFilterInjector pattern'i).
    // Plan 14 Faz C1: interface implementasyonu + DI kayıt. Mevcut Injector
    // refactor edilmedi (Faz C2). vNext modüller bu interface'i kullanır.
    //
    // FilterKey'ler: "sube" (PDKS), "sube" (DER), "urunKategori" (DER) gibi.
    // DataSourceKey zorunlu (BKM şube heterojenliği).
    public class SpInjectionScope : IUserDataScope
    {
        private readonly MosaikContext _context;

        public SpInjectionScope(MosaikContext context)
        {
            _context = context;
        }

        public string Scope => "spInjection";

        // Aktif FilterDefinition varsa kullanıcının en az 1 kaydı zorunlu (Plan 07 Faz 4).
        public bool RequiresExplicitGrant => true;

        public async Task<bool> HasAccessAsync(int userId, string? dataSourceKey, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;

            // FilterValue '*' magic = tümü.
            // Concrete value için CSV içinde olmalı (örn. "1,4477" arasında "4477").
            var filters = await _context.UserDataFilters
                .AsNoTracking()
                .Where(f => f.UserId == userId
                         && (f.DataSourceKey == null || f.DataSourceKey == dataSourceKey))
                .Select(f => new { f.FilterKey, f.FilterValue })
                .ToListAsync();

            foreach (var f in filters)
            {
                if (f.FilterValue == "*") return true;
                var parts = (f.FilterValue ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries
                                                          | StringSplitOptions.TrimEntries);
                if (parts.Contains(value, StringComparer.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        public async Task<List<string>> ListAccessibleValuesAsync(int userId, string? dataSourceKey)
        {
            var filters = await _context.UserDataFilters
                .AsNoTracking()
                .Where(f => f.UserId == userId
                         && (f.DataSourceKey == null || f.DataSourceKey == dataSourceKey))
                .Select(f => f.FilterValue)
                .ToListAsync();

            // '*' yıldız tüm kayıt anlamı — expand etmez.
            if (filters.Any(v => v == "*")) return new List<string> { "*" };

            return filters
                .SelectMany(v => (v ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries
                                                   | StringSplitOptions.TrimEntries))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
