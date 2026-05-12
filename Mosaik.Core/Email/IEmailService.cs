namespace Mosaik.Core.Email
{
    // Plan 31 — Cross-modül email servisi (Notification/Approval/Circular/Contract).
    //
    // Result pattern: caller `IsSuccess` flag'i ile gerçek başarıyı doğrular.
    // `WasSkipped` SMTP devre dışı, `ErrorCode` failure sebebi (smtp_5xx, smtp_auth,
    // smtp_network, unexpected). Silent failure'a yol açmaz.
    public interface IEmailService
    {
        bool IsEnabled { get; }

        Task<EmailSendResult> SendAsync(
            string to,
            string subject,
            string htmlBody,
            CancellationToken ct = default);

        Task<EmailBulkResult> SendBulkAsync(
            IEnumerable<string> recipients,
            string subject,
            string htmlBody,
            CancellationToken ct = default);
    }
}
