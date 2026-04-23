using System.Text.Json;
using System.Text.RegularExpressions;
using ReportPanel.Models;

namespace ReportPanel.Services
{
    // M-10 Faz 3 (ADR-007): DashboardConfigJson save-time validation.
    // Hard errors save'i bloke eder; soft warnings kaydeder ama audit'e yazilir.
    // Runtime enforcement (required result missing, unknown type placeholder) Faz 4'te DashboardRenderer'da.
    public static class DashboardConfigValidator
    {
        private static readonly Regex ContractKeyPattern = new(@"^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled);
        private static readonly Regex WidgetIdPattern = new(@"^w_[a-z]+_[a-z0-9]{6,}$", RegexOptions.Compiled);
        private static readonly HashSet<string> KnownWidgetTypes = new(StringComparer.Ordinal) { "kpi", "chart", "table" };
        private static readonly HashSet<string> KnownShapes = new(StringComparer.Ordinal) { "row", "table" };

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

            // Non-dashboard reports reach here with null — sessizce gec.
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

            // schemaVersion — ADR-007 forward-compat fallback: 0 (unset) tolere edilir, >1 reddedilir.
            if (config.SchemaVersion > 1)
                result.Errors.Add($"Desteklenmeyen schemaVersion: {config.SchemaVersion}. Beklenen: 1.");

            if (config.Tabs == null || config.Tabs.Count == 0)
                result.Errors.Add("En az bir sekme gereklidir.");

            // resultContract: key pattern + duplicate + entry shape.
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

                        // Binding: name-based (hard error if unresolved) vs legacy index vs orphan (soft).
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
    }
}
