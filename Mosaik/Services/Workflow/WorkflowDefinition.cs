using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mosaik.Services.Workflow
{
    // Plan 36 — sequential-workflow-designer toJSON çıktısı parse'i.
    // Faz A: sequential step listesi (no-branch). switch/loop adımları ileride genişler.
    public sealed class WorkflowDefinition
    {
        [JsonPropertyName("steps")]
        public List<WorkflowDefinitionStep> Steps { get; set; } = new();

        public static WorkflowDefinition? Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            return JsonSerializer.Deserialize<WorkflowDefinition>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
    }

    public sealed class WorkflowDefinitionStep
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("properties")]
        public Dictionary<string, JsonElement>? Properties { get; set; }
    }
}
