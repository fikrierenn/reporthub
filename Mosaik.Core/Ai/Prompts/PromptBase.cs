using System.Text;

namespace Mosaik.Core.Ai.Prompts
{
    // Plan 16.5 Faz D — Türkçe extraction prompt şablon yardımcısı.
    // Modüle özgü prompt'lar bu sınıfı kullanarak tutarlı JSON çıkış + OCR tolerans direktifleri alır.
    public static class PromptBase
    {
        // Tüm extraction prompt'larında kullanılacak OCR hata tolerans direktifi.
        public const string OcrToleranceDirective = """
            ÖNEMLİ — OCR METNİ KURALLARI:
            Bu metin taranmış bir belgeden OCR ile çıkarılmış olabilir. Aşağıdaki durumlara hazırlıklı ol:
            - Türkçe karakterler bozuk olabilir: ö→o, ü→u, ş→s, ç→c, ğ→g, ı→i veya tam tersi
            - Kelimeler arasında gereksiz boşluklar olabilir: "s ö z l e ş m e" → "sözleşme"
            - Sayılar bölünmüş olabilir: "60 . 000" veya "6 0.000" → 60000
            - Tarihler farklı formatlarda olabilir: "01.01.2024", "1 Ocak 2024", "01/01/2024"
            Bu hataları zihinsel olarak düzelterek gerçek bilgiyi çıkar.
            """;

        // JSON çıkış direktifi — tüm extraction prompt'larında zorunlu.
        public const string JsonOutputDirective = """
            ÇIKTI: Yalnızca geçerli JSON. Markdown kod bloğu (```), önek veya açıklama yazma.
            Emin olmadığın alan için null döndür — asla tahmin etme veya uydurma.
            """;

        // Tarih format direktifi.
        public const string DateFormatDirective = """
            TARİH FORMATI: Tüm tarihler YYYY-MM-DD formatında. Tarih bulunamazsa null.
            """;

        // Prompt oluşturma yardımcısı: domain-specific talimat + standart direktifler.
        public static string BuildSystemPrompt(
            string roleDescription,
            string domainInstructions,
            bool includeOcrTolerance = true,
            bool includeDateFormat = true)
        {
            var sb = new StringBuilder();
            sb.AppendLine(roleDescription);
            sb.AppendLine();
            sb.AppendLine(domainInstructions);

            if (includeOcrTolerance)
            {
                sb.AppendLine();
                sb.AppendLine(OcrToleranceDirective);
            }

            if (includeDateFormat)
            {
                sb.AppendLine();
                sb.AppendLine(DateFormatDirective);
            }

            sb.AppendLine();
            sb.AppendLine(JsonOutputDirective);
            return sb.ToString().Trim();
        }

        // Token tahmini (1 token ≈ 4 karakter, Türkçe için yaklaşık).
        public static int EstimateTokens(string text) =>
            string.IsNullOrEmpty(text) ? 0 : Math.Max(1, text.Length / 4);

        // Uzun metni token limitine göre kırp (son cümleyi kesmemek için son '. ' aranır).
        public static string TruncateToTokenLimit(string text, int maxTokens)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var maxChars = maxTokens * 4;
            if (text.Length <= maxChars) return text;

            var truncated = text[..maxChars];
            var lastSentence = truncated.LastIndexOf(". ", StringComparison.Ordinal);
            return lastSentence > maxChars / 2
                ? truncated[..(lastSentence + 1)]
                : truncated;
        }
    }
}
