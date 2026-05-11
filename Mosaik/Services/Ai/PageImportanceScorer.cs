using System.Text.RegularExpressions;

namespace Mosaik.Services.Ai
{
    // Plan 27 Faz A-01: Uzun sözleşmelerde tüm sayfaları full AI'ya göndermek pahalı
    // ve halüsinasyon riski yüksek (lost in the middle). Bu scorer regex + heuristic
    // ile her sayfanın kritiklik skorunu hesaplar. Worker yüksek skorlu sayfaları
    // önceliklendirir, düşük skorluları kırpar veya summarize eder.
    //
    // Patternler Türk sözleşme dili (TBK + ETK + ticari sözleşme jargonu) odaklı.
    // İngilizce sözleşmeler için ek pattern setine geri dönülebilir.
    public sealed class PageImportanceScorer
    {
        private readonly ILogger<PageImportanceScorer> _logger;

        public PageImportanceScorer(ILogger<PageImportanceScorer> logger)
        {
            _logger = logger;
        }

        // Anahtar kelime kategorileri — TR sözleşme dili.
        // Skor: kelime başına puan + sayfa skoru toplamı. Sayfa boyutuna normalize edilmez
        // (kısa kritik madde sayfası uzun ek tabloyu yenmeli).
        private static readonly (Regex Pattern, int Score, string Category)[] Keywords =
        {
            // §5/§6 kritik maddeler — en yüksek
            (new(@"\b(s[üu]re|fesih|fesh[ei]?dilme|sona\s+erme|taahh[üu]t.*?s[üu]re)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), 10, "süre/fesih"),
            (new(@"\b(devir|devred[ei]?lme|temlik)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), 10, "devir"),
            (new(@"\b(m[üu]cbir\s+sebep|force\s+majeure|mucbir)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), 10, "mücbir"),
            (new(@"\b(yetkili\s+mahkeme|uyu[şs]mazl[ıi]k|tahkim|m[üu]nhasir)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), 10, "mahkeme"),

            // Cezai şart + sorumluluk
            (new(@"\b(ceza[ıi]?\s+[şs]art|tazminat|cayma\s+bedel|temerr[üu]t|ihlal)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), 8, "ceza"),
            (new(@"\b(sorumluluk\s+limit|sorumsuzluk|teminat|garanti)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), 7, "sorumluluk"),

            // Taraflar + sözleşme kimliği
            (new(@"\b(taraflar|s[öo]zle[şs]me\s+no|abonelik\s+no|m[üu][şs]teri\s+no|referans\s+no)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), 8, "taraflar/no"),
            (new(@"\b(tan[ıi]mlar|definitions)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), 6, "tanımlar"),

            // Ödeme + faturalama
            (new(@"\b(y[üu]k[üu]ml[üu]l[üu]k|edim|y[üu]k[üu]ml[üu]\b)", RegexOptions.IgnoreCase | RegexOptions.Compiled), 6, "yükümlülük"),
            (new(@"\b([öo]deme|fatura|vade|tahsil|kdv|stopaj)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), 6, "ödeme"),
            (new(@"\b(fiyat|bedel|tarife|[üu]cret|tutar)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), 5, "fiyat"),

            // Gizlilik + KVKK
            (new(@"\b(gizlilik|ifşa|s[ıi]r|kvkk|gdpr|ki[şs]isel\s+veri)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), 6, "gizlilik"),

            // Madde başlığı pattern'leri (§5.1, MADDE 5, 5.1, V. gibi)
            (new(@"^\s*(MADDE\s+\d+|§\s*\d+|\d+\.\s*[A-ZĞÜŞİÖÇ])", RegexOptions.Compiled | RegexOptions.Multiline), 3, "madde başlığı"),

            // İmza/kaşe sayfası işareti — ilk/son sayfada normaldir
            (new(@"\b(imza|ka[şs]e|m[üu]h[üu]r|yetkili\s+ki[şs]i)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), 4, "imza"),

            // Negative — ek/appendix sayfaları öncelik düşür
            (new(@"\b(EK[-\s]\d|tablo\s+\d|liste|appendix|form|adresler)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), -3, "ek/tablo"),
        };

        public PageScore Score(int pageIndex, string pageText)
        {
            if (string.IsNullOrWhiteSpace(pageText))
                return new PageScore(pageIndex, 0, Array.Empty<string>(), pageText?.Length ?? 0);

            // İlk/son sayfa bonus — kapak sayfası ve imza sayfası genelde kritik (taraflar, sözleşme no, tarihler).
            int positionalBonus = pageIndex == 0 ? 6 : 0;

            int total = positionalBonus;
            var hits = new List<string>();

            foreach (var (pattern, score, category) in Keywords)
            {
                var matches = pattern.Matches(pageText).Count;
                if (matches > 0)
                {
                    total += score * Math.Min(matches, 5); // bir sayfada aynı kategori 5+ kez geçerse dur (spam guard)
                    hits.Add($"{category}×{matches}");
                }
            }

            // Çok kısa sayfa (< 100 char) muhtemelen boş veya filigran — skor 0'a kırp.
            if (pageText.Length < 100)
                total = Math.Min(total, 2);

            return new PageScore(pageIndex, total, hits.ToArray(), pageText.Length);
        }

        // Bir liste sayfa skorundan kritik olanları seç.
        // Strateji: top N skorlu sayfa + ilk sayfa + son sayfa zorunlu dahil edilir.
        public IReadOnlyList<int> SelectCriticalPages(IReadOnlyList<PageScore> allPages, int targetCount = 12)
        {
            if (allPages.Count == 0) return Array.Empty<int>();
            if (allPages.Count <= targetCount) return allPages.Select(p => p.PageIndex).ToList();

            var sorted = allPages.OrderByDescending(p => p.Score).Take(targetCount).Select(p => p.PageIndex).ToHashSet();

            // İlk + son sayfa garanti
            sorted.Add(0);
            sorted.Add(allPages.Count - 1);

            var result = sorted.OrderBy(i => i).ToList();
            _logger.LogInformation(
                "PageImportance: {Total} sayfadan {Selected} kritik seçildi: [{Pages}]",
                allPages.Count, result.Count, string.Join(",", result.Select(i => i + 1)));

            return result;
        }

        public sealed record PageScore(int PageIndex, int Score, IReadOnlyList<string> Hits, int TextLength);
    }
}
