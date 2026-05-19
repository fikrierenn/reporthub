namespace Mosaik.Core.Notification
{
    // Plan 17 Faz H — cross-modül bildirim servisi.
    // Modüller bu interface üzerinden notification yaratır (Circular, Approval, HR, Task).
    public interface INotificationService
    {
        // Tek bir kullanıcıya bildirim oluştur.
        Task<Notification> CreateAsync(int userId, string entityType, int? entityId,
            string title, string? message, string? targetUrl, string? notificationType, string? createdBy);

        // Toplu — örn. bir tamim yayınlandığında tüm aktif kullanıcılara.
        Task<int> CreateBulkAsync(IEnumerable<int> userIds, string entityType, int? entityId,
            string title, string? message, string? targetUrl, string? notificationType, string? createdBy);

        // Tüm aktif kullanıcılara bildirim gönder (Mosaik.Users.IsActive=1).
        Task<int> NotifyAllActiveUsersAsync(string entityType, int? entityId,
            string title, string? message, string? targetUrl, string? notificationType, string? createdBy);

        // Kullanıcının okunmamış bildirim sayısı (sidebar badge).
        Task<int> GetUnreadCountAsync(int userId);

        // Kullanıcının son N bildirimi (varsayılan 30).
        Task<List<Notification>> GetRecentAsync(int userId, int take = 30, bool unreadOnly = false);

        // Tek bildirimi okundu işaretle (sahibi olmalı).
        Task<bool> MarkAsReadAsync(int notificationId, int userId);

        // Kullanıcının tüm bildirimlerini okundu işaretle.
        Task<int> MarkAllAsReadAsync(int userId);

        // N-1 Hibrit: ExternalKey bazlı dedup ile toplu bildirim. Var olanları atlar.
        Task<int> CreateBulkIfNotExistsAsync(string externalKeyPrefix, IEnumerable<int> userIds,
            string entityType, int? entityId, string title, string? message,
            string? targetUrl, string? notificationType, string? createdBy);
    }
}
