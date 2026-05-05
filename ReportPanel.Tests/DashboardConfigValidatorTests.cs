using ReportPanel.Services;

namespace ReportPanel.Tests;

// M-10 Faz 3 (ADR-007): DashboardConfigValidator save-time contract.
// Hard errors save'i bloke eder; soft warnings audit'e dusurulur ama kayit izni verir.
public class DashboardConfigValidatorTests
{
    // ---- Empty / null / non-dashboard ----

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_accepts_null_or_empty(string? json)
    {
        var r = DashboardConfigValidator.Validate(json);
        Assert.False(r.HasErrors);
        Assert.False(r.HasWarnings);
    }

    [Fact]
    public void Validate_rejects_invalid_json()
    {
        var r = DashboardConfigValidator.Validate("{not: valid");
        Assert.True(r.HasErrors);
        Assert.Contains(r.Errors, e => e.Contains("JSON"));
    }

    // ---- Happy path ----

    [Fact]
    public void Validate_accepts_minimal_valid_config()
    {
        var json = """
        {
          "schemaVersion": 1,
          "resultContract": { "summary": { "resultSet": 0 } },
          "tabs": [ { "title": "Genel", "components": [
            { "id": "w_kpi_abcdef", "type": "kpi", "result": "summary" }
          ] } ]
        }
        """;
        var r = DashboardConfigValidator.Validate(json);
        Assert.False(r.HasErrors);
        Assert.False(r.HasWarnings);
    }

    [Fact]
    public void Validate_accepts_legacy_resultset_binding()
    {
        var json = """
        {
          "tabs": [ { "components": [ { "type": "kpi", "result": "rs0" } ] } ]
        }
        """;
        var r = DashboardConfigValidator.Validate(json);
        Assert.False(r.HasErrors);
    }

    // ---- Hard errors ----

    [Fact]
    public void Validate_rejects_empty_tabs()
    {
        var r = DashboardConfigValidator.Validate("""{ "tabs": [] }""");
        Assert.Contains(r.Errors, e => e.Contains("sekme"));
    }

    [Fact]
    public void Validate_rejects_unsupported_schema_version()
    {
        // ADR-008 F-3: MaxSchemaVersion=2. schemaVersion 3+ reddedilir.
        var r = DashboardConfigValidator.Validate("""{ "schemaVersion": 3, "tabs": [ { "components": [] } ] }""");
        Assert.Contains(r.Errors, e => e.Contains("schemaVersion"));
    }

    [Fact]
    public void Validate_accepts_schema_version_2()
    {
        // ADR-008 F-3: v2 konfigurasyon kabul edilir.
        var r = DashboardConfigValidator.Validate("""{ "schemaVersion": 2, "tabs": [ { "title": "Genel", "components": [] } ] }""");
        Assert.DoesNotContain(r.Errors, e => e.Contains("schemaVersion"));
    }

    [Theory]
    [InlineData("1starts_with_digit")]
    [InlineData("has-dash")]
    [InlineData("has space")]
    [InlineData("has.dot")]
    public void Validate_rejects_invalid_contract_key(string key)
    {
        var json = $$"""
        {
          "resultContract": { "{{key}}": { "resultSet": 0 } },
          "tabs": [ { "components": [] } ]
        }
        """;
        var r = DashboardConfigValidator.Validate(json);
        Assert.Contains(r.Errors, e => e.Contains("İsim geçersiz"));
    }

    [Fact]
    public void Validate_rejects_negative_contract_resultset()
    {
        var json = """
        {
          "resultContract": { "summary": { "resultSet": -1 } },
          "tabs": [ { "components": [] } ]
        }
        """;
        var r = DashboardConfigValidator.Validate(json);
        Assert.Contains(r.Errors, e => e.Contains("negatif"));
    }

    [Fact]
    public void Validate_rejects_invalid_shape()
    {
        var json = """
        {
          "resultContract": { "summary": { "resultSet": 0, "shape": "matrix" } },
          "tabs": [ { "components": [] } ]
        }
        """;
        var r = DashboardConfigValidator.Validate(json);
        Assert.Contains(r.Errors, e => e.Contains("shape"));
    }

    [Fact]
    public void Validate_rejects_widget_result_not_in_contract()
    {
        var json = """
        {
          "resultContract": { "summary": { "resultSet": 0 } },
          "tabs": [ { "components": [ { "type": "kpi", "result": "missing" } ] } ]
        }
        """;
        var r = DashboardConfigValidator.Validate(json);
        Assert.Contains(r.Errors, e => e.Contains("adlı isim tanımı yok"));
    }

[Fact]
    public void Validate_rejects_invalid_widget_id_format()
    {
        var json = """
        {
          "tabs": [ { "components": [ { "id": "bad-id", "type": "kpi", "result": "rs0" } ] } ]
        }
        """;
        var r = DashboardConfigValidator.Validate(json);
        Assert.Contains(r.Errors, e => e.Contains("id formatı"));
    }

    [Fact]
    public void Validate_rejects_duplicate_widget_ids()
    {
        var json = """
        {
          "tabs": [ { "components": [
            { "id": "w_kpi_abcdef", "type": "kpi", "result": "rs0" },
            { "id": "w_kpi_abcdef", "type": "kpi", "result": "rs1" }
          ] } ]
        }
        """;
        var r = DashboardConfigValidator.Validate(json);
        Assert.Contains(r.Errors, e => e.Contains("başka bir bileşende"));
    }

    // ---- Soft warnings ----

    [Fact]
    public void Validate_warns_required_contract_unused()
    {
        var json = """
        {
          "resultContract": {
            "summary": { "resultSet": 0, "required": true },
            "detail":  { "resultSet": 1 }
          },
          "tabs": [ { "components": [ { "type": "kpi", "result": "detail" } ] } ]
        }
        """;
        var r = DashboardConfigValidator.Validate(json);
        Assert.False(r.HasErrors);
        Assert.Contains(r.Warnings, w => w.Contains("'summary'") && w.Contains("required"));
    }

    [Fact]
    public void Validate_warns_unknown_widget_type()
    {
        var json = """
        {
          "tabs": [ { "components": [ { "type": "heatmap", "result": "rs0" } ] } ]
        }
        """;
        var r = DashboardConfigValidator.Validate(json);
        Assert.False(r.HasErrors);
        Assert.Contains(r.Warnings, w => w.Contains("bilinmeyen bileşen tipi"));
    }

    // ADR-007 Faz 6: NoBinding artık hard error (V2 builder default `result: "rs0"` set eder, eksikse save bloklanır).
    [Fact]
    public void Validate_errors_widget_no_binding()
    {
        var json = """
        {
          "tabs": [ { "components": [ { "type": "kpi" } ] } ]
        }
        """;
        var r = DashboardConfigValidator.Validate(json);
        Assert.True(r.HasErrors);
        Assert.Contains(r.Errors, e => e.Contains("sonuç bağlantısı yok"));
    }

    // ============================================================
    // ADR-008 F-3 · Schema v2 yeni alanlar
    // ============================================================

    [Theory]
    [InlineData("basic")]
    [InlineData("delta")]
    [InlineData("sparkline")]
    [InlineData("progress")]
    public void Validate_accepts_known_kpi_variants(string variant)
    {
        // Her variant için alt-config sağla (delta/sparkline/progress için zorunlu)
        var extras = variant switch
        {
            "delta" => @", ""delta"": {""compareColumn"": ""prev""}",
            "sparkline" => @", ""trend"": {""labelColumn"": ""t"", ""valueColumn"": ""v""}",
            "progress" => @", ""progress"": {""targetValue"": 100}",
            _ => ""
        };
        var json = $$"""
        {
          "schemaVersion": 2,
          "tabs": [ { "components": [ { "type": "kpi", "variant": "{{variant}}", "result": "rs0" {{extras}} } ] } ]
        }
        """;
        var r = DashboardConfigValidator.Validate(json);
        Assert.DoesNotContain(r.Errors, e => e.Contains("KPI varyantı"));
    }

    [Fact]
    public void Validate_rejects_unknown_kpi_variant()
    {
        var json = """
        {
          "schemaVersion": 2,
          "tabs": [ { "components": [ { "type": "kpi", "variant": "supersonic", "result": "rs0" } ] } ]
        }
        """;
        var r = DashboardConfigValidator.Validate(json);
        Assert.Contains(r.Errors, e => e.Contains("KPI varyantı") && e.Contains("supersonic"));
    }

    [Theory]
    [InlineData("line")]
    [InlineData("area")]
    [InlineData("bar")]
    [InlineData("hbar")]
    [InlineData("stacked")]
    [InlineData("pie")]
    [InlineData("doughnut")]
    [InlineData("radar")]
    [InlineData("polarArea")]
    [InlineData("scatter")]
    public void Validate_accepts_known_chart_variants(string variant)
    {
        var json = $$"""
        {
          "schemaVersion": 2,
          "tabs": [ { "components": [ { "type": "chart", "variant": "{{variant}}", "result": "rs0" } ] } ]
        }
        """;
        var r = DashboardConfigValidator.Validate(json);
        Assert.DoesNotContain(r.Errors, e => e.Contains("grafik tipi"));
    }

    [Fact]
    public void Validate_rejects_unknown_chart_variant()
    {
        var json = """
        {
          "schemaVersion": 2,
          "tabs": [ { "components": [ { "type": "chart", "variant": "sankey", "result": "rs0" } ] } ]
        }
        """;
        var r = DashboardConfigValidator.Validate(json);
        Assert.Contains(r.Errors, e => e.Contains("grafik tipi") && e.Contains("sankey"));
    }

    [Fact]
    public void Validate_rejects_unknown_number_format()
    {
        var json = """
        {
          "schemaVersion": 2,
          "tabs": [ { "components": [ { "type": "kpi", "numberFormat": "hex", "result": "rs0" } ] } ]
        }
        """;
        var r = DashboardConfigValidator.Validate(json);
        Assert.Contains(r.Errors, e => e.Contains("sayı formatı"));
    }

    [Fact]
    public void Validate_rejects_kpi_delta_without_compareColumn()
    {
        var json = """
        {
          "schemaVersion": 2,
          "tabs": [ { "components": [ { "type": "kpi", "variant": "delta", "result": "rs0" } ] } ]
        }
        """;
        var r = DashboardConfigValidator.Validate(json);
        Assert.Contains(r.Errors, e => e.Contains("delta") && e.Contains("compareColumn"));
    }

    [Fact]
    public void Validate_rejects_kpi_sparkline_without_trend_columns()
    {
        var json = """
        {
          "schemaVersion": 2,
          "tabs": [ { "components": [ { "type": "kpi", "variant": "sparkline", "trend": { "labelColumn": "t" }, "result": "rs0" } ] } ]
        }
        """;
        var r = DashboardConfigValidator.Validate(json);
        Assert.Contains(r.Errors, e => e.Contains("sparkline") && e.Contains("trend"));
    }

    [Fact]
    public void Validate_rejects_kpi_progress_without_target()
    {
        var json = """
        {
          "schemaVersion": 2,
          "tabs": [ { "components": [ { "type": "kpi", "variant": "progress", "result": "rs0" } ] } ]
        }
        """;
        var r = DashboardConfigValidator.Validate(json);
        Assert.Contains(r.Errors, e => e.Contains("progress"));
    }

    [Fact]
    public void Validate_rejects_invalid_table_pageSize()
    {
        var json = """
        {
          "schemaVersion": 2,
          "tabs": [ { "components": [ { "type": "table", "result": "rs0", "tableOptions": { "pageSize": 7 } } ] } ]
        }
        """;
        var r = DashboardConfigValidator.Validate(json);
        Assert.Contains(r.Errors, e => e.Contains("sayfa boyutu"));
    }

    [Fact]
    public void Validate_rejects_invalid_conditional_format_mode()
    {
        var json = """
        {
          "schemaVersion": 2,
          "tabs": [ { "components": [ { "type": "table", "result": "rs0", "columns": [ { "key": "a", "label": "A", "conditionalFormat": { "mode": "rainbow" } } ] } ] } ]
        }
        """;
        var r = DashboardConfigValidator.Validate(json);
        Assert.Contains(r.Errors, e => e.Contains("koşullu format"));
    }

    [Fact]
    public void Validate_rejects_invalid_calculated_field_name()
    {
        var json = """
        {
          "schemaVersion": 2,
          "calculatedFields": [ { "name": "Has-Dash", "formula": "a+b" } ],
          "tabs": [ { "components": [] } ]
        }
        """;
        var r = DashboardConfigValidator.Validate(json);
        Assert.Contains(r.Errors, e => e.Contains("camelCase"));
    }

    [Fact]
    public void Validate_rejects_duplicate_calculated_field_name()
    {
        var json = """
        {
          "schemaVersion": 2,
          "calculatedFields": [
            { "name": "deltaCiro", "formula": "a-b" },
            { "name": "deltaCiro", "formula": "c-d" }
          ],
          "tabs": [ { "components": [] } ]
        }
        """;
        var r = DashboardConfigValidator.Validate(json);
        Assert.Contains(r.Errors, e => e.Contains("aynı adda"));
    }

    [Fact]
    public void Validate_rejects_calculated_field_empty_formula()
    {
        var json = """
        {
          "schemaVersion": 2,
          "calculatedFields": [ { "name": "x", "formula": "" } ],
          "tabs": [ { "components": [] } ]
        }
        """;
        var r = DashboardConfigValidator.Validate(json);
        Assert.Contains(r.Errors, e => e.Contains("formül"));
    }
}
