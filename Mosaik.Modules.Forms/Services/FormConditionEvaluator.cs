using System.Text.Json;
using System.Text.Json.Nodes;
using Mosaik.Modules.Forms.Entities;

namespace Mosaik.Modules.Forms.Services
{
    // Plan 56 M-A G3 — koşullu alan görünürlüğü (conditional-logic). ConditionalLogic ölü alandı;
    // artık STRUCTURED tek-koşul JSON tutuyor: {"field":"anonim","op":"eq","value":"Hayır"}.
    // Saf/testable — DB'ye dokunmaz. Danışman kararı (conf 90): expression-string parser YAZILMAZ;
    // structured deserialize + basit karşılaştırma → sıfır injection yüzeyi. survey-core client-side
    // visibleIf'i FormSchemaBuilder emit eder; server bu değerlendirmeyi AUTHORITATIVE tekrar yapar.
    public static class FormConditionEvaluator
    {
        // op: "eq" (eşit) | "ne" (eşit değil). Value ordinal string karşılaştırma.
        public sealed record FormCondition(string Field, string Op, string Value);

        // Bozuk/eksik JSON → null. Tüketen (IsVisible + SchemaBuilder) null'ı "koşul yok" sayar;
        // IsVisible fail-closed yönü = görünür+required (danışman rec: son kullanıcıyı bloklamaz,
        // required'ı sessizce bypass da etmez).
        public static FormCondition? TryParse(string? conditionalLogic)
        {
            if (string.IsNullOrWhiteSpace(conditionalLogic))
                return null;

            JsonNode? node;
            try { node = JsonNode.Parse(conditionalLogic); }
            catch (JsonException) { return null; }

            if (node is not JsonObject o)
                return null;

            // Güvenli çıkarım: GetValue<string>() sayısal/bool JsonValue'da InvalidOperationException
            // fırlatır (JsonException DEĞİL) — legacy/elle-düzenlenmiş {"field":123,...} satırı submit +
            // admin GET'te yakalanmamış 500 üretirdi. AsString null döner, TryParse tam fail-closed→null.
            var field = AsString(o["field"]);
            var op = AsString(o["op"]);
            var value = AsString(o["value"]);

            if (string.IsNullOrWhiteSpace(field) || string.IsNullOrWhiteSpace(value))
                return null;
            if (op is not ("eq" or "ne"))
                return null;

            return new FormCondition(field, op, value);
        }

        // JsonValue string ise değeri döner; sayı/bool/obje/null ise null (GetValue<string> throw etmez).
        private static string? AsString(JsonNode? node)
            => node is JsonValue v && v.TryGetValue(out string? s) ? s : null;

        // Sunucu-otoriter görünürlük: koşul yok/bozuk → görünür (fail-closed = required zorlanır).
        // Koşul varsa referans alanın submitted değerini karşılaştır.
        public static bool IsVisible(FormField field, IReadOnlyDictionary<string, string?> values)
        {
            var cond = TryParse(field.ConditionalLogic);
            if (cond is null)
                return true;

            values.TryGetValue(cond.Field, out var actual);
            var match = string.Equals(actual ?? string.Empty, cond.Value, StringComparison.Ordinal);
            return cond.Op == "eq" ? match : !match;
        }

        // survey-core visibleIf expression'ı (client-side). Value SURVEY-DÜZEY çift-tırnaklı literal'e
        // çevrilir: yalnız `\` ve `"` kaçırılır (expression breakout engellenir). JSON-düzey kaçış +
        // Türkçe karakter, dış `schema.ToJsonString()` + client `JSON.parse` tarafından halledilir —
        // BURADA JsonSerializer kullanmak ÇİFT-escape (`\\u0131`) yapar, survey-core `\u`'yu çözemez,
        // Türkçe koşul (Evet/Hayır) kırılır. Field whitelist regex'ten geçmiş → {field} güvenli.
        public static string? BuildVisibleIf(string? conditionalLogic)
        {
            var cond = TryParse(conditionalLogic);
            if (cond is null)
                return null;

            var escaped = cond.Value.Replace("\\", "\\\\").Replace("\"", "\\\"");
            var op = cond.Op == "eq" ? "=" : "<>";
            return $"{{{cond.Field}}} {op} \"{escaped}\"";
        }
    }
}
