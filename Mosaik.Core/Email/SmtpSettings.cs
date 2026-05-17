namespace Mosaik.Core.Email
{
    // Plan 31 — SMTP email konfigürasyonu (appsettings.json → "SmtpSettings").
    public class SmtpSettings
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public bool UseSsl { get; set; } = true;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromAddress { get; set; } = string.Empty;
        public string FromName { get; set; } = "Mosaik";
        public bool Enabled { get; set; } = false;
        public string AppUrl { get; set; } = string.Empty;  // email link'leri için (ör. https://mosaik.bkm.com.tr)
    }
}
