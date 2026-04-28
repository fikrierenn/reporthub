using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ReportPanel.Models;
using System.Text.RegularExpressions;

namespace ReportPanel.Services;

/// <summary>
/// Stored procedure keşif servisi: liste / parametre meta / önizleme.
/// AdminController.SpList + ProcParams + SpPreview action'larından çıkarıldı (M-13 R4.1, 28 Nisan 2026).
/// SpPreview kompleks olduğu için ayrı R4.2 pass'inde bu servise eklenecek.
/// </summary>
public class SpExplorerService
{
    private readonly ReportPanelContext _context;
    private readonly ILogger<SpExplorerService> _logger;

    public SpExplorerService(ReportPanelContext context, ILogger<SpExplorerService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>SP listesi: aktif data source'taki user procedure'lar (sys.procedures).</summary>
    public async Task<SpListResult> ListAsync(string dataSourceKey)
    {
        if (string.IsNullOrWhiteSpace(dataSourceKey))
        {
            return new SpListResult(false, "DataSource secilmedi.", Array.Empty<SpInfo>());
        }

        var ds = await _context.DataSources.AsNoTracking()
            .FirstOrDefaultAsync(d => d.DataSourceKey == dataSourceKey && d.IsActive);
        if (ds == null)
        {
            return new SpListResult(false, "DataSource bulunamadi veya pasif.", Array.Empty<SpInfo>());
        }

        var procs = new List<SpInfo>();
        try
        {
            await using var conn = new SqlConnection(ds.ConnString);
            await conn.OpenAsync();
            const string sql = @"
                SELECT SCHEMA_NAME(schema_id) AS SchemaName, name AS ProcName
                FROM sys.procedures
                WHERE is_ms_shipped = 0
                ORDER BY SCHEMA_NAME(schema_id), name";
            await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 15 };
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var schema = reader["SchemaName"]?.ToString() ?? "dbo";
                var name = reader["ProcName"]?.ToString() ?? "";
                var full = schema == "dbo" ? name : $"{schema}.{name}";
                procs.Add(new SpInfo(full, schema, name));
            }
        }
        catch (Exception ex)
        {
            // M-02: connection exception message user'a sızdırılmaz (credentials sızıntı riski).
            _logger.LogWarning(ex, "SpList connection failed for DataSource {DataSourceKey}", dataSourceKey);
            return new SpListResult(false, "Veri kaynağına bağlanılamadı. Bağlantı ayarlarını kontrol edin.", Array.Empty<SpInfo>());
        }

        return new SpListResult(true, null, procs);
    }

    /// <summary>SP'nin parametrelerini çıkarır: name, label, type (UI form için), required.</summary>
    public async Task<SpParamsResult> GetParametersAsync(string dataSourceKey, string procName)
    {
        if (string.IsNullOrWhiteSpace(dataSourceKey) || string.IsNullOrWhiteSpace(procName))
        {
            return new SpParamsResult(false, "Missing parameters.", Array.Empty<SpParamInfo>());
        }

        // SP adı schema.proc veya tek isim olabilir.
        var trimmed = procName.Trim();
        var match = Regex.Match(trimmed, @"^(?<schema>[A-Za-z_][A-Za-z0-9_]*)\.(?<proc>[A-Za-z_][A-Za-z0-9_]*)$");
        string schemaName, procShortName;
        if (match.Success)
        {
            schemaName = match.Groups["schema"].Value;
            procShortName = match.Groups["proc"].Value;
        }
        else if (Regex.IsMatch(trimmed, @"^[A-Za-z_][A-Za-z0-9_]*$"))
        {
            schemaName = "dbo";
            procShortName = trimmed;
        }
        else
        {
            return new SpParamsResult(false, "Invalid procedure name.", Array.Empty<SpParamInfo>());
        }

        var ds = await _context.DataSources.AsNoTracking()
            .FirstOrDefaultAsync(d => d.DataSourceKey == dataSourceKey && d.IsActive);
        if (ds == null)
        {
            return new SpParamsResult(false, "Data source not found.", Array.Empty<SpParamInfo>());
        }

        var parameters = new List<SpParamInfo>();
        const string sql = @"
SELECT p.name, t.name AS type_name, p.has_default_value, p.is_output
FROM sys.parameters p
JOIN sys.objects o ON p.object_id = o.object_id
JOIN sys.types t ON p.user_type_id = t.user_type_id
WHERE o.type IN ('P','PC')
  AND o.name = @ProcName
  AND SCHEMA_NAME(o.schema_id) = @SchemaName
ORDER BY p.parameter_id;";

        try
        {
            await using var connection = new SqlConnection(ds.ConnString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ProcName", procShortName);
            command.Parameters.AddWithValue("@SchemaName", schemaName);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var name = reader.GetString(0);
                if (name.StartsWith("@", StringComparison.Ordinal))
                {
                    name = name.Substring(1);
                }
                var typeName = reader.GetString(1);
                var hasDefault = reader.GetBoolean(2);
                var isOutput = reader.GetBoolean(3);
                var required = !hasDefault && !isOutput;

                parameters.Add(new SpParamInfo(name, name, MapSqlType(typeName), required));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ProcParams query failed for {DataSourceKey}.{ProcName}", dataSourceKey, procName);
            return new SpParamsResult(false, "Parametreler okunamadı.", Array.Empty<SpParamInfo>());
        }

        return new SpParamsResult(true, null, parameters);
    }

    private static string MapSqlType(string sqlType)
    {
        var lower = sqlType.ToLowerInvariant();
        return lower switch
        {
            "int" or "bigint" or "smallint" or "tinyint" => "number",
            "decimal" or "numeric" or "money" or "smallmoney" or "float" or "real" => "decimal",
            "bit" => "checkbox",
            "date" or "datetime" or "datetime2" or "smalldatetime" or "datetimeoffset" => "date",
            _ => "text"
        };
    }
}

public record SpListResult(bool Success, string? Error, IReadOnlyList<SpInfo> Procedures);
public record SpInfo(string Name, string Schema, string ShortName);

public record SpParamsResult(bool Success, string? Error, IReadOnlyList<SpParamInfo> Fields);
public record SpParamInfo(string Name, string Label, string Type, bool Required);
