namespace Mosaik.Core.Users
{
    // Plan 34 Faz E S-20 — modüller (SOP, Tamim follow-up vb.) aktif kullanıcı
    // ID listesini bu abstraction üzerinden alır. Impl Mosaik tarafında
    // MosaikContext.Users sorgular; modüller User entity'sine erişmez (ADR-002).
    //
    // Plan 18B HR sync sonrası departman bazlı genişletme (User ↔ Department
    // mapping) eklenecek; o zaman GetUserIdsByDepartmentsAsync metodu açılır.
    public interface IActiveUserDirectory
    {
        // IsActive=1 kullanıcıların UserId listesi. firmaId verilirse Users.FirmaIds
        // CSV "1,2,3" formatında o firmayı içerenler (ADR-012 multi-firma).
        Task<List<int>> GetActiveUserIdsAsync(int? firmaId = null);

        // Plan 34 Faz E S-22 — email göndermek için (UserId → Email) lookup.
        // Sadece IsActive=1 + Email NotNullOrEmpty kullanıcılar döner.
        // Verilen userIds boşsa boş dictionary.
        Task<Dictionary<int, string>> GetUserEmailsAsync(IEnumerable<int> userIds);

        // Plan 54 M5 — @mention çözümü + autocomplete. Aktif kullanıcılar (UserId+Username+FullName).
        // firmaId verilirse o firmaya erişenler (ADR-012). Username @mention anahtarıdır.
        Task<List<ActiveUserInfo>> GetActiveUsersAsync(int? firmaId = null);
    }

    // @mention çözümü + autocomplete için hafif kullanıcı kaydı.
    public sealed record ActiveUserInfo(int UserId, string Username, string FullName);
}
