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
          "tabs": [ { "components": [ { "type": "kpi", "resultSet": 0 } ] } ]
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
        var r = DashboardConfigValidator.Validate("""{ "schemaVersion": 2, "tabs": [ { "components": [] } ] }""");
        Assert.Contains(r.Errors, e => e.Contains("schemaVersion"));
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
    public void Validate_rejects_negative_widget_resultset()
    {
        var json = """
        {
          "tabs": [ { "components": [ { "type": "kpi", "resultSet": -1 } ] } ]
        }
        """;
        var r = DashboardConfigValidator.Validate(json);
        Assert.Contains(r.Errors, e => e.Contains("negatif"));
    }

    [Fact]
    public void Validate_rejects_invalid_widget_id_format()
    {
        var json = """
        {
          "tabs": [ { "components": [ { "id": "bad-id", "type": "kpi", "resultSet": 0 } ] } ]
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
            { "id": "w_kpi_abcdef", "type": "kpi", "resultSet": 0 },
            { "id": "w_kpi_abcdef", "type": "kpi", "resultSet": 1 }
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
          "tabs": [ { "components": [ { "type": "heatmap", "resultSet": 0 } ] } ]
        }
        """;
        var r = DashboardConfigValidator.Validate(json);
        Assert.False(r.HasErrors);
        Assert.Contains(r.Warnings, w => w.Contains("bilinmeyen bileşen tipi"));
    }

    [Fact]
    public void Validate_warns_orphan_widget_no_binding()
    {
        var json = """
        {
          "tabs": [ { "components": [ { "type": "kpi" } ] } ]
        }
        """;
        var r = DashboardConfigValidator.Validate(json);
        Assert.False(r.HasErrors);
        Assert.Contains(r.Warnings, w => w.Contains("sonuç bağlantısı yok"));
    }
}
