using System.Text.RegularExpressions;

namespace Mosaik.Modules.Kvkk.Services
{
    // Plan 40 (M6) Faz 5 — serbest-metin saklama süresini (RetentionText) yaklaşık güne çevirir.
    // "30-60 gün" / "2 yıl" / "1-3 yıl" / "Saklama + 10 yıl" gibi ifadelerden ÜST sınırı alır.
    // Eşleşme yoksa null (bilinmeyen süre — pattern bunu "ihlal" saymaz, sessizce atlar).
    public static class KvkkDurationParser
    {
        private static readonly Regex UnitPattern = new(
            @"(\d+)\s*(?:-\s*(\d+)\s*)?\s*(gün|ay|yıl)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static int? ParseMaxDays(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            int? maxDays = null;
            foreach (Match m in UnitPattern.Matches(text))
            {
                var high = m.Groups[2].Success ? m.Groups[2].Value : m.Groups[1].Value;
                if (!int.TryParse(high, out var value))
                    continue;

                var days = m.Groups[3].Value.ToLowerInvariant() switch
                {
                    "gün" => value,
                    "ay" => value * 30,
                    "yıl" => value * 365,
                    _ => 0
                };

                if (days > 0 && (maxDays is null || days > maxDays))
                    maxDays = days;
            }

            return maxDays;
        }
    }
}
