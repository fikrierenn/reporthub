using Ganss.Xss;

namespace Mosaik.Modules.Circular.Services
{
    // Plan 17 — Stored-XSS protection for Quill rich text content.
    // Sanitization happens at SAVE time (BlockService) so DB never holds malicious HTML.
    // Whitelist matches Quill toolbar capabilities (formats + color/background style).
    public static class QuillContentSanitizer
    {
        private static readonly HtmlSanitizer _sanitizer = BuildSanitizer();

        private static HtmlSanitizer BuildSanitizer()
        {
            var s = new HtmlSanitizer();

            // Reset defaults to enforce explicit whitelist.
            s.AllowedTags.Clear();
            s.AllowedAttributes.Clear();
            s.AllowedCssProperties.Clear();
            s.AllowedSchemes.Clear();

            // Tags Quill produces.
            foreach (var t in new[]
            {
                "p", "br", "strong", "em", "u", "s", "ol", "ul", "li",
                "a", "img", "h1", "h2", "h3", "h4", "h5", "h6",
                "blockquote", "code", "pre", "span", "div"
            })
            {
                s.AllowedTags.Add(t);
            }

            // Attributes.
            foreach (var a in new[] { "href", "src", "alt", "title", "class", "style" })
            {
                s.AllowedAttributes.Add(a);
            }

            // CSS properties (Quill color/background/align toolbar).
            foreach (var c in new[]
            {
                "color", "background-color", "text-align",
                "font-weight", "font-style", "text-decoration"
            })
            {
                s.AllowedCssProperties.Add(c);
            }

            // URL schemes.
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
    }
}
