namespace Mosaik.Core.Html
{
    // Plan 39 Faz B — Stored XSS koruma için render-time sanitize sözleşmesi.
    // Block.Content (Plan 17 Tamim), Comment.Body (Plan 35 — geleceği), Document
    // açıklama vb. user-generated rich text alanları DB'ye girmiş olabilir;
    // her render'da güvenli HTML üretmek için bu interface kullanılır.
    //
    // Implementation: Mosaik/Services/HtmlSanitizerContentSanitizer.cs (Ganss.Xss).
    // Singleton kayıt — config immutable, çağrı izole.
    public interface IContentSanitizer
    {
        // null/boş giriş → empty string. Sanitize edilemeyen içerik temizlenip döner.
        string Sanitize(string? html);
    }
}
