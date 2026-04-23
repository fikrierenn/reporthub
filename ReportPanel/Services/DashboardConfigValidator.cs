using System.Text.Json;
using System.Text.RegularExpressions;
using ReportPanel.Models;

namespace ReportPanel.Services
{
    // M-10 Faz 3 (ADR-007): DashboardConfigJson save-time validation.
    // M-11 F-3 (ADR-008): schema v2 alanları — variant / numberFormat / axisOptions /
    // tableOptions / delta / trend / progress / calculatedFields / conditionalFormat.
    //
    // Hard errors save'i bloke eder; soft warnings kaydeder ama audit'e yazılır.
    // Runtime enforcement (required result missing, unknown type placeholder) Faz 4'te DashboardRenderer'da.
    public static class DashboardConfigValidator
    {
        private static readonly Regex ContractKeyPattern = new(@"^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled);
        private static readonly Regex WidgetIdPattern = new(@"^w_[a-z]+_[a-z0-9]{6,}$", RegexOptions.Compiled);
        private static readonly Regex CalcFieldNamePattern = new(@"^[a-z][a-zA-Z0-9]*$", RegexOptions.Compiled); // camelCase

        private static readonly HashSet<string> KnownWidgetTypes = new(StringComparer.Ordinal) { "kpi", "chart", "table" };
        private static readonly HashSet<string> KnownShapes = new(StringComparer.Ordinal) { "row", "table" };

        // ADR-008 v2: Alt-tip whitelist
        private static readonly HashSet<string> KnownKpiVariants = new(StringComparer.Ordinal)
            { "basic", "delta", "sparkline", "progress" };
        private static readonly HashSet<string> KnownChartVariants = new(StringComparer.Ordinal)
            { "line", "area", "bar", "hbar", "stacked", "pie", "doughnut", "radar", "polarArea", "scatter" };
        private static readonly HashSet<string> KnownNumberFormats = new(StringComparer.Ordinal)
            { "auto", "currency", "currency-short", "percent", "decimal2", "number" };
        private static readonly HashSet<string> KnownTableColFormats = new(StringComparer.Ordinal)
            { "auto", "text", "currency", "number", "date", "percent" };
        private static readonly HashSet<string> KnownConditionalModes = new(StringComparer.Ordinal)
            { "none", "dataBar", "colorScale", "iconUpDown", "negativeRed" };
        private static readonly HashSet<int> KnownPageSizes = new() { 0, 10, 20, 50, 100 };

        // Maximum supported schema version.
        private const int MaxSchemaVersion = 2;

        private static readonly JsonSerializerOptions DeserializeOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public sealed record ValidationResult(List<string> Errors, List<string> Warnings)
        {
            public bool HasErrors => Errors.Count > 0;
            public bool HasWarnings => Warnings.Count > 0;
            public static ValidationResult Ok() => new(new List<string>(), new List<string>());
        }

        public static ValidationResult Validate(string? configJson)
        {
            var result = ValidationResult.Ok();

            if (string.IsNullOrWhiteSpace(configJson))
                return result;

            DashboardConfig? config;
            try
            {
                config = JsonSerializer.Deserialize<DashboardConfig>(configJson, DeserializeOptions);
            }
            catch (JsonException ex)
            {
                result.Errors.Add($"Pano yapılandırması geçerli JSON değil: {ex.Message}");
                return result;
            }

            if (config == null)
            {
                result.Errors.Add("Pano yapılandırması boş veya okunamıyor.");
                return result;
            }

            // schemaVersion: 0 (unset) ve 1 tolere (legacy), 2 aktif, >2 reddedilir.
            if (config.SchemaVersion > MaxSchemaVersion)
                result.Errors.Add($"Desteklenmeyen schemaVersion: {config.SchemaVersion}. Beklenen: 1 veya 2.");

            if (config.Tabs == null || config.Tabs.Count == 0)
                result.Errors.Add("En az bir sekme gereklidir.");

            // resultContract
            if (config.ResultContract != null)
            {
                foreach (var (key, entry) in config.ResultContract)
                {
                    if (string.IsNullOrWhiteSpace(key) || !ContractKeyPattern.IsMatch(key))
                    {
                        result.Errors.Add($"İsim geçersiz: '{key}'. Harf veya alt çizgi ile başlamalı, sadece harf/rakam/alt çizgi içermeli.");
                        continue;
                    }
                    if (entry == null)
                    {
                        result.Errors.Add($"'{key}' isim tanımı boş.");
                        continue;
                    }
                    if (entry.ResultSet < 0)
                        result.Errors.Add($"'{key}' isim tanımı: result set indeksi negatif olamaz ({entry.ResultSet}).");
                    if (!string.IsNullOrEmpty(entry.Shape) && !KnownShapes.Contains(entry.Shape))
                        result.Errors.Add($"'{key}' isim tanımı: shape 'row' veya 'table' olmalı, '{entry.Shape}' geçersiz.");
                }
            }

            // calculatedFields (ADR-008 v2)
            var calcFieldNames = new HashSet<string>(StringComparer.Ordinal);
            if (config.CalculatedFields != null)
            {
                for (var i = 0; i < config.CalculatedFields.Count; i++)
                {
                    var cf = config.CalculatedFields[i];
                    var label = !string.IsNullOrWhiteSpace(cf?.Name)
                        ? $"'{cf!.Name}' türetilmiş alanı"
                        : $"{i + 1}. türetilmiş alan";

                    if (cf == null) { result.Errors.Add($"{label}: tanım boş."); continue; }

                    if (string.IsNullOrWhiteSpace(cf.Name))
                        result.Errors.Add($"{label}: ad boş olamaz.");
                    else if (!CalcFieldNamePattern.IsMatch(cf.Name))
                        result.Errors.Add($"{label}: ad camelCase olmalı (küçük harfle başla, harf/rakam). '{cf.Name}' geçersiz.");
                    else if (!calcFieldNames.Add(cf.Name))
                        result.Errors.Add($"{label}: aynı adda başka bir türetilmiş alan var.");

                    if (string.IsNullOrWhiteSpace(cf.Formula))
                        result.Errors.Add($"{label}: formül boş olamaz.");

                    if (!string.IsNullOrEmpty(cf.Format) && !KnownNumberFormats.Contains(cf.Format))
                        result.Errors.Add($"{label}: format '{cf.Format}' geçersiz. Beklenen: {string.Join(", ", KnownNumberFormats)}.");
                }
            }

            var usedContractKeys = new HashSet<string>(StringComparer.Ordinal);
            var widgetIds = new HashSet<string>(StringComparer.Ordinal);

            if (config.Tabs != null)
            {
                for (var tabIdx = 0; tabIdx < config.Tabs.Count; tabIdx++)
                {
                    var tab = config.Tabs[tabIdx];
                    if (tab?.Components == null) continue;

                    var tabLabel = !string.IsNullOrWhiteSpace(tab.Title)
                        ? $"'{tab.Title}' sekmesi"
                        : $"{tabIdx + 1}. sekme";

                    for (var compIdx = 0; compIdx < tab.Components.Count; compIdx++)
                    {
                        var comp = tab.Components[compIdx];
                        if (comp == null) continue;

                        var compLabel = !string.IsNullOrWhiteSpace(comp.Title)
                            ? $"'{comp.Title}' bileşeni"
                            : $"{compIdx + 1}. bileşen";
                        var widgetLabel = $"{tabLabel} → {compLabel}";

                        if (!string.IsNullOrEmpty(comp.Id))
                        {
                            if (!WidgetIdPattern.IsMatch(comp.Id))
                                result.Errors.Add($"{widgetLabel}: id formatı geçersiz ('{comp.Id}'). Beklenen: w_<tip>_<hash>.");
                            else if (!widgetIds.Add(comp.Id))
                                result.Errors.Add($"{widgetLabel}: id '{comp.Id}' başka bir bileşende de kullanılıyor.");
                        }

                        if (string.IsNullOrEmpty(comp.Type))
                            result.Errors.Add($"{widgetLabel}: bileşen tipi boş olamaz.");
                        else if (!KnownWidgetTypes.Contains(comp.Type))
                            result.Warnings.Add($"{widgetLabel}: bilinmeyen bileşen tipi '{comp.Type}' — runtime'da 'kaldırılmış bileşen' olarak gösterilir.");

                        // ADR-008 v2: variant whitelist per type
                        ValidateVariant(result, widgetLabel, comp);

                        // ADR-008 v2: numberFormat whitelist
                        if (!string.IsNullOrEmpty(comp.NumberFormat) && !KnownNumberFormats.Contains(comp.NumberFormat))
                            result.Errors.Add($"{widgetLabel}: sayı formatı '{comp.NumberFormat}' geçersiz. Beklenen: {string.Join(", ", KnownNumberFormats)}.");

                        // KPI variant-spesifik alt-config gereksinimi
                        ValidateKpiVariantRequirements(result, widgetLabel, comp);

                        // Chart axisOptions (alan yapısı bool — deserialize değerleri zaten bool, sıkı kural yok)
                        // Chart datasets boş ise uyarı
                        if (comp.Type == "chart" && (comp.Datasets == null || comp.Datasets.Count == 0))
                            result.Warnings.Add($"{widgetLabel}: grafik dataset'i tanımlanmamış.");

                        // Table: tableOptions.pageSize + kolon format + conditionalFormat mode whitelist
                        if (comp.Type == "table")
                        {
                            if (comp.TableOptions != null && !KnownPageSizes.Contains(comp.TableOptions.PageSize))
                                result.Errors.Add($"{widgetLabel}: sayfa boyutu {comp.TableOptions.PageSize} geçersiz. Beklenen: {string.Join("/", KnownPageSizes)}.");

                            if (comp.Columns != null)
                            {
                                for (var ci = 0; ci < comp.Columns.Count; ci++)
                                {
                                    var col = comp.Columns[ci];
                                    if (col == null) continue;
                                    var colLabel = !string.IsNullOrWhiteSpace(col.Label) ? $"'{col.Label}' kolonu" : $"{ci + 1}. kolon";

                                    if (!string.IsNullOrEmpty(col.Format) && !KnownTableColFormats.Contains(col.Format))
                                        result.Errors.Add($"{widgetLabel} → {colLabel}: format '{col.Format}' geçersiz. Beklenen: {string.Join(", ", KnownTableColFormats)}.");

                                    if (col.ConditionalFormat != null && !KnownConditionalModes.Contains(col.ConditionalFormat.Mode))
                                        result.Errors.Add($"{widgetLabel} → {colLabel}: koşullu format modu '{col.ConditionalFormat.Mode}' geçersiz. Beklenen: {string.Join(", ", KnownConditionalModes)}.");
                                }
                            }
                        }

                        // Binding
                        if (!string.IsNullOrEmpty(comp.Result))
                        {
                            if (config.ResultContract == null || !config.ResultContract.ContainsKey(comp.Result))
                                result.Errors.Add($"{widgetLabel}: '{comp.Result}' adlı isim tanımı yok.");
                            else
                                usedContractKeys.Add(comp.Result);
                        }
                        else if (comp.ResultSet.HasValue)
                        {
                            if (comp.ResultSet.Value < 0)
                                result.Errors.Add($"{widgetLabel}: result set indeksi negatif olamaz ({comp.ResultSet.Value}).");
                        }
                        else
                        {
                            result.Warnings.Add($"{widgetLabel}: sonuç bağlantısı yok (ne isim ne result set seçilmiş) — runtime'da veri görünmez.");
                        }
                    }
                }
            }

            // Soft: required-declared-but-not-bound.
            if (config.ResultContract != null)
            {
                foreach (var (key, entry) in config.ResultContract)
                {
                    if (entry?.Required == true && !usedContractKeys.Contains(key))
                        result.Warnings.Add($"'{key}' isim tanımı 'required' işaretli ama hiçbir bileşen tarafından kullanılmıyor.");
                }
            }

            return result;
        }

        // ADR-008 v2: KPI {basic/delta/sparkline/progress}, chart {line/area/bar/hbar/stacked/pie/doughnut/radar/polarArea/scatter}.
        // Table için variant yok (null zorunlu).
        private static void ValidateVariant(ValidationResult result, string widgetLabel, DashboardComponent comp)
        {
            if (string.IsNullOrEmpty(comp.Variant))
            {
                // v1 geriye uyumlu: variant null — v2 Migration 18 sonrası dolu gelir, ama eski configler tolerate.
                return;
            }

            if (comp.Type == "kpi")
            {
                if (!KnownKpiVariants.Contains(comp.Variant))
                    result.Errors.Add($"{widgetLabel}: KPI varyantı '{comp.Variant}' geçersiz. Beklenen: {string.Join(", ", KnownKpiVariants)}.");
            }
            else if (comp.Type == "chart")
            {
                if (!KnownChartVariants.Contains(comp.Variant))
                    result.Errors.Add($"{widgetLabel}: grafik tipi '{comp.Variant}' geçersiz. Beklenen: {string.Join(", ", KnownChartVariants)}.");
            }
            else if (comp.Type == "table")
            {
                result.Warnings.Add($"{widgetLabel}: tablo bileşeninde varyant alanı kullanılmaz ('{comp.Variant}' yok sayılıyor).");
            }
        }

        // ADR-008 v2: KPI delta/sparkline/progress variant'ları alt-config gerektirir.
        private static void ValidateKpiVariantRequirements(ValidationResult result, string widgetLabel, DashboardComponent comp)
        {
            if (comp.Type != "kpi" || string.IsNullOrEmpty(comp.Variant)) return;

            switch (comp.Variant)
            {
                case "delta":
                    if (comp.Delta == null || string.IsNullOrWhiteSpace(comp.Delta.CompareColumn))
                        result.Errors.Add($"{widgetLabel}: 'delta' varyantı için karşılaştırma kolonu (delta.compareColumn) zorunludur.");
                    break;

                case "sparkline":
                    if (comp.Trend == null || string.IsNullOrWhiteSpace(comp.Trend.LabelColumn) || string.IsNullOrWhiteSpace(comp.Trend.ValueColumn))
                        result.Errors.Add($"{widgetLabel}: 'sparkline' varyantı için trend.labelColumn ve trend.valueColumn zorunludur.");
                    break;

                case "progress":
                    var hasCol = comp.Progress != null && !string.IsNullOrWhiteSpace(comp.Progress.TargetColumn);
                    var hasVal = comp.Progress != null && comp.Progress.TargetValue.HasValue;
                    if (!hasCol && !hasVal)
                        result.Errors.Add($"{widgetLabel}: 'progress' varyantı için progress.targetColumn veya progress.targetValue'dan biri zorunludur.");
                    break;
            }
        }
    }
}
