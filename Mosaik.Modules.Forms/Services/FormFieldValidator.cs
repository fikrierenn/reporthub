using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Mosaik.Modules.Forms.Entities;

namespace Mosaik.Modules.Forms.Services
{
    // Plan 41 Faz 1 — server-side authoritative validation (client survey-core validation'ı
    // TEKRARLAR, client'a güvenilmez — security-principles). Saf/testable, DB'ye dokunmaz.
    public static class FormFieldValidator
    {
        // Boş string = değer yok (Required kontrolü ayrı; burası format/uzunluk/regex).
        public static string? Validate(FormField field, string? rawValue)
        {
            if (field.FieldType is FormFieldType.Hidden or FormFieldType.Section)
                return null;

            if (field.IsRequired && string.IsNullOrWhiteSpace(rawValue))
                return $"'{field.Label}' zorunlu bir alan.";

            if (string.IsNullOrWhiteSpace(rawValue))
                return null; // opsiyonel + boş — geçerli

            switch (field.FieldType)
            {
                case FormFieldType.Number:
                    if (!decimal.TryParse(rawValue, out _))
                        return $"'{field.Label}' geçerli bir sayı olmalı.";
                    break;
                case FormFieldType.Date:
                case FormFieldType.DateTime:
                    if (!DateTime.TryParse(rawValue, out _))
                        return $"'{field.Label}' geçerli bir tarih olmalı.";
                    break;
                case FormFieldType.Checkbox:
                    if (!bool.TryParse(rawValue, out _))
                        return $"'{field.Label}' geçerli bir evet/hayır değeri olmalı.";
                    break;
            }

            var ruleError = ValidateRules(field, rawValue);
            if (ruleError != null)
                return ruleError;

            return null;
        }

        private static string? ValidateRules(FormField field, string rawValue)
        {
            if (string.IsNullOrWhiteSpace(field.ValidationRules))
                return null;

            // silent-failure-hunter CRITICAL: bozuk kural fail-open (null="geçerli") olamaz —
            // format kısıtlaması (regex/maxLength) sessizce bypass olurdu. Fail-closed: submit reddedilir.
            JsonNode? rules;
            try { rules = JsonNode.Parse(field.ValidationRules); }
            catch (JsonException) { return $"'{field.Label}' için tanımlı doğrulama kuralı geçersiz — form yöneticisine bildirin."; }

            if (rules is not JsonObject r)
                return $"'{field.Label}' için tanımlı doğrulama kuralı geçersiz — form yöneticisine bildirin.";

            if (r["minLength"] is JsonValue minL && minL.TryGetValue(out int minLength) && rawValue.Length < minLength)
                return $"'{field.Label}' en az {minLength} karakter olmalı.";
            if (r["maxLength"] is JsonValue maxL && maxL.TryGetValue(out int maxLength) && rawValue.Length > maxLength)
                return $"'{field.Label}' en fazla {maxLength} karakter olmalı.";
            if (r["regex"] is JsonValue rx && rx.TryGetValue(out string? pattern) && !string.IsNullOrWhiteSpace(pattern))
            {
                if (!Regex.IsMatch(rawValue, pattern, RegexOptions.None, TimeSpan.FromSeconds(1)))
                    return $"'{field.Label}' beklenen formata uymuyor.";
            }

            return null;
        }
    }
}
