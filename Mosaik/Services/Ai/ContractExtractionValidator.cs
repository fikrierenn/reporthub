using System.Text.Json;

namespace Mosaik.Services.Ai
{
    public sealed class ContractExtractionValidator
    {
        public static readonly string[] CriticalFields =
            ["counterparty", "startDate", "endDate", "parties",
             "contractValue", "jurisdiction", "obligations"];

        public sealed record Result(
            IReadOnlyList<string> NullFields,
            IReadOnlyList<string> Warnings);

        public Result Validate(string? stage1Json)
        {
            var nullFields = new List<string>();
            var warnings = new List<string>();

            if (string.IsNullOrWhiteSpace(stage1Json))
            {
                nullFields.AddRange(CriticalFields);
                return new Result(nullFields, warnings);
            }

            try
            {
                using var doc = JsonDocument.Parse(stage1Json);
                var root = doc.RootElement;

                if (!HasString(root, "counterparty"))
                    nullFields.Add("counterparty");

                if (!HasString(root, "startDate"))
                    nullFields.Add("startDate");

                if (!HasString(root, "endDate"))
                    nullFields.Add("endDate");

                if (!HasArray(root, "parties", minLength: 1))
                    nullFields.Add("parties");

                if (!HasNumber(root, "totalAmount"))
                    nullFields.Add("contractValue");

                if (!HasString(root, "jurisdiction"))
                    nullFields.Add("jurisdiction");

                if (!HasArray(root, "obligations", minLength: 1))
                    nullFields.Add("obligations");

                if (root.TryGetProperty("parties", out var partiesEl)
                    && partiesEl.ValueKind == JsonValueKind.Array
                    && partiesEl.GetArrayLength() < 2)
                    warnings.Add("parties: 2'den az taraf çıkarıldı");
            }
            catch (JsonException)
            {
                nullFields.AddRange(CriticalFields);
                warnings.Add("Stage1 JSON parse edilemedi — tüm alanlar eksik sayıldı");
            }

            return new Result(nullFields, warnings);
        }

        private static bool HasString(JsonElement el, string prop) =>
            el.TryGetProperty(prop, out var v)
            && v.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(v.GetString());

        private static bool HasNumber(JsonElement el, string prop) =>
            el.TryGetProperty(prop, out var v)
            && (v.ValueKind == JsonValueKind.Number
                || (v.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(v.GetString())));

        private static bool HasArray(JsonElement el, string prop, int minLength = 1) =>
            el.TryGetProperty(prop, out var v)
            && v.ValueKind == JsonValueKind.Array
            && v.GetArrayLength() >= minLength;
    }
}
