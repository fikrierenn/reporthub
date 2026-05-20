using Ganss.Xss;
using Mosaik.Core.Html;

namespace Mosaik.Services
{
    // Plan 39 Faz B — IContentSanitizer implementation (Ganss.Xss.HtmlSanitizer 9.x).
    //
    // Render-time sanitize — defense-in-depth pattern. Yazma sırasında modül-spesifik
    // sanitizer (Mosaik.Modules.Circular.Services.QuillContentSanitizer) DB'ye temiz
    // HTML yazıyor; bu sınıf eski kayıtları + ileride eklenebilecek user-generated
    // alanları (Comment.Body, Document açıklama) render anında bir kez daha geçirir.
    //
    // Whitelist QuillContentSanitizer ile uyumlu — Quill toolbar tag/attr/css setini
    // kapsar. İki sanitize idempotent (zaten temiz HTML değişmez).
    //
    // Thread-safety: HtmlSanitizer instance config immutable + Sanitize stateless;
    // Singleton kayıt güvenli.
    public sealed class HtmlSanitizerContentSanitizer : IContentSanitizer
    {
        private readonly HtmlSanitizer _sanitizer;

        public HtmlSanitizerContentSanitizer()
        {
            _sanitizer = new HtmlSanitizer();

            _sanitizer.AllowedTags.Clear();
            _sanitizer.AllowedAttributes.Clear();
            _sanitizer.AllowedCssProperties.Clear();
            _sanitizer.AllowedSchemes.Clear();

            foreach (var t in new[]
            {
                "p", "br", "strong", "em", "u", "s", "ol", "ul", "li",
                "a", "img", "h1", "h2", "h3", "h4", "h5", "h6",
                "blockquote", "code", "pre", "span", "div"
            })
            {
                _sanitizer.AllowedTags.Add(t);
            }

            foreach (var a in new[] { "href", "src", "alt", "title", "class", "style" })
            {
                _sanitizer.AllowedAttributes.Add(a);
            }

            foreach (var c in new[]
            {
                "color", "background-color", "text-align",
                "font-weight", "font-style", "text-decoration"
            })
            {
                _sanitizer.AllowedCssProperties.Add(c);
            }

            _sanitizer.AllowedSchemes.Add("http");
            _sanitizer.AllowedSchemes.Add("https");
            _sanitizer.AllowedSchemes.Add("mailto");
        }

        public string Sanitize(string? html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return string.Empty;
            }

            return _sanitizer.Sanitize(html);
        }
    }
}
