using Mosaik.Core.Domain;

namespace Mosaik.Services
{
    // Plan 20 Faz A — Organizasyon şeması servis arayüzü.
    // Faz A: pozisyon CRUD + tree + cycle detect.
    // Faz C: Zirve canlı join (GetChartWithIncumbentsAsync) — şu an placeholder, allowlist sonrası implement.
    public interface IOrgChartService
    {
        Task<List<OrgPosition>> GetAllAsync(bool includeInactive = false);
        Task<OrgPosition?> GetByIdAsync(int id);
        Task<OrgPosition?> GetByCodeAsync(string code);
        Task<List<OrgPosition>> GetTreeAsync(bool includeInactive = false);

        Task<ServiceResult<OrgPosition>> CreateAsync(string code, string title, int? parentPositionId, int displayOrder, string? description, string createdBy);
        Task<ServiceResult> UpdateAsync(int id, string title, int? parentPositionId, int displayOrder, bool isActive, string? description, string updatedBy);
        Task<ServiceResult> DeleteAsync(int id, string deletedBy);

        // Faz B — Drag-drop reorder. positionId yeni parent altına taşınır + aynı parent
        // altındaki tüm kardeşlerin DisplayOrder'ı siblingOrderIds dizisine göre yeniden
        // atanır (1..N). Cycle detect uygulanır.
        Task<ServiceResult> ReorderAsync(int positionId, int? newParentId, int[] siblingOrderIds, string updatedBy);

        // Faz B — Tanımsız Zirve unvanını tek-tık import. Code = unvan adı, Title = unvan adı
        // (Title Case'e çevrilir). ParentPositionId NULL (root) — admin sonra drag-drop ile yerleştirir.
        Task<ServiceResult<OrgPosition>> ImportFromZirveCodeAsync(string code, string createdBy);

        // Faz B — Zirve'de var, Mosaik'te yok unvanları tespit eder. IK DataSourceKey'in
        // ConnString'i ile dbo.vw_PersonelDepartman sorgulanır. Hata olursa (error, []) döner.
        Task<(List<string> unknownCodes, string? error)> GetUnknownZirveCodesAsync();
    }
}
