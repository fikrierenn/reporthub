using System.Text.Json;
using Mosaik.Models;

namespace Mosaik.Services.Ai
{
    public sealed partial class AiExtractionWorker
    {
        private (List<ContractAiSuggestionDraft> Drafts, string? ParseError) ParseDrafts(string? rawJson)
        {
            var list = new List<ContractAiSuggestionDraft>();
            if (string.IsNullOrWhiteSpace(rawJson)) return (list, null);

            try
            {
                using var doc = JsonDocument.Parse(rawJson);
                var root = doc.RootElement;

                if (root.TryGetProperty("obligations", out var oa) && oa.ValueKind == JsonValueKind.Array)
                {
                    foreach (var o in oa.EnumerateArray())
                        list.Add(new ContractAiSuggestionDraft(
                            SuggestionType.Obligation,
                            Title: GetStr(o, "title") ?? "Yükümlülük",
                            Description: BuildObligationDescription(o),
                            Confidence: ParseConfidence(GetStr(o, "confidence")),
                            DataJson: o.GetRawText()));
                }

                if (root.TryGetProperty("events", out var ea) && ea.ValueKind == JsonValueKind.Array)
                {
                    foreach (var e in ea.EnumerateArray())
                    {
                        var date = GetStr(e, "date");
                        var desc = GetStr(e, "description");
                        list.Add(new ContractAiSuggestionDraft(
                            SuggestionType.ContractEvent,
                            Title: GetStr(e, "title") ?? "Tarih",
                            Description: string.IsNullOrWhiteSpace(date) ? desc : $"{date} — {desc}",
                            Confidence: ParseConfidence(GetStr(e, "confidence")),
                            DataJson: e.GetRawText()));
                    }
                }

                if (root.TryGetProperty("risks", out var ra) && ra.ValueKind == JsonValueKind.Array)
                {
                    foreach (var r in ra.EnumerateArray())
                        list.Add(new ContractAiSuggestionDraft(
                            SuggestionType.RiskWarning,
                            Title: GetStr(r, "title") ?? "Risk",
                            Description: GetStr(r, "description"),
                            Confidence: ParseConfidence(GetStr(r, "severity")),
                            DataJson: r.GetRawText()));
                }
            }
            catch (JsonException jex)
            {
                _logger.LogWarning(jex,
                    "AI öneri JSON parse başarısız — extraction sıfır öneriyle kaydediliyor. RawJson(ilk 200): {Raw}",
                    rawJson?[..Math.Min(200, rawJson.Length)]);
                return (list, $"AI öneri JSON parse başarısız: {jex.GetType().Name}.");
            }
            return (list, null);
        }

        private (List<AiSuggestion> Suggestions, string? ParseError) BuildSuggestions(int extractionId, int firmaId, string? rawJson)
        {
            var (drafts, parseError) = ParseDrafts(rawJson);
            var now = DateTime.UtcNow;
            var suggestions = drafts.Select(d => new AiSuggestion
            {
                ExtractionId = extractionId,
                FirmaId = firmaId,
                SuggestionType = d.SuggestionType,
                Title = Truncate(d.Title, 200) ?? string.Empty,
                Description = Truncate(d.Description, 2000),
                Confidence = d.Confidence,
                Status = SuggestionStatus.Pending,
                SuggestionDataJson = d.DataJson,
                CreatedAt = now,
                UpdatedAt = now
            }).ToList();
            return (suggestions, parseError);
        }

        private sealed record ContractAiSuggestionDraft(
            SuggestionType SuggestionType, string Title, string? Description, Confidence Confidence, string? DataJson);

        private static string BuildObligationDescription(JsonElement o)
        {
            var desc = GetStr(o, "description") ?? "";
            var amount = o.TryGetProperty("amount", out var a) && a.ValueKind == JsonValueKind.Number && a.TryGetDecimal(out var d) ? d : (decimal?)null;
            var currency = GetStr(o, "currency");
            var dueDate = GetStr(o, "dueDate");
            var recurrence = GetStr(o, "recurrenceType");

            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(desc)) parts.Add(desc);
            if (amount.HasValue) parts.Add($"Tutar: {amount:N2} {currency ?? "TRY"}");
            if (!string.IsNullOrWhiteSpace(dueDate)) parts.Add($"Vade: {dueDate}");
            if (!string.IsNullOrWhiteSpace(recurrence)) parts.Add($"Periyot: {recurrence}");
            return string.Join(" · ", parts);
        }

        private static string? GetStr(JsonElement el, string prop) =>
            el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(v.GetString())
                ? v.GetString() : null;

        private static Confidence ParseConfidence(string? s)
        {
            var v = s?.Trim().ToLowerInvariant();
            if (v == null) return Confidence.Medium;
            if (v.StartsWith("high") || v.Contains("yüksek")) return Confidence.High;
            if (v.StartsWith("low")  || v.Contains("düşük"))  return Confidence.Low;
            return Confidence.Medium;
        }

        private static string? Truncate(string? s, int max) =>
            string.IsNullOrEmpty(s) ? s : (s.Length <= max ? s : s[..max]);
    }
}
