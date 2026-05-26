namespace Mosaik.Core.Messaging
{
    // Merkezi bildirim + email katmanı.
    // In-app notification (her zaman) + HTML email (HtmlEmail != null ve SMTP aktifse) birlikte gönderir.
    // Caller şablon seçimini yapar (EmailTemplates.XXX), IMessenger kanalları birleştirir.
    public interface IMessenger
    {
        Task SendAsync(int userId, MessengerPayload payload, CancellationToken ct = default);

        Task SendBulkAsync(IReadOnlyList<int> userIds, MessengerPayload payload, CancellationToken ct = default);
    }

    public record MessengerPayload(
        string Title,
        string Body,
        string? HtmlEmail = null,       // null → sadece in-app; dolu → email de gönderilir
        string? EntityType = null,
        int? EntityId = null,
        string? TargetUrl = null,
        string? NotificationType = null,
        string? ExternalKey = null      // dedup — null → CreateBulkAsync, dolu → CreateBulkIfNotExistsAsync
    );
}
