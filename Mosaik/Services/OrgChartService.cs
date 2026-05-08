using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Domain;
using Mosaik.Models;

namespace Mosaik.Services
{
    // Plan 20 Faz A — OrgPosition CRUD + cycle-safe parent atama.
    // Code unique + case-insensitive trim match Zirve `Unvan` ile eşleşir.
    public class OrgChartService : IOrgChartService
    {
        private readonly MosaikContext _context;
        private readonly ILogger<OrgChartService> _logger;

        public OrgChartService(MosaikContext context, ILogger<OrgChartService> logger)
        {
            _context = context;
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
