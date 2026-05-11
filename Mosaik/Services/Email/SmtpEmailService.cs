using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Mosaik.Core.Email;

namespace Mosaik.Services.Email
{
    // Plan 31 — SMTP üzerinden HTML email gönderici.
    // Enabled=false ise sessizce skip eder (dev/test sırasında SMTP yokken patlamasın).
    public class SmtpEmailService : IEmailService
    {
        private readonly SmtpSettings _settings;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(IOptions<SmtpSettings> options, ILogger<SmtpEmailService> logger)
        {
            _settings = options.Value;
            _logger = logger;
        }

        public bool IsEnabled => _settings.Enabled
            && !string.IsNullOrWhiteSpace(_settings.Host)
            && !string.IsNullOrWhiteSpace(_settings.FromAddress);

        public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
        {
            if (!IsEnabled)
            {
                _logger.LogDebug("Email devre dışı, atlandı. Alıcı: {To}, Konu: {Subject}", to, subject);
                return;
            }

            try
            {
                using var client = BuildClient();
                using var message = BuildMessage(to, subject, htmlBody);
                await client.SendMailAsync(message, ct);
                _logger.LogInformation("Email gönderildi. Alıcı: {To}, Konu: {Subject}", to, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email gönderilemedi. Alıcı: {To}, Konu: {Subject}", to, subject);
            }
        }

        public async Task SendBulkAsync(IEnumerable<string> recipients, string subject, string htmlBody, CancellationToken ct = default)
        {
            if (!IsEnabled) return;

            var tasks = recipients.Select(r => SendAsync(r, subject, htmlBody, ct));
            await Task.WhenAll(tasks);
        }

        private SmtpClient BuildClient() => new(_settings.Host, _settings.Port)
        {
            EnableSsl = _settings.UseSsl,
            Credentials = new NetworkCredential(_settings.Username, _settings.Password)
        };

        private MailMessage BuildMessage(string to, string subject, string htmlBody)
        {
            var msg = new MailMessage
            {
                From = new MailAddress(_settings.FromAddress, _settings.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            msg.To.Add(to);
            return msg;
        }
    }
}
