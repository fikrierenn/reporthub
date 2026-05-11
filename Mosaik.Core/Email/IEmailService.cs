namespace Mosaik.Core.Email
{
    // Plan 31 — Cross-modül email servisi (Notification/Approval/Circular/Contract).
    public interface IEmailService
    {
        bool IsEnabled { get; }
        Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
        Task SendBulkAsync(IEnumerable<string> recipients, string subject, string htmlBody, CancellationToken ct = default);
    }
}
