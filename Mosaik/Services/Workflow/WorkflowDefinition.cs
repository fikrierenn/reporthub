using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mosaik.Services.Workflow
{
    // Plan 36 — sequential-workflow-designer toJSON çıktısı + legacy {steps:[]} formatı parse'i.
    // Designer native: {properties, sequence: [...]}
    // Legacy / test seed: {steps: [...]}
    // İkisi de desteklenir; öncelik: sequence varsa onu kullan.
    public sealed class WorkflowDefinition
    {
        [JsonPropertyName("steps")]
        public List<WorkflowDefinitionStep>? RawSteps { get; set; }

        [JsonPropertyName("sequence")]
        public List<WorkflowDefinitionStep>? RawSequence { get; set; }

        [JsonIgnore]
        public List<WorkflowDefinitionStep> Steps =>
            RawSequence is { Count: > 0 } ? RawSequence
            : RawSteps ?? new List<WorkflowDefinitionStep>();

        public static WorkflowDefinition? Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                return JsonSerializer.Deserialize<WorkflowDefinition>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }

    public sealed class WorkflowDefinitionStep
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        // Designer uses both "type" (custom) and "componentType" (task/container/switch).
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("componentType")]
        public string? ComponentType { get; set; }

        [JsonPropertyName("properties")]
        public Dictionary<string, JsonElement>? Properties { get; set; }
    }
}
