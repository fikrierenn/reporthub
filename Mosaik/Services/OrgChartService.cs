using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Mosaik.Core.Domain;
using Mosaik.Models;

namespace Mosaik.Services
{
    // Plan 20 Faz A — OrgPosition CRUD + cycle-safe parent atama.
    // Code unique + case-insensitive trim match Zirve `Unvan` ile eşleşir.
    public class OrgChartService : IOrgChartService
    {
        private readonly MosaikContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<OrgChartService> _logger;

        private const string CacheKeyIncumbents = "OrgChart:Incumbents";
        private static readonly TimeSpan IncumbentsTtl = TimeSpan.FromMinutes(5);

        public OrgChartService(MosaikContext context, IMemoryCache cache, ILogger<OrgChartService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        private DbSet<OrgPosition> Positions => _context.OrgPositions;

        public Task<List<OrgPosition>> GetAllAsync(bool includeInactive = false)
        {
            var q = Positions.AsNoTracking();
            if (!includeInactive) q = q.Where(p => p.IsActive);
            return q.OrderBy(p => p.DisplayOrder).ThenBy(p => p.Title).ToListAsync();
        }

        public Task<OrgPosition?> GetByIdAsync(int id) =>
            Positions.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);

        public Task<OrgPosition?> GetByCodeAsync(string code)
        {
            var trimmed = (code ?? "").Trim();
            return Positions.AsNoTracking().FirstOrDefaultAsync(p => p.Code == trimmed);
        }

        public async Task<List<OrgPosition>> GetTreeAsync(bool includeInactive = false)
        {
            // Tüm pozisyonları çek, in-memory parent/child grafiği kur.
            // 30-50 düğüm beklendiği için flat fetch + groupby uygundur.
            var all = await GetAllAsync(includeInactive);
            var byParent = all.ToLookup(p => p.ParentPositionId);

            // EF tracking tarafında nav property olmaması için manuel doldurmaya gerek yok;
            // Children koleksiyonu şu an dolu olmayabilir. Caller'a flat liste döner; tree
            // gerekiyorsa byParent[null] kök, byParent[id] çocukları kullanılır.
            // Faz C render katmanı bunu UI tree'ye çevirir.
            return all;
        }

        public async Task<ServiceResult<OrgPosition>> CreateAsync(
            string code, string title, int? parentPositionId, int displayOrder, string? description, string createdBy)
        {
            if (string.IsNullOrWhiteSpace(code))
                return ServiceResult<OrgPosition>.Failure("Kod zorunludur.");
            if (string.IsNullOrWhiteSpace(title))
                return ServiceResult<OrgPosition>.Failure("Başlık zorunludur.");

            var trimmedCode = code.Trim();
            if (await Positions.AnyAsync(p => p.Code == trimmedCode))
                return ServiceResult<OrgPosition>.Failure("Bu kod zaten mevcut.");

            if (parentPositionId.HasValue)
            {
                if (!await Positions.AnyAsync(p => p.Id == parentPositionId.Value))
                    return ServiceResult<OrgPosition>.Failure("Bağlı olduğu görev bulunamadı.");
            }

            var position = new OrgPosition
            {
                Code = trimmedCode,
                Title = title.Trim(),
                ParentPositionId = parentPositionId,
                DisplayOrder = displayOrder,
                IsActive = true,
                Description = description?.Trim(),
                CreatedBy = createdBy
            };

            Positions.Add(position);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OrgChartService.CreateAsync code={Code} parentId={ParentId}", trimmedCode, parentPositionId);
                return ServiceResult<OrgPosition>.Failure("Kayıt sırasında hata oluştu.");
            }

            return ServiceResult<OrgPosition>.Ok(position, "Görev eklendi.");
        }

        public async Task<ServiceResult> UpdateAsync(
            int id, string title, int? parentPositionId, int displayOrder, bool isActive, string? description, string updatedBy)
        {
            if (string.IsNullOrWhiteSpace(title))
                return ServiceResult.Failure("Başlık zorunludur.");

            var position = await Positions.FirstOrDefaultAsync(p => p.Id == id);
            if (position == null) return ServiceResult.Failure("Görev bulunamadı.");

            // Cycle detection: yeni parent kendi alt-ağacında mıyım?
            if (parentPositionId.HasValue)
            {
                if (parentPositionId.Value == id)
                    return ServiceResult.Failure("Görev kendisinin altına alınamaz.");

                var parentExists = await Positions.AnyAsync(p => p.Id == parentPositionId.Value);
                if (!parentExists) return ServiceResult.Failure("Bağlı olduğu görev bulunamadı.");

                if (await CreatesCycleAsync(id, parentPositionId.Value))
                    return ServiceResult.Failure("Bu atama döngü oluşturur (görev kendi alt-ağacına bağlanamaz).");
            }

            position.Title = title.Trim();
            position.ParentPositionId = parentPositionId;
            position.DisplayOrder = displayOrder;
            position.IsActive = isActive;
            position.Description = description?.Trim();
            position.UpdatedBy = updatedBy;
            position.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OrgChartService.UpdateAsync id={Id}", id);
                return ServiceResult.Failure("Güncelleme sırasında hata oluştu.");
            }

            return ServiceResult.Ok("Görev güncellendi.");
        }

        public async Task<ServiceResult> DeleteAsync(int id, string deletedBy)
        {
            var position = await Positions.FirstOrDefaultAsync(p => p.Id == id);
            if (position == null) return ServiceResult.Failure("Görev bulunamadı.");

            if (await Positions.AnyAsync(p => p.ParentPositionId == id))
                return ServiceResult.Failure("Bu görevin altında başka görevler var; önce onları taşıyın.");

            Positions.Remove(position);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OrgChartService.DeleteAsync id={Id}", id);
                return ServiceResult.Failure("Silme sırasında hata oluştu.");
            }

            _logger.LogInformation("OrgPosition deleted id={Id} by={By}", id, deletedBy);
            return ServiceResult.Ok("Görev silindi.");
        }

        public async Task<ServiceResult> ReorderAsync(int positionId, int? newParentId, int[] siblingOrderIds, string updatedBy)
        {
            var position = await Positions.FirstOrDefaultAsync(p => p.Id == positionId);
            if (position == null) return ServiceResult.Failure("Görev bulunamadı.");

            if (newParentId.HasValue)
            {
                if (newParentId.Value == positionId)
                    return ServiceResult.Failure("Görev kendisinin altına alınamaz.");

                if (!await Positions.AnyAsync(p => p.Id == newParentId.Value))
                    return ServiceResult.Failure("Hedef üst görev bulunamadı.");

                if (await CreatesCycleAsync(positionId, newParentId.Value))
                    return ServiceResult.Failure("Bu taşıma döngü oluşturur.");
            }

            siblingOrderIds ??= Array.Empty<int>();
            if (siblingOrderIds.Length > 0)
            {
                if (!siblingOrderIds.Contains(positionId))
                    return ServiceResult.Failure("Kardeş sıralama listesi taşınan görevi içermiyor.");
                if (siblingOrderIds.Distinct().Count() != siblingOrderIds.Length)
                    return ServiceResult.Failure("Kardeş sıralama listesinde tekrar eden ID var.");
            }

            position.ParentPositionId = newParentId;
            position.UpdatedBy = updatedBy;
            position.UpdatedAt = DateTime.UtcNow;

            if (siblingOrderIds.Length > 0)
            {
                var siblings = await Positions.Where(p => siblingOrderIds.Contains(p.Id)).ToListAsync();
                var siblingMap = siblings.ToDictionary(p => p.Id);

                // Tüm kardeşler newParentId altında olmalı (taşınan zaten yeni parent'ı aldı, diğerleri zaten aynı parent altında olmalı).
                foreach (var sibling in siblings)
                {
                    if (sibling.Id == positionId) continue;
                    if (sibling.ParentPositionId != newParentId)
                        return ServiceResult.Failure("Kardeş listesi farklı parent altında ID içeriyor.");
                }

                for (int i = 0; i < siblingOrderIds.Length; i++)
                {
                    if (!siblingMap.TryGetValue(siblingOrderIds[i], out var sibling)) continue;
                    sibling.DisplayOrder = i + 1;
                    if (sibling.Id != positionId)
                    {
                        sibling.UpdatedBy = updatedBy;
                        sibling.UpdatedAt = DateTime.UtcNow;
                    }
                }
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OrgChartService.ReorderAsync id={Id} newParent={Parent}", positionId, newParentId);
                return ServiceResult.Failure("Sıralama kaydedilemedi.");
            }

            return ServiceResult.Ok("Sıralama güncellendi.");
        }

        public async Task<ServiceResult<OrgPosition>> ImportFromZirveCodeAsync(string code, string createdBy)
        {
            if (string.IsNullOrWhiteSpace(code))
                return ServiceResult<OrgPosition>.Failure("Unvan kodu zorunludur.");

            var trimmed = code.Trim();
            if (await Positions.AnyAsync(p => p.Code == trimmed))
                return ServiceResult<OrgPosition>.Failure("Bu unvan zaten Mosaik'te tanımlı.");

            // Title Case: tüm kelimeleri büyük başlat, geri kalanı küçük (Türkçe destekli)
            var ti = System.Globalization.CultureInfo.GetCultureInfo("tr-TR").TextInfo;
            var title = ti.ToTitleCase(trimmed.ToLower(System.Globalization.CultureInfo.GetCultureInfo("tr-TR")));

            var position = new OrgPosition
            {
                Code = trimmed,
                Title = title,
                ParentPositionId = null,
                DisplayOrder = 9999,
                IsActive = true,
                CreatedBy = createdBy
            };

            Positions.Add(position);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OrgChartService.ImportFromZirveCodeAsync code={Code}", trimmed);
                return ServiceResult<OrgPosition>.Failure("İçe aktarma sırasında hata oluştu.");
            }

            return ServiceResult<OrgPosition>.Ok(position, "Unvan içe aktarıldı.");
        }

        public async Task<(List<string> unknownCodes, string? error)> GetUnknownZirveCodesAsync()
        {
            var ds = await _context.DataSources.AsNoTracking()
                .FirstOrDefaultAsync(d => d.DataSourceKey == "IK" && d.IsActive);
            if (ds == null)
                return (new List<string>(), "IK DataSource tanımlı değil veya pasif.");
            if (string.IsNullOrWhiteSpace(ds.ConnString))
                return (new List<string>(), "IK DataSource bağlantı bilgisi boş.");

            var existing = await Positions.AsNoTracking()
                .Select(p => p.Code).ToListAsync();
            var existingSet = existing.Select(c => c.Trim().ToUpperInvariant()).ToHashSet();

            var zirve = new List<string>();
            try
            {
                await using var conn = new SqlConnection(ds.ConnString);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(
                    @"SELECT DISTINCT LTRIM(RTRIM(Unvan)) AS Unvan
                      FROM dbo.vw_PersonelDepartman
                      WHERE Ict IS NULL AND Unvan IS NOT NULL AND LTRIM(RTRIM(Unvan)) <> ''",
                    conn) { CommandType = CommandType.Text, CommandTimeout = 30 };
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var u = reader.GetString(0);
                    if (!string.IsNullOrWhiteSpace(u)) zirve.Add(u);
                }
            }
            catch (OperationCanceledException)
            {
                throw; // Request iptal — error olarak loglama
            }
            catch (DbException ex)
            {
                _logger.LogError(ex, "OrgChartService.GetUnknownZirveCodesAsync — DB hatası ds={Ds}", ds.DataSourceKey);
                return (new List<string>(), "Zirve'ye erişilemedi. Lütfen sistem yöneticisine bildirin.");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "OrgChartService.GetUnknownZirveCodesAsync — bağlantı yapılandırma hatası");
                return (new List<string>(), "Zirve bağlantı yapılandırması hatalı. Lütfen sistem yöneticisine bildirin.");
            }

            var unknown = zirve
                .Where(u => !existingSet.Contains(u.Trim().ToUpperInvariant()))
                .OrderBy(u => u)
                .ToList();
            return (unknown, null);
        }

        public async Task<OrgChartWithIncumbents> GetChartWithIncumbentsAsync()
        {
            // Cache hit
            if (_cache.TryGetValue<OrgChartWithIncumbents>(CacheKeyIncumbents, out var cached) && cached != null)
                return cached;

            var positions = await GetAllAsync(includeInactive: false);
            var result = new OrgChartWithIncumbents
            {
                Positions = positions,
                FetchedAtUtc = DateTime.UtcNow
            };

            var ds = await _context.DataSources.AsNoTracking()
                .FirstOrDefaultAsync(d => d.DataSourceKey == "IK" && d.IsActive);
            if (ds == null || string.IsNullOrWhiteSpace(ds.ConnString))
            {
                result.Error = "IK DataSource tanımlı değil veya bağlantı bilgisi boş.";
                return result;
            }

            var positionCodeUpper = positions
                .Select(p => p.Code.Trim().ToUpperInvariant())
                .ToHashSet();

            try
            {
                await using var conn = new SqlConnection(ds.ConnString);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(
                    @"SELECT Personelno, AdSoyad, Unvan, Lokasyon, AltLokasyon, Departman, Firma
                      FROM dbo.vw_PersonelDepartman
                      WHERE Ict IS NULL",
                    conn) { CommandType = CommandType.Text, CommandTimeout = 30 };
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var unvan = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
                    var unvanKey = unvan.Trim().ToUpperInvariant();
                    var inc = new OrgIncumbent
                    {
                        PersonelNo = reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                        AdSoyad = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                        Unvan = unvan,
                        Lokasyon = reader.IsDBNull(3) ? null : reader.GetString(3),
                        AltLokasyon = reader.IsDBNull(4) ? null : reader.GetString(4),
                        Departman = reader.IsDBNull(5) ? null : reader.GetString(5),
                        Firma = reader.IsDBNull(6) ? null : reader.GetString(6)
                    };

                    if (string.IsNullOrEmpty(unvanKey))
                    {
                        result.UnmatchedIncumbents.Add(inc);
                        continue;
                    }

                    if (positionCodeUpper.Contains(unvanKey))
                    {
                        if (!result.IncumbentsByCode.TryGetValue(unvanKey, out var list))
                        {
                            list = new List<OrgIncumbent>();
                            result.IncumbentsByCode[unvanKey] = list;
                        }
                        list.Add(inc);
                    }
                    else
                    {
                        result.UnmatchedIncumbents.Add(inc);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw; // Request iptal — error olarak loglama
            }
            catch (DbException ex)
            {
                _logger.LogError(ex, "OrgChartService.GetChartWithIncumbentsAsync — DB hatası ds={Ds}", ds.DataSourceKey);
                result.Error = "Zirve'ye erişilemedi. Lütfen sistem yöneticisine bildirin.";
                return result;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "OrgChartService.GetChartWithIncumbentsAsync — bağlantı yapılandırma hatası");
                result.Error = "Zirve bağlantı yapılandırması hatalı. Lütfen sistem yöneticisine bildirin.";
                return result;
            }

            _cache.Set(CacheKeyIncumbents, result, IncumbentsTtl);
            return result;
        }

        // True dönerse: candidateParent positionId'nin alt-ağacında — atama cycle yaratır.
        private async Task<bool> CreatesCycleAsync(int positionId, int candidateParentId)
        {
            // candidateParent'tan köke kadar çık; positionId'ye rastlarsak cycle.
            var visited = new HashSet<int> { positionId };
            int? cursor = candidateParentId;
            int hops = 0;
            while (cursor.HasValue)
            {
                if (!visited.Add(cursor.Value)) return true; // pre-existing cycle (data anomalisi) — yine de güvenli reject
                if (cursor.Value == positionId) return true;

                var parent = await Positions.AsNoTracking()
                    .Where(p => p.Id == cursor.Value)
                    .Select(p => new { p.ParentPositionId })
                    .FirstOrDefaultAsync();

                if (parent == null) break;
                cursor = parent.ParentPositionId;

                if (++hops > 1000)
                {
                    _logger.LogWarning("OrgChart cycle check exceeded 1000 hops; aborting from positionId={PosId}", positionId);
                    return true;
                }
            }
            return false;
        }
    }
}
