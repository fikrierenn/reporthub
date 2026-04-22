using System.Text.RegularExpressions;

namespace ReportPanel.Services
{
    /// <summary>
    /// G-03: Multi-tenant data filter whitelist. FilterKey SP parametre adına dönüştüğü için T-SQL
    /// identifier kurallarına uymalı. FilterValue STRING_SPLIT ile CSV olarak parse ediliyor —
    /// sadece alfanumerik + virgül + tire + alt tire + nokta + boşluk.
    /// </summary>
    public static class UserDataFilterValidator
    {
        public static readonly Regex FilterKeyRegex = new(
            @"^[a-zA-Z_][a-zA-Z0-9_]{0,62}$",
            RegexOptions.Compiled);

        public static readonly Regex FilterValueRegex = new(
            @"^[a-zA-Z0-9,_\-\. ]+$",
            RegexOptions.Compiled);

        public static bool IsValidKey(string? key) =>
            !string.IsNullOrWhiteSpace(key) && FilterKeyRegex.IsMatch(key);

        public static bool IsValidValue(string? value) =>
            !string.IsNullOrWhiteSpace(value) && FilterValueRegex.IsMatch(value);

        public static bool IsValid(string? key, string? value) =>
            IsValidKey(key) && IsValidValue(value);
    }
}
