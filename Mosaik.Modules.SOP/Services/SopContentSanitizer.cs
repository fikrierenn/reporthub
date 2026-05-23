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

            // TinyMCE Word-class output: tablo, image, font color/size, alignment, hr, page break.
            foreach (var t in new[]
            {
                "p", "br", "hr", "strong", "em", "u", "s", "sub", "sup",
                "ol", "ul", "li", "a", "img",
                "h1", "h2", "h3", "h4", "h5", "h6",
                "blockquote", "code", "pre", "span", "div",
                "table", "thead", "tbody", "tfoot", "tr", "th", "td", "caption", "colgroup", "col",
                "figure", "figcaption"
            })
                s.AllowedTags.Add(t);

            foreach (var a in new[]
            {
                "href", "src", "alt", "title", "class", "style",
                "width", "height", "align", "valign",
                "colspan", "rowspan", "scope", "target", "rel",
                "data-mce-style", "data-mce-src", "data-mce-href"
            })
                s.AllowedAttributes.Add(a);

            foreach (var c in new[]
            {
                "color", "background-color", "text-align", "vertical-align",
                "font-weight", "font-style", "text-decoration",
                "font-family", "font-size", "line-height",
                "width", "height", "max-width",
                "padding", "padding-top", "padding-right", "padding-bottom", "padding-left",
                "margin", "margin-top", "margin-right", "margin-bottom", "margin-left",
                "border", "border-top", "border-right", "border-bottom", "border-left",
                "border-color", "border-style", "border-width", "border-collapse"
            })
                s.AllowedCssProperties.Add(c);

            s.AllowedSchemes.Add("http");
            s.AllowedSchemes.Add("https");
            s.AllowedSchemes.Add("mailto");
            s.AllowedSchemes.Add("data");                                   // base64 inline image (TinyMCE paste)

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
