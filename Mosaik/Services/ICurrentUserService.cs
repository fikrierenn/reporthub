namespace Mosaik.Services
{
    // ADR-012 — Firma erişimi session claim'lerinden okunur.
    // Login sırasında AuthController FirmaIds CSV'sini parse edip her firma için ayrı "firmaId" claim'i ekler.
    // Her request'te HttpContext.User'dan resolve edilir (Scoped).
    public interface ICurrentUserService
    {
        int? UserId { get; }
        string? Username { get; }
        IReadOnlyList<int> FirmaIds { get; }  // 0 öğe = modül kapalı, N öğe = N firmaya erişim
        bool IsAuthenticated { get; }
    }
}
