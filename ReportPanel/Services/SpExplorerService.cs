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

    /// <summary>
    /// SP'yi tip-bazlı default'larla çalıştır + sonuç set'lerini döndür.
    /// Admin override (paramsJson dict) belirli parametrelerin default değerini geçersiz kılar.
    /// maxRows clamp 1..100; truncated true ise SP daha fazla satır döndü ama kesildi.
    /// </summary>
    public async Task<SpPreviewResult> PreviewAsync(
        string dataSourceKey,
        string procName,
        int maxRows,
        Dictionary<string, string>? overrides)
    {
        if (string.IsNullOrWhiteSpace(dataSourceKey) || string.IsNullOrWhiteSpace(procName))
        {
            return new SpPreviewResult(false, "DataSource ve ProcName gerekli.", Array.Empty<SpPreviewResultSet>());
        }

        var ds = await _context.DataSources.AsNoTracking()
            .FirstOrDefaultAsync(d => d.DataSourceKey == dataSourceKey && d.IsActive);
        if (ds == null)
        {
            return new SpPreviewResult(false, "DataSource bulunamadi.", Array.Empty<SpPreviewResultSet>());
        }

        // maxRows guvenlik clamp: 1..100
        if (maxRows < 1) maxRows = 10;
        if (maxRows > 100) maxRows = 100;

        var resolvedOverrides = overrides ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var resultSets = new List<SpPreviewResultSet>();

        try
        {
            await using var conn = new SqlConnection(ds.ConnString);
            await conn.OpenAsync();

            // SP parametrelerini meta'dan oku, tip-bazli sensible default + admin override.
            var paramList = await BuildSpParametersAsync(conn, procName, resolvedOverrides);

            await using var cmd = new SqlCommand(procName, conn)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 30
            };
            if (paramList.Count > 0) cmd.Parameters.AddRange(paramList.ToArray());

            await using var reader = await cmd.ExecuteReaderAsync();
            var rsIndex = 0;
            do
            {
                var columns = new List<SpPreviewColumn>();
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    columns.Add(new SpPreviewColumn(reader.GetName(i), reader.GetFieldType(i)?.Name ?? "object"));
                }

                var rows = new List<Dictionary<string, object?>>();
                var rowCount = 0;
                while (await reader.ReadAsync() && rowCount < maxRows)
                {
                    var row = new Dictionary<string, object?>();
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        row[reader.GetName(i)] = await reader.IsDBNullAsync(i) ? null : reader.GetValue(i);
                    }
                    rows.Add(row);
                    rowCount++;
                }
                // Sayim icin kalan satirlari oku (truncated flag).
                var totalRows = rowCount;
                while (await reader.ReadAsync()) totalRows++;

                resultSets.Add(new SpPreviewResultSet(rsIndex, columns, rows, totalRows, totalRows > maxRows));
                rsIndex++;
            } while (await reader.NextResultAsync());
        }
        catch (SqlException sx)
        {
            // Admin-only endpoint: SQL hatasi admin'e gosterilmek SP debug icin faydali.
            return new SpPreviewResult(false, $"SQL hatası ({sx.Number}): {sx.Message}", resultSets);
        }
        catch (Exception ex)
        {
            // M-02: generic connection exception user'a sizintisiz.
            _logger.LogWarning(ex, "SpPreview failed for {DataSourceKey}.{ProcName}", dataSourceKey, procName);
            return new SpPreviewResult(false, "Veri kaynağına bağlanılamadı.", resultSets);
        }

        return new SpPreviewResult(true, null, resultSets);
    }

    private static async Task<List<SqlParameter>> BuildSpParametersAsync(
        SqlConnection conn,
        string procName,
        Dictionary<string, string> overrides)
    {
        var paramList = new List<SqlParameter>();
        try
        {
            await using var metaCmd = new SqlCommand(
                @"SELECT PARAMETER_NAME, DATA_TYPE
                  FROM INFORMATION_SCHEMA.PARAMETERS
                  WHERE SPECIFIC_NAME = @sp
                  ORDER BY ORDINAL_POSITION", conn)
            { CommandTimeout = 10 };
            // SCHEMA.NAME formatinda gelirse kisa adi al.
            var shortName = procName.Contains('.') ? procName[(procName.LastIndexOf('.') + 1)..] : procName;
            metaCmd.Parameters.AddWithValue("@sp", shortName);
            await using var metaReader = await metaCmd.ExecuteReaderAsync();
            while (await metaReader.ReadAsync())
            {
                var pname = metaReader["PARAMETER_NAME"]?.ToString() ?? "";
                if (string.IsNullOrWhiteSpace(pname)) continue;
                var ptype = metaReader["DATA_TYPE"]?.ToString()?.ToLowerInvariant() ?? "";

                // F-02: SP NULL kabul etmezse patlamasin diye tip-bazli sensible default.
                object defaultValue = ptype switch
                {
                    "date" or "datetime" or "datetime2" or "smalldatetime" or "datetimeoffset" => DateTime.UtcNow.Date,
                    "time" => TimeSpan.Zero,
                    "int" or "bigint" or "smallint" or "tinyint" => 0,
                    "bit" => false,
                    "decimal" or "numeric" or "money" or "smallmoney" or "float" or "real" => 0m,
                    "char" or "varchar" or "nchar" or "nvarchar" or "text" or "ntext" => string.Empty,
                    "uniqueidentifier" => Guid.Empty,
                    _ => DBNull.Value
                };

                // F-02 override: admin'in verdigi deger varsa tip'e cast ederek default yerine kullan.
                var pnameClean = pname.TrimStart('@');
                object finalValue = defaultValue;
                if (overrides.TryGetValue(pnameClean, out var overrideRaw) && !string.IsNullOrWhiteSpace(overrideRaw))
                {
                    finalValue = ptype switch
                    {
                        "date" or "datetime" or "datetime2" or "smalldatetime" or "datetimeoffset"
                            => DateTime.TryParse(overrideRaw, out var d) ? (object)d : defaultValue,
                        "time"
                            => TimeSpan.TryParse(overrideRaw, out var t) ? (object)t : defaultValue,
                        "int" or "bigint" or "smallint" or "tinyint"
                            => long.TryParse(overrideRaw, out var n) ? (object)n : defaultValue,
                        "bit"
                            => overrideRaw == "1" || overrideRaw.Equals("true", StringComparison.OrdinalIgnoreCase),
                        "decimal" or "numeric" or "money" or "smallmoney" or "float" or "real"
                            => decimal.TryParse(overrideRaw, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var m) ? (object)m : defaultValue,
                        "uniqueidentifier"
                            => Guid.TryParse(overrideRaw, out var g) ? (object)g : defaultValue,
                        _ => overrideRaw
                    };
                }

                paramList.Add(new SqlParameter(pname, finalValue));
            }
        }
        catch
        {
            // Parametre çıkarma başarısız olursa yine de SP parametresiz denenecek.
        }
        return paramList;
    }
}

public record SpPreviewResult(bool Success, string? Error, IReadOnlyList<SpPreviewResultSet> ResultSets);
public record SpPreviewResultSet(int Index, IReadOnlyList<SpPreviewColumn> Columns, IReadOnlyList<Dictionary<string, object?>> Rows, int RowCount, bool Truncated);
public record SpPreviewColumn(string Name, string Type);

public record SpListResult(bool Success, string? Error, IReadOnlyList<SpInfo> Procedures);
public record SpInfo(string Name, string Schema, string ShortName);

public record SpParamsResult(bool Success, string? Error, IReadOnlyList<SpParamInfo> Fields);
public record SpParamInfo(string Name, string Label, string Type, bool Required);
