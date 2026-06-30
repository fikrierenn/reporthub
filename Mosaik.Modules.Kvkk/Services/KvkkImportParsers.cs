using System.Text;
using System.Text.RegularExpressions;

namespace Mosaik.Modules.Kvkk.Services
{
    // Plan 40 Faz 1 — xlsx serbest-metin alanlarını parse eden saf yardımcılar (DB'siz, test edilebilir).
    public static class KvkkImportParsers
    {
        // "m.5/2/ç (TTK m.390 ...)" → "5/2-ç" ; "m.5/1 (Açık rıza)" → "5/1". Eşleşmezse null.
        private static readonly Regex LegalBasisRe =
            new(@"m\.?\s*(\d+)\s*/\s*(\d+)(?:\s*/\s*([A-Za-zçÇğĞ]))?", RegexOptions.Compiled);

        // Risk: "Düşük..."→0, "Orta"→1, "Yüksek..."→2 (varsayılan Orta).
        public static byte ParseRiskLevel(string? s)
        {
            var n = Normalize(s);
            if (n.Contains("dusuk")) return 0;
            if (n.Contains("yuksek")) return 2;
            return 1;
        }

        public static string? ParseLegalBasisArticle(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            var m = LegalBasisRe.Match(s);
            if (!m.Success) return null;
            var a = m.Groups[1].Value;
            var b = m.Groups[2].Value;
            return m.Groups[3].Success
                ? $"{a}/{b}-{m.Groups[3].Value.ToLowerInvariant()}"
                : $"{a}/{b}";
        }

        // Virgül/noktalı virgül ile ayrılmış listeyi böler (boşları atar).
        public static List<string> SplitList(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return new List<string>();
            return s.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
        }

        // Yurt dışı aktarım var mı (boş / "Yok" / "Hayır" → yok).
        public static bool HasCrossBorder(string? s)
        {
            var n = Normalize(s);
            return n.Length > 0 && n != "yok" && n != "hayir" && n != "yoktur" && n != "-";
        }

        // Aktarım mekanizması tahmini: 1 SCC/Standart, 2 BCR, 3 taahhütname, 4 arızi açık rıza, 5 sözleşme ifası.
        public static byte GuessMechanism(string? s)
        {
            var n = Normalize(s);
            if (n.Contains("standart") || n.Contains("scc")) return 1;
            if (n.Contains("bcr") || n.Contains("baglayici")) return 2;
            if (n.Contains("taahhut")) return 3;
            if (n.Contains("acik riza")) return 4;
            return 5;
        }

        // İlk ülke/alıcı token'ı ("AB/ABD (Microsoft 365) | ..." → "AB/ABD").
        public static string FirstCountry(string? s)
        {
            var t = (s ?? string.Empty).Trim();
            var stop = t.IndexOfAny(new[] { '(', '|', ';' });
            if (stop > 0) t = t[..stop];
            return t.Trim();
        }

        // Türkçe ASCII-fold (ş→s, ü→u, ö→o, ç→c, ğ→g, ı/İ→i) + lower —
        // ASCII keyword eşleşmesi için (örn. "Düşük" → "dusuk").
        private static string Normalize(string? s)
        {
            var t = (s ?? string.Empty).Trim();
            var sb = new StringBuilder(t.Length);
            foreach (var ch in t)
            {
                sb.Append(ch switch
                {
                    'İ' or 'I' or 'ı' => 'i',
                    'ş' or 'Ş' => 's',
                    'ü' or 'Ü' => 'u',
                    'ö' or 'Ö' => 'o',
                    'ç' or 'Ç' => 'c',
                    'ğ' or 'Ğ' => 'g',
                    _ => ch
                });
            }
            return sb.ToString().ToLowerInvariant();
        }
    }
}
