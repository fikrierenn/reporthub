namespace Mosaik.Modules.Kvkk.Services
{
    // Plan 40 (M6) — reverse search saf eşleştirme (DB'siz, test edilebilir).
    // "ad soyad" / "iban" / "parmak izi" sorgusu → DataElement code/displayName/alias eşleşmesi.
    public static class DataElementMatcher
    {
        // Türkçe-duyarsız normalize: önce İ→I / ı→i (ToLowerInvariant'ın İ→"i̇" combining-dot
        // bozmasını önlemek için lower'dan ÖNCE), sonra ToLowerInvariant.
        public static string Normalize(string? s) =>
            (s ?? string.Empty).Trim().Replace('İ', 'I').Replace('ı', 'i').ToLowerInvariant();

        // Import yönü (haystack): serbest metin (örn. "Ad, soyad, TC kimlik no") bir veri
        // öğesinin displayName veya alias'ını İÇERİYOR mu? Reverse-search'ün tersi yön.
        public static bool TextContainsElement(string text, string displayName, string? aliases)
        {
            var hay = Normalize(text);
            if (hay.Length == 0) return false;
            if (Normalize(displayName).Length > 0 && hay.Contains(Normalize(displayName)))
                return true;
            if (!string.IsNullOrEmpty(aliases))
                foreach (var a in aliases.Split(',', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries))
                {
                    var na = Normalize(a);
                    if (na.Length >= 2 && hay.Contains(na))
                        return true;
                }
            return false;
        }

        public static bool Matches(string query, string elementCode, string displayName, string? aliases)
        {
            var q = Normalize(query);
            if (q.Length == 0)
                return false;
            if (Normalize(elementCode).Contains(q))
                return true;
            if (Normalize(displayName).Contains(q))
                return true;
            if (!string.IsNullOrEmpty(aliases))
                foreach (var a in aliases.Split(',', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries))
                    if (Normalize(a).Contains(q))
                        return true;
            return false;
        }
    }
}
