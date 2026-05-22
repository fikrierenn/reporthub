using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using Mosaik.Core.Ai;
using Mosaik.Models;

namespace Mosaik.Services.Ai
{
    public sealed partial class AiExtractionWorker
    {
        // D-02-1 (2026-05-22): InputTokens/OutputTokens eklendi — worker toplamına dahil edilecek.
        public sealed record Stage3Outcome(
            string Json,
            IReadOnlyList<string> FailedFields,
            IReadOnlyList<string> SkippedFields,
            int InputTokens = 0,
            int OutputTokens = 0);

        private async Task<Stage3Outcome> RunStage3RetryAsync(
            IAiSummaryProvider ai,
            string rawText,
            string stage1Json,
            IReadOnlyList<string> nullFields,
            int extractionId,
            CancellationToken ct)
        {
            var current = JsonNode.Parse(stage1Json)?.AsObject();
            if (current is null)
                return new Stage3Outcome(stage1Json, [], []);

            var fields = nullFields.Take(3).ToList();
            var skipped = nullFields.Skip(3).ToList();
            var failed = new List<string>();
            int patchedCount = 0;
            int totalIn = 0, totalOut = 0;

            foreach (var field in fields)
            {
                ct.ThrowIfCancellationRequested();

                var prompt = ExtractionPrompts.GetStage3Prompt(field);
                if (prompt is null)
                {
                    _logger.LogWarning(
                        "Stage3 field={Field} prompt registry'de yok. ExtractionId={Id}", field, extractionId);
                    failed.Add(field);
                    continue;
                }

                try
                {
                    var result = await ai.GenerateAsync(new AiRequest(
                        SystemPrompt: prompt,
                        UserPrompt: ExtractionPrompts.BuildUserPrompt(rawText),
                        RequireJson: true,
                        Purpose: $"contract_extraction_stage3_{field}"
                    ), ct);

                    if (!result.IsSuccess)
                    {
                        _logger.LogWarning(
                            "Stage3 field={Field} AI başarısız. Error={Error} ExtractionId={Id}",
                            field, result.Error, extractionId);
                        failed.Add(field);
                        continue;
                    }

                    var stripped = StripJsonMarkdown(result.RawJson ?? string.Empty);
                    if (string.IsNullOrEmpty(stripped))
                    {
                        _logger.LogWarning(
                            "Stage3 field={Field} AI prose döndürdü, JSON object yok. ExtractionId={Id}",
                            field, extractionId);
                        failed.Add(field);
                        continue;
                    }

                    JsonObject? patch;
                    try
                    {
                        patch = JsonNode.Parse(stripped)?.AsObject();
                    }
                    catch (JsonException jex)
                    {
                        _logger.LogWarning(jex,
                            "Stage3 field={Field} patch JSON parse fail. ExtractionId={Id}",
                            field, extractionId);
                        failed.Add(field);
                        continue;
                    }

                    if (patch is null)
                    {
                        _logger.LogWarning(
                            "Stage3 field={Field} patch object değil (array/primitive). ExtractionId={Id}",
                            field, extractionId);
                        failed.Add(field);
                        continue;
                    }

                    foreach (var kv in patch)
                    {
                        // AI'nın açık null cevabı da geçerli (Stage1 halüsinasyonu temizler).
                        current[kv.Key] = kv.Value?.DeepClone();
                    }

                    totalIn  += result.InputTokens;
                    totalOut += result.OutputTokens;
                    patchedCount++;
                    _logger.LogInformation(
                        "Stage3 field={Field} patch uygulandı. ExtractionId={Id}", field, extractionId);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (HttpRequestException hex)
                {
                    _logger.LogWarning(hex,
                        "Stage3 field={Field} AI network hatası. ExtractionId={Id}", field, extractionId);
                    failed.Add(field);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Stage3 field={Field} beklenmedik hata. ExtractionId={Id}", field, extractionId);
                    failed.Add(field);
                }
            }

            if (skipped.Count > 0)
                _logger.LogInformation(
                    "Stage3 max-3 limit: retry=[{Retry}] skipped=[{Skip}] ExtractionId={Id}",
                    string.Join(",", fields), string.Join(",", skipped), extractionId);

            var json = patchedCount > 0 ? current.ToJsonString() : stage1Json;
            return new Stage3Outcome(json, failed, skipped, totalIn, totalOut);
        }

        private static string StripJsonMarkdown(string raw)
        {
            var trimmed = raw.Trim();
            var start = trimmed.IndexOf('{');
            var end = trimmed.LastIndexOf('}');
            return start >= 0 && end > start ? trimmed[start..(end + 1)] : string.Empty;
        }
    }
}
