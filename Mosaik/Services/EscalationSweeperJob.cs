using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Notification;
using Mosaik.Models;

namespace Mosaik.Services
{
    // Plan 54 M4 — Dashboard→Alert sweeper (Hangfire, saatlik).
    // Aktif her EscalationRule için: raporun SP'sini GLOBAL (user-data-filter YOK) çalıştır →
    // rs[ResultSet] üzerinde Aggregation(Column) ile scalar metrik → Operator/Threshold karşılaştır →
    // aşımda INotificationService ile bildirim (ExternalKey günlük dedup — saatlik spam yok).
    // OI (Operational Intelligence) ≠ BI: amaç eşik-aşımı tetikli proaktif bildirim.
    public class EscalationSweeperJob
    {
        private readonly MosaikContext _db;
        private readonly StoredProcedureExecutor _spExecutor;
        private readonly INotificationService _notifications;
        private readonly ILogger<EscalationSweeperJob> _logger;

        public EscalationSweeperJob(
            MosaikContext db,
            StoredProcedureExecutor spExecutor,
            INotificationService notifications,
            ILogger<EscalationSweeperJob> logger)
        {
            _db = db;
            _spExecutor = spExecutor;
            _notifications = notifications;
            _logger = logger;
        }

        public async Task ExecuteAsync(CancellationToken ct = default)
        {
            var rules = await _db.EscalationRules.AsNoTracking()
                .Where(r => r.IsActive)
                .ToListAsync(ct);

            if (rules.Count == 0)
            {
                _logger.LogInformation("EscalationSweeperJob: aktif kural yok.");
                return;
            }

            int evaluated = 0, fired = 0, failed = 0;

            foreach (var rule in rules)
            {
                try
                {
                    var report = await _db.ReportCatalog.AsNoTracking()
                        .Include(r => r.DataSource)
                        .FirstOrDefaultAsync(r => r.ReportId == rule.ReportId, ct);

                    if (report == null || !report.IsActive
                        || report.DataSource == null || !report.DataSource.IsActive)
                    {
                        await RecordErrorAsync(rule.Id, "Rapor veya veri kaynağı pasif/bulunamadı.", ct);
                        failed++;
                        continue;
                    }

                    // GLOBAL değerlendirme — parametresiz çalıştırma (user-data-filter enjekte edilmez).
                    var resultSets = await _spExecutor.ExecuteMultipleAsync(
                        report.DataSource.ConnString, report.ProcName, new List<SqlParameter>());

                    var metric = ComputeMetric(resultSets, rule.ResultSet, rule.Column, rule.Aggregation);
                    if (metric == null)
                    {
                        await RecordErrorAsync(rule.Id,
                            $"Metrik hesaplanamadı (rs={rule.ResultSet}, kolon='{rule.Column}', agg='{rule.Aggregation}').", ct);
                        failed++;
                        continue;
                    }

                    evaluated++;
                    var breached = Compare(metric.Value, rule.Operator, rule.Threshold);

                    if (breached)
                    {
                        var userIds = ParseUserIds(rule.NotifyUserIds);
                        if (userIds.Count > 0)
                        {
                            var today = DateTime.UtcNow;
                            var externalKeyPrefix = $"escalation:{rule.Id}:{today:yyyyMMdd}";
                            var valueStr = metric.Value.ToString("0.##", CultureInfo.InvariantCulture);
                            var thresholdStr = rule.Threshold.ToString("0.##", CultureInfo.InvariantCulture);
                            await _notifications.CreateBulkIfNotExistsAsync(
                                externalKeyPrefix,
                                userIds,
                                entityType: "escalation_rule",
                                entityId: rule.Id,
                                title: $"Eşik aşıldı: {rule.Name}",
                                message: $"{rule.Name} — ölçülen {valueStr}, eşik {OperatorLabel(rule.Operator)} {thresholdStr}.",
                                targetUrl: $"/Reports/Run/{rule.ReportId}",
                                notificationType: "alert",
                                createdBy: "system");
                        }
                        fired++;
                        // Bildirim gönderildikten SONRA durum yazımı best-effort: patlarsa
                        // kullanıcılar zaten uyarıldı; generic catch'e düşüp yanıltıcı "hata"
                        // durumu yazmamalı (günlük dedup tekrar fire'ı engeller).
                        await SafeUpdateAsync(() => RecordFiredAsync(rule.Id, metric.Value, ct), rule.Id, "fired");
                    }
                    else
                    {
                        await SafeUpdateAsync(() => RecordEvaluatedAsync(rule.Id, metric.Value, ct), rule.Id, "evaluated");
                    }
                }
                catch (SqlException sex)
                {
                    failed++;
                    _logger.LogError(sex, "EscalationSweeperJob: RuleId={Id} SP hatası.", rule.Id);
                    await SafeUpdateAsync(() => RecordErrorAsync(rule.Id, "Veritabanı işleminde hata oluştu.", ct), rule.Id, "error");
                }
                catch (Exception ex)
                {
                    failed++;
                    _logger.LogError(ex, "EscalationSweeperJob: RuleId={Id} beklenmedik hata.", rule.Id);
                    await SafeUpdateAsync(() => RecordErrorAsync(rule.Id, "Beklenmedik bir hata oluştu.", ct), rule.Id, "error");
                }
            }

            _logger.LogInformation(
                "EscalationSweeperJob tamamlandı. Kural: {Total}, Değerlendirilen: {Eval}, Tetiklenen: {Fired}, Hata: {Failed}",
                rules.Count, evaluated, fired, failed);
        }

        // rs[resultSet] üzerinde aggregation(column) → scalar. Hesaplanamazsa null.
        public static decimal? ComputeMetric(
            List<List<Dictionary<string, object>>> resultSets, int resultSet, string column, string aggregation)
        {
            if (resultSet < 0 || resultSet >= resultSets.Count)
                return null;

            var rows = resultSets[resultSet];

            if (string.Equals(aggregation, "count", StringComparison.OrdinalIgnoreCase))
                return rows.Count;

            if (rows.Count == 0)
                return null;

            if (string.Equals(aggregation, "first", StringComparison.OrdinalIgnoreCase))
                return rows[0].TryGetValue(column, out var fv) ? ToDecimal(fv) : null;

            var values = new List<decimal>();
            foreach (var row in rows)
                if (row.TryGetValue(column, out var v) && ToDecimal(v) is decimal d)
                    values.Add(d);

            if (values.Count == 0)
                return null;

            return aggregation.ToLowerInvariant() switch
            {
                "sum" => values.Sum(),
                "avg" => values.Average(),
                "min" => values.Min(),
                "max" => values.Max(),
                _ => null
            };
        }

        private static decimal? ToDecimal(object? value)
        {
            if (value == null) return null;
            switch (value)
            {
                case decimal dec: return dec;
                case int i: return i;
                case long l: return l;
                case double db: return (decimal)db;
                case float f: return (decimal)f;
                case short s: return s;
                case byte b: return b;
            }
            var str = value.ToString();
            if (string.IsNullOrWhiteSpace(str)) return null;
            // Yalnız InvariantCulture — tr-TR fallback '.' / ',' anlamını ters çevirip 1000x
            // hatalı parse üretebilirdi (silent-failure). SP kolonları zaten typed döner;
            // bu dal yalnız string'e düşmüş edge için, kültür-belirsizlik riskini almıyoruz.
            return decimal.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var inv) ? inv : null;
        }

        public static bool Compare(decimal value, string op, decimal threshold) => op switch
        {
            "gt" => value > threshold,
            "gte" => value >= threshold,
            "lt" => value < threshold,
            "lte" => value <= threshold,
            "eq" => value == threshold,
            _ => false
        };

        private static string OperatorLabel(string op) => op switch
        {
            "gt" => ">",
            "gte" => "≥",
            "lt" => "<",
            "lte" => "≤",
            "eq" => "=",
            _ => op
        };

        private static List<int> ParseUserIds(string csv)
        {
            var result = new List<int>();
            foreach (var part in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                if (int.TryParse(part, out var id))
                    result.Add(id);
            return result;
        }

        // Durum yazımı best-effort: DB blip'inde tek kuralın yazımı patlasa bile
        // foreach diğer kurallara devam etsin (per-rule izolasyon korunur).
        private async Task SafeUpdateAsync(Func<Task> update, int ruleId, string what)
        {
            try { await update(); }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "EscalationSweeperJob: RuleId={Id} '{What}' durum yazılamadı.", ruleId, what);
            }
        }

        private Task RecordEvaluatedAsync(int ruleId, decimal value, CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            return _db.EscalationRules.Where(r => r.Id == ruleId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.LastValue, value)
                    .SetProperty(r => r.LastEvaluatedAt, now)
                    .SetProperty(r => r.LastError, (string?)null), ct);
        }

        private Task RecordFiredAsync(int ruleId, decimal value, CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            return _db.EscalationRules.Where(r => r.Id == ruleId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.LastValue, value)
                    .SetProperty(r => r.LastEvaluatedAt, now)
                    .SetProperty(r => r.LastFiredAt, now)
                    .SetProperty(r => r.LastError, (string?)null), ct);
        }

        private Task RecordErrorAsync(int ruleId, string error, CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            return _db.EscalationRules.Where(r => r.Id == ruleId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.LastEvaluatedAt, now)
                    .SetProperty(r => r.LastError, error), ct);
        }
    }
}
