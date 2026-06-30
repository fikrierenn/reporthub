using Mosaik.Core.Domain;

namespace Mosaik.Core.Comments
{
    // Plan 54 M5 — cross-modül yorum servisi (ADR-002). Modüller (Circular/SOP) bu
    // abstraction üzerinden yorum ekler/listeler; MosaikContext'e doğrudan erişmez.
    public interface ICommentService
    {
        // Yorum ekle: entityType lookup doğrula → hedef varlığın firmasını SUNUCU-TARAFI çöz
        // → kullanıcının erişebildiği firmalar (allowedFirmaIds) içinde mi kontrol et (multi-tenant
        // write-path güvenliği, client'tan gelen firmaId'ye GÜVENME) → Body sanitize → kaydet →
        // @mention parse → fan-out. targetUrl bildirimde "yoruma git" linki.
        Task<ServiceResult<Comment>> AddAsync(
            string entityType, int entityId, IReadOnlyCollection<int> allowedFirmaIds,
            int authorId, string authorName, string rawBody, string targetUrl,
            CancellationToken ct = default);

        // Bir varlığın yorumları (eskiden yeniye). FirmaId güvenlik filtresi.
        Task<List<Comment>> GetForEntityAsync(
            string entityType, int entityId, int firmaId, CancellationToken ct = default);
    }
}
