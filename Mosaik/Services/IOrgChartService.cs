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
    }
}
