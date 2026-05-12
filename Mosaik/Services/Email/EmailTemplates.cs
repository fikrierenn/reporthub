using System.Net;

namespace Mosaik.Services.Email
{
    // Plan 31 — HTML email şablonları. Mosaik brand (kırmızı + koyu gri).
    // $$ prefix: CSS içindeki tek { } literal, çift {{ }} interpolation.
    //
    // GÜVENLİK (security-principles.md §2):
    // Kullanıcı kontrollü her metin parametresi E() ile HtmlEncode edilir.
    // C# raw string ($$"""...""") Razor'ın @ auto-encode'unu yapmaz — elle encode zorunlu.
    // appUrl Configuration'dan gelir ama yine de Uri whitelist + encode (defansif).
    internal static class EmailTemplates
    {
        private static string E(string? s) => WebUtility.HtmlEncode(s ?? string.Empty);

        private static string SafeUrl(string? url) =>
            !string.IsNullOrEmpty(url)
            && Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                ? E(url)
                : "#";

        private static string Wrap(string title, string bodyContent) => $$"""
            <!DOCTYPE html>
            <html lang="tr">
            <head>
              <meta charset="UTF-8" />
              <meta name="viewport" content="width=device-width, initial-scale=1.0" />
              <title>{{E(title)}}</title>
              <style>
                body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Arial, sans-serif; background: #f3f4f6; margin: 0; padding: 0; }
                .wrapper { max-width: 600px; margin: 32px auto; background: #fff; border-radius: 10px; overflow: hidden; box-shadow: 0 2px 12px rgba(0,0,0,.08); }
                .header { background: #111827; padding: 24px 32px; }
                .header h1 { color: #dc2626; margin: 0; font-size: 22px; letter-spacing: .5px; }
                .header p { color: #9ca3af; margin: 4px 0 0; font-size: 13px; }
                .body { padding: 28px 32px; color: #1f2937; font-size: 15px; line-height: 1.6; }
                .footer { background: #f9fafb; padding: 16px 32px; text-align: center; color: #9ca3af; font-size: 12px; border-top: 1px solid #e5e7eb; }
                .btn { display: inline-block; margin-top: 20px; padding: 11px 24px; background: #dc2626; color: #fff; font-weight: 600; text-decoration: none; border-radius: 6px; font-size: 14px; }
                table { width: 100%; border-collapse: collapse; margin-top: 16px; }
                th { background: #f3f4f6; text-align: left; padding: 8px 12px; font-size: 12px; color: #6b7280; text-transform: uppercase; }
                td { padding: 9px 12px; border-bottom: 1px solid #f3f4f6; font-size: 14px; }
                .badge-red { background: #fee2e2; color: #b91c1c; padding: 2px 8px; border-radius: 4px; font-size: 12px; font-weight: 600; }
                .badge-yellow { background: #fef3c7; color: #92400e; padding: 2px 8px; border-radius: 4px; font-size: 12px; font-weight: 600; }
                .badge-green { background: #d1fae5; color: #047857; padding: 2px 8px; border-radius: 4px; font-size: 12px; font-weight: 600; }
              </style>
            </head>
            <body>
              <div class="wrapper">
                <div class="header">
                  <h1>Mosaik</h1>
                  <p>Şirket içi portal</p>
                </div>
                <div class="body">{{bodyContent}}</div>
                <div class="footer">Bu e-posta Mosaik tarafından otomatik gönderilmiştir. Lütfen yanıtlamayın.</div>
              </div>
            </body>
            </html>
            """;

        // Sözleşme yükümlülüğü gecikmiş.
        public static string OverdueObligation(string obligationTitle, string dueDate, string firmaName, string appUrl) =>
            Wrap("Geciken Yükümlülük", $$"""
                <p>Merhaba,</p>
                <p><strong>{{E(firmaName)}}</strong> hesabınızda bir yükümlülük son ödeme tarihini geçti:</p>
                <table>
                  <tr><th>Yükümlülük</th><th>Son Tarih</th><th>Durum</th></tr>
                  <tr><td>{{E(obligationTitle)}}</td><td>{{E(dueDate)}}</td><td><span class="badge-red">Gecikmiş</span></td></tr>
                </table>
                <a class="btn" href="{{SafeUrl(appUrl)}}/Contracts">Sözleşmeleri Görüntüle</a>
                """);

        // Yaklaşan yükümlülük hatırlatması.
        public static string ObligationReminder(string obligationTitle, string dueDate, int daysLeft, string firmaName, string appUrl) =>
            Wrap("Yükümlülük Hatırlatması", $$"""
                <p>Merhaba,</p>
                <p><strong>{{E(firmaName)}}</strong> hesabınızda bir yükümlülüğün son tarihi yaklaşıyor:</p>
                <table>
                  <tr><th>Yükümlülük</th><th>Son Tarih</th><th>Kalan</th></tr>
                  <tr><td>{{E(obligationTitle)}}</td><td>{{E(dueDate)}}</td><td><span class="badge-yellow">{{daysLeft}} gün</span></td></tr>
                </table>
                <a class="btn" href="{{SafeUrl(appUrl)}}/Contracts">Sözleşmeleri Görüntüle</a>
                """);

        // AI extraction tamamlandı.
        public static string AiExtractionComplete(string documentName, int suggestionCount, int extractionId, string appUrl) =>
            Wrap("AI Analizi Tamamlandı", $$"""
                <p>Merhaba,</p>
                <p><strong>{{E(documentName)}}</strong> dokümanının AI analizi tamamlandı.</p>
                <table>
                  <tr><th>Doküman</th><th>Öneriler</th><th>Durum</th></tr>
                  <tr><td>{{E(documentName)}}</td><td>{{suggestionCount}} öneri</td><td><span class="badge-yellow">İnceleme Bekliyor</span></td></tr>
                </table>
                <p>Yükümlülük ve risk uyarısı önerilerini incelemek için aşağıdaki bağlantıya tıklayın.</p>
                <a class="btn" href="{{SafeUrl(appUrl)}}/Contracts/AiReview/{{extractionId}}">İncelemeye Git</a>
                """);

        // Tamim/duyuru yayınlandı.
        public static string CircularPublished(string subject, string firmaName, int circularId, string appUrl) =>
            Wrap("Yeni Tamim", $$"""
                <p>Merhaba,</p>
                <p><strong>{{E(firmaName)}}</strong> için yeni bir tamim yayınlandı:</p>
                <table>
                  <tr><th>Konu</th><th>Durum</th></tr>
                  <tr><td>{{E(subject)}}</td><td><span class="badge-green">Yayında</span></td></tr>
                </table>
                <a class="btn" href="{{SafeUrl(appUrl)}}/Tamim/Circulars/Details/{{circularId}}">Tamimi Aç</a>
                """);

        // Onay bekleyen talep.
        public static string ApprovalPending(string requestTitle, string firmaName, int approvalId, string appUrl) =>
            Wrap("Onayınız Bekleniyor", $$"""
                <p>Merhaba,</p>
                <p><strong>{{E(firmaName)}}</strong> hesabında onayınızı bekleyen bir talep var:</p>
                <table>
                  <tr><th>Talep</th><th>Durum</th></tr>
                  <tr><td>{{E(requestTitle)}}</td><td><span class="badge-yellow">Onay Bekliyor</span></td></tr>
                </table>
                <a class="btn" href="{{SafeUrl(appUrl)}}/Approvals/Details/{{approvalId}}">Talebi İncele</a>
                """);
    }
}
