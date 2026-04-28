using System.Data;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using ReportPanel.Models;

namespace ReportPanel.Services;

/// <summary>
/// Rapor parametre schema parser + form validation servisi (static utility — DI gerektirmez).
/// 3 sorumluluk: ParamSchemaJson parse, type normalization, form -> SqlParameter validation.
///
/// ReportsController.ParseParamSchema + NormalizeType + ValidateAndBuildParameters +
/// ResolveDefaultValue + ParamValidationResult'tan çıkarıldı (M-13 R6.4, 28 Nisan 2026).
/// </summary>
public static class ReportParamValidator
{
    /// <summary>ReportCatalog.ParamSchemaJson'u alan listesine parse eder. Iki format: { fields: [...] } veya legacy { fieldName: "type" }.</summary>
    public static List<ReportParamField> ParseSchema(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<ReportParamField>();
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                doc.RootElement.TryGetProperty("fields", out var fieldsElement) &&
                fieldsElement.ValueKind == JsonValueKind.Array)
            {
                return fieldsElement
                    .EnumerateArray()
                    .Where(e => e.ValueKind == JsonValueKind.Object)
                    .Select(e => new ReportParamField
                    {
                        Name = e.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                        Label = e.TryGetProperty("label", out var l) ? l.GetString() ?? "" : "",
                        Type = e.TryGetProperty("type", out var t) ? t.GetString() ?? "text" : "text",
                        Required = e.TryGetProperty("required", out var r) && r.GetBoolean(),
                        Placeholder = e.TryGetProperty("placeholder", out var p) ? p.GetString() ?? "" : "",
                        HelpText = e.TryGetProperty("help", out var h) ? h.GetString() ?? "" : "",
                        DefaultValue = e.TryGetProperty("default", out var d)
                            ? d.GetString() ?? ""
                            : (e.TryGetProperty("defaultValue", out var dv) ? dv.GetString() ?? "" : "")
                    })
                    .Where(f => !string.IsNullOrWhiteSpace(f.Name))
                    .ToList();
            }

            // Legacy schema: { "FieldName": "date", "Other": "int" }
            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                var list = new List<ReportParamField>();
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    var type = prop.Value.ValueKind == JsonValueKind.String
                        ? prop.Value.GetString() ?? "text"
                        : "text";
                    list.Add(new ReportParamField
                    {
                        Name = prop.Name,
                        Label = prop.Name,
                        Type = NormalizeType(type),
                        Required = false,
                        Placeholder = "",
                        HelpText = "",
                        DefaultValue = ""
                    });
                }
                return list;
            }
        }
        catch
        {
            return new List<ReportParamField>();
        }

        return new List<ReportParamField>();
    }

    /// <summary>SQL/legacy tip adlarını UI canonical form'una map'ler (int → number, bool → checkbox, vb.).</summary>
    public static string NormalizeType(string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return "text";
        }

        var lower = type.Trim().ToLowerInvariant();
        return lower switch
        {
            "int" or "integer" => "number",
            "decimal" or "float" or "double" => "decimal",
            "bit" or "bool" or "boolean" => "checkbox",
            "date" or "datetime" => "date",
            _ => lower
        };
    }

    /// <summary>Form'dan rapor parametrelerini doğrular ve SqlParameter listesi üretir. ParamsJson + raw value map çıktıda var (audit + UI prefill icin).</summary>
    public static ParamValidationResult ValidateAndBuild(
        List<ReportParamField> fields,
        IFormCollection form)
    {
        var result = new ParamValidationResult { Success = true };
        var paramValues = new Dictionary<string, object?>();
        var rawValues = new Dictionary<string, string>();

        foreach (var field in fields)
        {
            var raw = form[field.Name].ToString();
            if (string.IsNullOrWhiteSpace(raw))
            {
                raw = ResolveDefaultValue(field);
            }
            var isEmpty = string.IsNullOrWhiteSpace(raw);
            rawValues[field.Name] = raw;

            if (field.Required && isEmpty)
            {
                result.Success = false;
                result.Errors.Add($"{field.Label} is required.");
                continue;
            }

            object? value = null;
            var type = NormalizeType(field.Type);

            if (!isEmpty)
            {
                switch (type)
                {
                    case "number":
                        if (int.TryParse(raw, out var intValue))
                        {
                            value = intValue;
                        }
                        else
                        {
                            result.Success = false;
                            result.Errors.Add($"{field.Label} must be a number.");
                        }
                        break;
                    case "decimal":
                        if (decimal.TryParse(raw, out var decimalValue))
                        {
                            value = decimalValue;
                        }
                        else
                        {
                            result.Success = false;
                            result.Errors.Add($"{field.Label} must be a number.");
                        }
                        break;
                    case "checkbox":
                        value = true;
                        break;
                    case "date":
                        if (DateTime.TryParse(raw, out var dateValue))
                        {
                            value = dateValue;
                        }
                        else
                        {
                            result.Success = false;
                            result.Errors.Add($"{field.Label} must be a valid date.");
                        }
                        break;
                    default:
                        value = raw.Trim();
                        break;
                }
            }

            paramValues[field.Name] = value;
        }

        if (!result.Success)
        {
            return result;
        }

        foreach (var kvp in paramValues)
        {
            var param = new SqlParameter("@" + kvp.Key, kvp.Value ?? DBNull.Value);
            result.Parameters.Add(param);
        }

        result.ParamsJson = JsonSerializer.Serialize(paramValues);
        result.ParamValues = rawValues;
        return result;
    }

    private static string ResolveDefaultValue(ReportParamField field)
    {
        if (string.IsNullOrWhiteSpace(field.DefaultValue))
        {
            return string.Empty;
        }

        if (string.Equals(NormalizeType(field.Type), "date", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(field.DefaultValue.Trim(), "today", StringComparison.OrdinalIgnoreCase))
        {
            return DateTime.Today.ToString("yyyy-MM-dd");
        }

        return field.DefaultValue.Trim();
    }
}

public sealed class ParamValidationResult
{
    public bool Success { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<SqlParameter> Parameters { get; set; } = new();
    public string ParamsJson { get; set; } = "{}";
    public Dictionary<string, string> ParamValues { get; set; } = new();
}
