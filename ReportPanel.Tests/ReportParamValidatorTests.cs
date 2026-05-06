using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using ReportPanel.Models;
using ReportPanel.Services;

namespace ReportPanel.Tests;

/// <summary>
/// M-13 R6.4 regression coverage: ReportParamValidator (static utility).
/// 3 sorumluluk: ParseSchema, NormalizeType, ValidateAndBuild.
/// </summary>
public class ReportParamValidatorTests
{
    // ---- ParseSchema ----

    [Fact]
    public void ParseSchema_returns_empty_list_for_null_or_whitespace()
    {
        Assert.Empty(ReportParamValidator.ParseSchema(null));
        Assert.Empty(ReportParamValidator.ParseSchema(""));
        Assert.Empty(ReportParamValidator.ParseSchema("   "));
    }

    [Fact]
    public void ParseSchema_returns_empty_for_invalid_json()
    {
        Assert.Empty(ReportParamValidator.ParseSchema("{not valid json"));
        Assert.Empty(ReportParamValidator.ParseSchema("[1, 2, 3]")); // dizi obje değil
    }

    [Fact]
    public void ParseSchema_parses_modern_fields_array()
    {
        var json = """
        {
          "fields": [
            {"name": "StartDate", "label": "Başlangıç", "type": "date", "required": true, "default": "today"},
            {"name": "Branch", "label": "Şube", "type": "text", "placeholder": "FSM"}
          ]
        }
        """;
        var fields = ReportParamValidator.ParseSchema(json);

        Assert.Equal(2, fields.Count);
        Assert.Equal("StartDate", fields[0].Name);
        Assert.Equal("Başlangıç", fields[0].Label);
        Assert.Equal("date", fields[0].Type);
        Assert.True(fields[0].Required);
        Assert.Equal("today", fields[0].DefaultValue);
        Assert.Equal("Branch", fields[1].Name);
        Assert.Equal("FSM", fields[1].Placeholder);
        Assert.False(fields[1].Required);
    }

    [Fact]
    public void ParseSchema_parses_legacy_format_with_normalize_type()
    {
        var json = """{ "StartDate": "datetime", "Count": "int", "Active": "bool" }""";
        var fields = ReportParamValidator.ParseSchema(json);

        Assert.Equal(3, fields.Count);
        Assert.Equal("date", fields[0].Type);     // datetime → date
        Assert.Equal("number", fields[1].Type);   // int → number
        Assert.Equal("checkbox", fields[2].Type); // bool → checkbox
    }

    [Fact]
    public void ParseSchema_skips_fields_without_name()
    {
        var json = """{ "fields": [{"name": ""}, {"name": "Valid"}, {"label": "no name"}] }""";
        var fields = ReportParamValidator.ParseSchema(json);

        Assert.Single(fields);
        Assert.Equal("Valid", fields[0].Name);
    }

    [Fact]
    public void ParseSchema_supports_defaultValue_alias()
    {
        var json = """{ "fields": [{"name": "X", "defaultValue": "abc"}] }""";
        var fields = ReportParamValidator.ParseSchema(json);

        Assert.Equal("abc", fields[0].DefaultValue);
    }

    // ---- NormalizeType ----

    [Theory]
    [InlineData("int", "number")]
    [InlineData("integer", "number")]
    [InlineData("decimal", "decimal")]
    [InlineData("float", "decimal")]
    [InlineData("double", "decimal")]
    [InlineData("bit", "checkbox")]
    [InlineData("bool", "checkbox")]
    [InlineData("boolean", "checkbox")]
    [InlineData("date", "date")]
    [InlineData("datetime", "date")]
    [InlineData("DATETIME", "date")]    // case-insensitive
    [InlineData("  int  ", "number")]   // trim
    [InlineData("text", "text")]
    [InlineData("custom", "custom")]    // tanımsız → as-is lowercase
    public void NormalizeType_canonical_mapping(string input, string expected)
    {
        Assert.Equal(expected, ReportParamValidator.NormalizeType(input));
    }

    [Fact]
    public void NormalizeType_null_or_empty_returns_text()
    {
        Assert.Equal("text", ReportParamValidator.NormalizeType(null));
        Assert.Equal("text", ReportParamValidator.NormalizeType(""));
        Assert.Equal("text", ReportParamValidator.NormalizeType("   "));
    }

    // ---- ValidateAndBuild ----

    [Fact]
    public void ValidateAndBuild_required_field_empty_fails()
    {
        var fields = new List<ReportParamField>
        {
            new() { Name = "StartDate", Label = "Başlangıç", Type = "date", Required = true }
        };
        var form = MakeForm(new Dictionary<string, string> { ["StartDate"] = "" });

        var result = ReportParamValidator.ValidateAndBuild(fields, form);

        Assert.False(result.Success);
        Assert.Contains("Başlangıç", result.Errors[0]);
        Assert.Contains("required", result.Errors[0]);
    }

    [Fact]
    public void ValidateAndBuild_invalid_number_fails()
    {
        var fields = new List<ReportParamField>
        {
            new() { Name = "Count", Label = "Adet", Type = "number" }
        };
        var form = MakeForm(new Dictionary<string, string> { ["Count"] = "abc" });

        var result = ReportParamValidator.ValidateAndBuild(fields, form);

        Assert.False(result.Success);
        Assert.Contains("Adet", result.Errors[0]);
    }

    [Fact]
    public void ValidateAndBuild_valid_form_produces_sql_parameters()
    {
        var fields = new List<ReportParamField>
        {
            new() { Name = "Count", Label = "Adet", Type = "number" },
            new() { Name = "Title", Label = "Başlık", Type = "text" }
        };
        var form = MakeForm(new Dictionary<string, string>
        {
            ["Count"] = "42",
            ["Title"] = "Rapor"
        });

        var result = ReportParamValidator.ValidateAndBuild(fields, form);

        Assert.True(result.Success);
        Assert.Empty(result.Errors);
        Assert.Equal(2, result.Parameters.Count);
        Assert.Equal("@Count", result.Parameters[0].ParameterName);
        Assert.Equal(42, result.Parameters[0].Value);
        Assert.Equal("@Title", result.Parameters[1].ParameterName);
        Assert.Equal("Rapor", result.Parameters[1].Value);
        Assert.Contains("Count", result.ParamsJson);
        Assert.Equal("42", result.ParamValues["Count"]);
    }

    [Fact]
    public void ValidateAndBuild_uses_default_value_when_form_empty()
    {
        var fields = new List<ReportParamField>
        {
            new() { Name = "Tag", Label = "Etiket", Type = "text", DefaultValue = "default-tag" }
        };
        var form = MakeForm(new Dictionary<string, string>()); // boş form

        var result = ReportParamValidator.ValidateAndBuild(fields, form);

        Assert.True(result.Success);
        Assert.Equal("default-tag", result.Parameters[0].Value);
    }

    [Fact]
    public void ValidateAndBuild_date_today_default_resolves_to_today()
    {
        var fields = new List<ReportParamField>
        {
            new() { Name = "When", Label = "Tarih", Type = "date", DefaultValue = "today" }
        };
        var form = MakeForm(new Dictionary<string, string>());

        var result = ReportParamValidator.ValidateAndBuild(fields, form);

        Assert.True(result.Success);
        Assert.IsType<DateTime>(result.Parameters[0].Value);
        var date = (DateTime)result.Parameters[0].Value!;
        Assert.Equal(DateTime.Today, date);
    }

    [Fact]
    public void ValidateAndBuild_optional_empty_field_yields_DBNull()
    {
        var fields = new List<ReportParamField>
        {
            new() { Name = "Optional", Label = "Opsiyonel", Type = "text", Required = false }
        };
        var form = MakeForm(new Dictionary<string, string> { ["Optional"] = "" });

        var result = ReportParamValidator.ValidateAndBuild(fields, form);

        Assert.True(result.Success);
        Assert.Equal(DBNull.Value, result.Parameters[0].Value);
    }

    private static IFormCollection MakeForm(Dictionary<string, string> data)
    {
        var dict = new Dictionary<string, StringValues>();
        foreach (var kv in data) dict[kv.Key] = new StringValues(kv.Value);
        return new FormCollection(dict);
    }
}
