using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using Microsoft.Extensions.Options;
using Mosaik.Core.Email;

namespace Mosaik.Services.Email
{
    // Plan 31 — SMTP üzerinden HTML email gönderici.
    // Enabled=false ise Skipped döner (dev/test sırasında SMTP yokken patlamasın).
    // Result pattern (silent-failure-hunter / mosaik-security skill): caller başarısızlığı
    // görmek zorunda, exception swallow yasak. Failure → `email_send_failed` audit
    // (audit log caller tarafında, çünkü AuditLog dependency cross-modül yayılmasın).
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

        public async Task<EmailSendResult> SendAsync(
            string to,
            string subject,
            string htmlBody,
            CancellationToken ct = default)
        {
            if (!IsEnabled)
            {
                _logger.LogDebug("Email devre dışı, atlandı. Alıcı: {To}, Konu: {Subject}", to, subject);
                return EmailSendResult.Skipped("smtp_disabled");
            }

            try
            {
                using var client = BuildClient();
                using var message = BuildMessage(to, subject, htmlBody);
                await client.SendMailAsync(message, ct);
                _logger.LogInformation("Email gönderildi. Alıcı: {To}, Konu: {Subject}", to, subject);
                return EmailSendResult.Ok();
            }
            catch (SmtpException sex)
            {
                var code = sex.StatusCode == SmtpStatusCode.GeneralFailure
                    ? "smtp_unknown"
                    : $"smtp_{(int)sex.StatusCode}";
                _logger.LogError(sex, "SMTP hatası. Alıcı: {To}, StatusCode: {Code}", to, sex.StatusCode);
                return EmailSendResult.Failed(code, sex.Message);
            }
            catch (SocketException nex)
            {
                _logger.LogError(nex, "SMTP ağ hatası. Alıcı: {To}", to);
                return EmailSendResult.Failed("smtp_network", nex.Message);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Email gönderimi iptal edildi. Alıcı: {To}", to);
                return EmailSendResult.Failed("cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email gönderilemedi (beklenmedik). Alıcı: {To}", to);
                return EmailSendResult.Failed("unexpected", ex.Message);
            }
        }

        public async Task<EmailBulkResult> SendBulkAsync(
            IEnumerable<string> recipients,
            string subject,
            string htmlBody,
            CancellationToken ct = default)
        {
            var list = recipients.ToList();

            if (!IsEnabled)
            {
                _logger.LogWarning(
                    "SMTP devre dışı — bulk gönderim atlandı. Skipped={Count}, Konu: {Subject}",
                    list.Count, subject);
                return new EmailBulkResult(Sent: 0, Skipped: list.Count, Failures: Array.Empty<EmailBulkFailure>());
            }

            // Sequential — paralel SMTP bağlantısı çoğu SMTP gateway'inde rate-limit veya
            // connection-cap'e takılır. Caller ölçek istiyorsa batch+throttle ekler.
            var failures = new List<EmailBulkFailure>();
            var sent = 0;

            foreach (var recipient in list)
            {
                var result = await SendAsync(recipient, subject, htmlBody, ct);
                if (result.IsSuccess) sent++;
                else if (!result.WasSkipped)
                    failures.Add(new EmailBulkFailure(recipient, result.ErrorCode ?? "unknown"));
            }

            return new EmailBulkResult(sent, Skipped: 0, failures);
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
