using System.Collections.Generic;
using System.Text.Json;
using ReportPanel.Models;

namespace ReportPanel.Tests;

// Plan 05 B — JsonExtensionData round-trip esneklik.
// Frontend'in eklediği typed-olmayan alanlar Extra dict'e düşer, save/load sırasında
// kaybolmadan korunur. Backend o alanı kullanmak istediğinde Extra'dan parse eder.
public class DashboardConfigExtensionDataTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    [Fact]
    public void Component_unknown_field_lands_in_extra()
    {
        var json = """{"type":"kpi","title":"T","tooltip":"hover ipucu","customNumber":42}""";

        var comp = JsonSerializer.Deserialize<DashboardComponent>(json, Options);

        Assert.NotNull(comp);
        Assert.NotNull(comp!.Extra);
        Assert.True(comp.Extra!.ContainsKey("tooltip"));
        Assert.Equal("hover ipucu", comp.Extra["tooltip"].GetString());
        Assert.Equal(42, comp.Extra["customNumber"].GetInt32());
    }

    [Fact]
    public void Component_unknown_field_round_trips_through_serialize()
    {
        var json = """{"type":"table","title":"T","experimentalFlag":true,"tooltip":"x"}""";

        var comp = JsonSerializer.Deserialize<DashboardComponent>(json, Options);
        var serialized = JsonSerializer.Serialize(comp, Options);

        Assert.Contains("\"experimentalFlag\":true", serialized);
        Assert.Contains("\"tooltip\":\"x\"", serialized);
    }

    [Fact]
    public void Config_top_level_unknown_field_round_trips()
    {
        var json = """{"schemaVersion":2,"layout":"standard","tabs":[],"theme":"dark","experimental":{"newFeature":true}}""";

        var cfg = JsonSerializer.Deserialize<DashboardConfig>(json, Options);
        var serialized = JsonSerializer.Serialize(cfg, Options);

        Assert.NotNull(cfg!.Extra);
        Assert.Equal("dark", cfg.Extra!["theme"].GetString());
        Assert.Contains("\"theme\":\"dark\"", serialized);
        Assert.Contains("\"experimental\"", serialized);
    }

    [Fact]
    public void TableColumn_unknown_field_round_trips()
    {
        var json = """{"key":"satis","label":"Satış","tooltip":"birim TL","sortable":true}""";

        var col = JsonSerializer.Deserialize<TableColumnDef>(json, Options);
        var serialized = JsonSerializer.Serialize(col, Options);

        Assert.Equal("birim TL", col!.Extra!["tooltip"].GetString());
        Assert.Contains("\"sortable\":true", serialized);
    }

    [Fact]
    public void Tab_unknown_field_round_trips()
    {
        var json = """{"title":"Genel","components":[],"icon":"fa-chart-bar","collapsed":false}""";

        var tab = JsonSerializer.Deserialize<DashboardTab>(json, Options);
        var serialized = JsonSerializer.Serialize(tab, Options);

        Assert.Equal("fa-chart-bar", tab!.Extra!["icon"].GetString());
        Assert.Contains("\"icon\":\"fa-chart-bar\"", serialized);
    }

    [Fact]
    public void Existing_typed_fields_still_deserialize_correctly()
    {
        // Regression: Extra eklemek typed alanları bozmamalı
        var json = """{"type":"kpi","title":"Test","span":3,"column":"satis","agg":"sum"}""";

        var comp = JsonSerializer.Deserialize<DashboardComponent>(json, Options);

        Assert.Equal("kpi", comp!.Type);
        Assert.Equal("Test", comp.Title);
        Assert.Equal(3, comp.Span);
        Assert.Equal("satis", comp.Column);
        Assert.Equal("sum", comp.Agg);
        // Extra null veya boş — typed alanlar Extra'ya gitmedi
        Assert.True(comp.Extra == null || comp.Extra.Count == 0);
    }

    [Fact]
    public void Full_config_with_extra_round_trips_through_validator()
    {
        // End-to-end: JSON → DashboardConfig (Extra dolu) → tekrar JSON, validator bozulmuyor
        var json = """
        {
          "schemaVersion": 2,
          "tabs": [
            {
              "title": "Genel",
              "components": [
                { "type": "kpi", "title": "K", "result": "rs0", "tooltip": "ipucu", "futureField": 99 }
              ]
            }
          ]
        }
        """;

        var cfg = JsonSerializer.Deserialize<DashboardConfig>(json, Options);
        var roundTripped = JsonSerializer.Serialize(cfg, Options);

        Assert.Contains("tooltip", roundTripped);
        Assert.Contains("futureField", roundTripped);

        // Validator round-tripped JSON'u da kabul eder
        var result = ReportPanel.Services.DashboardConfigValidator.Validate(roundTripped);
        Assert.False(result.HasErrors);
    }
}
