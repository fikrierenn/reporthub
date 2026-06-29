using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Mosaik.Core.Domain;
using Mosaik.Core.Lookup;
using Mosaik.Models;

namespace Mosaik.Services
{
    // YonetIQ LookupService port + IMemoryCache 10dk TTL.
    // Cache invalidation: CRUD sonrası otomatik flush.
    public class LookupService : ILookupService
    {
        private readonly MosaikContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<LookupService> _logger;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

        public LookupService(MosaikContext context, IMemoryCache cache, ILogger<LookupService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        private static string CacheKey(string typeCode) => $"lookup_values_{typeCode}";

        public async Task<List<DictionaryValue>> GetValuesAsync(string typeCode)
        {
            var key = CacheKey(typeCode);
            if (_cache.TryGetValue(key, out List<DictionaryValue>? cached) && cached != null)
                return cached;

            var values = await _context.DictionaryValues
                .AsNoTracking()
                .Include(v => v.Type)
                .Where(v => v.IsActive
                         && v.Type != null
                         && v.Type.IsActive
                         && v.Type.Code == typeCode)
                .OrderBy(v => v.DisplayOrder).ThenBy(v => v.Label)
                .ToListAsync();

            _cache.Set(key, values, CacheDuration);
            return values;
        }

        public async Task<DictionaryValue?> GetByCodeAsync(string typeCode, string valueCode)
        {
            var values = await GetValuesAsync(typeCode);
            return values.FirstOrDefault(v => v.Code == valueCode);
        }

        public async Task<List<DictionaryType>> GetTypesAsync()
        {
            return await _context.DictionaryTypes
                .AsNoTracking()
                .Include(t => t.Values)
                .Where(t => t.IsActive)
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<ServiceResult<DictionaryValue>> CreateValueAsync(
            int typeId, string code, string label, int displayOrder, string createdBy)
        {
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(label))
                return ServiceResult<DictionaryValue>.Failure("Kod ve etiket zorunludur.");

            var type = await _context.DictionaryTypes.FindAsync(typeId);
            if (type == null) return ServiceResult<DictionaryValue>.Failure("Tip bulunamadı.");

            if (await _context.DictionaryValues.AnyAsync(v => v.TypeId == typeId && v.Code == code))
                return ServiceResult<DictionaryValue>.Failure("Bu kod zaten mevcut.");

            var value = new DictionaryValue
            {
                TypeId = typeId,
                Code = code.Trim(),
                Label = label.Trim(),
                DisplayOrder = displayOrder,
                IsActive = true,
                CreatedBy = createdBy
            };
            _context.DictionaryValues.Add(value);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LookupService.CreateValueAsync typeId={TypeId} code={Code}", typeId, code);
                return ServiceResult<DictionaryValue>.Failure("Kayıt sırasında hata oluştu.");
            }

            _cache.Remove(CacheKey(type.Code));
            return ServiceResult<DictionaryValue>.Ok(value, "Eklendi.");
        }

        public async Task<ServiceResult> UpdateValueAsync(int valueId, string label, int displayOrder, string updatedBy)
        {
            if (string.IsNullOrWhiteSpace(label))
                return ServiceResult.Failure("Etiket zorunludur.");

            var value = await _context.DictionaryValues.Include(v => v.Type).FirstOrDefaultAsync(v => v.Id == valueId);
            if (value == null) return ServiceResult.Failure("Değer bulunamadı.");

            value.Label = label.Trim();
            value.DisplayOrder = displayOrder;
            value.UpdatedBy = updatedBy;
            value.UpdatedAt = DateTime.UtcNow;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LookupService.UpdateValueAsync valueId={ValueId}", valueId);
                return ServiceResult.Failure("Güncelleme sırasında hata oluştu.");
            }

            if (value.Type != null) _cache.Remove(CacheKey(value.Type.Code));
            return ServiceResult.Ok("Güncellendi.");
        }

        public async Task<ServiceResult> SetActiveAsync(int valueId, bool active, string updatedBy)
        {
            var value = await _context.DictionaryValues.Include(v => v.Type).FirstOrDefaultAsync(v => v.Id == valueId);
            if (value == null) return ServiceResult.Failure("Değer bulunamadı.");

            value.IsActive = active;
            value.UpdatedBy = updatedBy;
            value.UpdatedAt = DateTime.UtcNow;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LookupService.SetActiveAsync valueId={ValueId} active={Active}", valueId, active);
                return ServiceResult.Failure("Güncelleme sırasında hata oluştu.");
            }

            if (value.Type != null) _cache.Remove(CacheKey(value.Type.Code));
            return ServiceResult.Ok(active ? "Aktif edildi." : "Pasif edildi.");
        }

        public void InvalidateCache(string typeCode) => _cache.Remove(CacheKey(typeCode));
    }
}
