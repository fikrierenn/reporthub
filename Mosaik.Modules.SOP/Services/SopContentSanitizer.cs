using Ganss.Xss;

namespace Mosaik.Modules.SOP.Services
{
    // Plan 34 §10 madde 6 — Quill sanitize Tamim/Circular pattern'ından kopya (modül izolasyonu).
    // Save time sanitize: DB asla zararlı HTML tutmaz.
    public static class SopContentSanitizer
    {
        private static readonly HtmlSanitizer _sanitizer = BuildSanitizer();

        private static HtmlSanitizer BuildSanitizer()
        {
            var s = new HtmlSanitizer();
            s.AllowedTags.Clear();
            s.AllowedAttributes.Clear();
            s.AllowedCssProperties.Clear();
            s.AllowedSchemes.Clear();

            foreach (var t in new[]
            {
                "p", "br", "strong", "em", "u", "s", "ol", "ul", "li",
                "a", "img", "h1", "h2", "h3", "h4", "h5", "h6",
                "blockquote", "code", "pre", "span", "div"
            })
                s.AllowedTags.Add(t);

            foreach (var a in new[] { "href", "src", "alt", "title", "class", "style" })
                s.AllowedAttributes.Add(a);

            foreach (var c in new[]
            {
                "color", "background-color", "text-align",
                "font-weight", "font-style", "text-decoration"
            })
                s.AllowedCssProperties.Add(c);

            s.AllowedSchemes.Add("http");
            s.AllowedSchemes.Add("https");
            s.AllowedSchemes.Add("mailto");

            return s;
        }

        public static string Sanitize(string? html)
        {
            if (string.IsNullOrWhiteSpace(html)) return string.Empty;
            return _sanitizer.Sanitize(html);
        }

        // Plan 34 Faz F için PlainText derive (AI context cache).
        // Basit HTML tag stripping; gerçek metin uzunluğu hesabı için yeterli.
        public static string ExtractPlainText(string? html, int maxChars = 30_000)
        {
            if (string.IsNullOrWhiteSpace(html)) return string.Empty;
            var sanitized = _sanitizer.Sanitize(html);
            var text = System.Text.RegularExpressions.Regex.Replace(sanitized, "<[^>]+>", " ");
            text = System.Net.WebUtility.HtmlDecode(text);
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();
            return text.Length > maxChars ? text[..maxChars] : text;
        }
    }
}
