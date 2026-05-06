using System.Data;
using Microsoft.Data.SqlClient;

namespace ReportPanel.Services;

/// <summary>
/// SQL Server stored procedure execution servisi.
/// Tek result set (`ExecuteAsync`) ve multi result set (`ExecuteMultipleAsync`) varyantları.
/// CommandTimeout 120s — uzun raporlar için. ReadAsync ile satır bazlı stream + DBNull → "".
///
/// ReportsController.ExecuteStoredProcedure + ExecuteStoredProcedureMultiResultSets static helper'larından
/// çıkarıldı (M-13 R6.3, 28 Nisan 2026).
/// </summary>
public class StoredProcedureExecutor
{
    public async Task<SpExecutionResult> ExecuteAsync(
        string connectionString,
        string procName,
        List<SqlParameter> parameters)
    {
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand(procName, connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 120
        };

        if (parameters.Count > 0)
        {
            command.Parameters.AddRange(parameters.ToArray());
        }

        using var reader = await command.ExecuteReaderAsync();
        var result = new SpExecutionResult();

        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object>();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var value = await reader.IsDBNullAsync(i) ? null : reader.GetValue(i);
                row[reader.GetName(i)] = value ?? "";
            }
            result.Rows.Add(row);
        }

        return result;
    }

    public async Task<List<List<Dictionary<string, object>>>> ExecuteMultipleAsync(
        string connectionString,
        string procName,
        List<SqlParameter> parameters)
    {
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand(procName, connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 120
        };

        if (parameters.Count > 0)
        {
            command.Parameters.AddRange(parameters.ToArray());
        }

        using var reader = await command.ExecuteReaderAsync();
        var allResultSets = new List<List<Dictionary<string, object>>>();

        do
        {
            var rows = new List<Dictionary<string, object>>();
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>();
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    var value = await reader.IsDBNullAsync(i) ? null : reader.GetValue(i);
                    row[reader.GetName(i)] = value ?? "";
                }
                rows.Add(row);
            }
            allResultSets.Add(rows);
        } while (await reader.NextResultAsync());

        return allResultSets;
    }
}

public class SpExecutionResult
{
    public List<Dictionary<string, object>> Rows { get; set; } = new();
}
