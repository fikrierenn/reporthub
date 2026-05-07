using Microsoft.EntityFrameworkCore;
using Mosaik.Models;

namespace Mosaik.Core.DataScope
{
    // Rapor erişim filtresi (raporGrubu — Plan 07 Faz 7).
    // ReportCatalog.ReportGroups join üzerinden EF-side filter.
    // ReportsController.Index'in inline mantığını sarmalar.
    //
    // dataSourceKey burada NULL kullanılır — raporGrubu cross-cutting,
    // rapor sisteminden bağımsız.
    public class ReportAccessScope : IUserDataScope
    {
        private readonly MosaikContext _context;

        public ReportAccessScope(MosaikContext context)
        {
            _context = context;
        }

        public string Scope => "reportAccess";

        // raporGrubu aktif olduğunda kullanıcının en az 1 kaydı zorunlu.
        // Aktif değilse: kullanıcı tüm raporları görür.
        public bool RequiresExplicitGrant => true;

        public async Task<bool> HasAccessAsync(int userId, string? dataSourceKey, string groupId)
        {
            if (string.IsNullOrWhiteSpace(groupId)) return false;

            // raporGrubu için: kullanıcının atanmış GroupId listesi.
            var values = await _context.UserDataFilters
                .AsNoTracking()
                .Where(f => f.UserId == userId
                         && f.FilterKey == "raporGrubu"
                         && f.DataSourceKey == null)
                .Select(f => f.FilterValue)
                .ToListAsync();

            if (values.Count == 0) return false;          // deny-by-default
            if (values.Contains("*")) return true;         // tümü
            return values.Contains(groupId, StringComparer.OrdinalIgnoreCase);
        }

        public async Task<List<string>> ListAccessibleValuesAsync(int userId, string? dataSourceKey)
        {
            var values = await _context.UserDataFilters
                .AsNoTracking()
                .Where(f => f.UserId == userId
                         && f.FilterKey == "raporGrubu"
                         && f.DataSourceKey == null)
                .Select(f => f.FilterValue)
                .ToListAsync();

            if (values.Any(v => v == "*")) return new List<string> { "*" };
            return values.Where(v => !string.IsNullOrWhiteSpace(v))
                         .Cast<string>()
                         .Distinct(StringComparer.OrdinalIgnoreCase)
                         .ToList();
        }
    }
}
