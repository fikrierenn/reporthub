using System.Text;
using ReportPanel.Models;
using ReportPanel.Services.Eval;
using ReportPanel.Services.Rendering;

namespace ReportPanel.Services
{
    // M-11 F-2: Dashboard render orkestrasyonu. Önceki 422 satırlık monolithic class
    // Rendering/ altına split edildi (RenderContext + KpiRenderer + ChartRenderer +
    // TableRenderer + PlaceholderRenderer + DashboardShellRenderer). Bu dosya sadece
    // orchestration: shell → widgets → scripts → end.
    //
    // Public API static — mevcut 9 XSS testi ve ReportsController call-site
    // bozulmaz. DI refactor F-7'de (live preview endpoint) yapılacak.
    public static class DashboardRenderer
    {
        public static string Render(DashboardConfig config, List<List<Dictionary<string, object>>> resultSets)
        {
            // Plan 05 Faz 2: render öncesi result set satırlarını CalculatedFields ile zenginleştir.
            // InjectResultSets sonrası client-side JS (window.__RS) zenginleşmiş veriyi görür,
            // tablo body / KPI / chart hepsi computed kolonu doğal olarak okur.
            EnrichWithCalculatedFields(config, resultSets);

            var sb = new StringBuilder();

            DashboardShellRenderer.BeginHtml(sb);
            DashboardShellRenderer.InjectResultSets(sb, resultSets);
            DashboardShellRenderer.RenderTabsHeader(sb, config);
            DashboardShellRenderer.RenderRequiredMissingBanner(sb, config, resultSets);

            var gridCols = DashboardShellRenderer.GridColsClass(config.Layout);

            for (var t = 0; t < config.Tabs.Count; t++)
            {
                var display = t == 0 ? "" : " style='display:none'";
                sb.AppendLine($"<div class='tab-content' id='tab-{t}'{display}>");
                sb.AppendLine($"<div class='grid {gridCols} gap-4'>");

                foreach (var comp in config.Tabs[t].Components)
                {
                    var spanCls = comp.Span > 1 ? $" col-span-{comp.Span}" : "";
                    var rs = config.ResolveResultSet(comp, resultSets.Count);
                    if (rs is null)
                    {
                        PlaceholderRenderer.RenderMissingResult(sb, comp, spanCls);
                        continue;
                    }

                    switch (comp.Type)
                    {
                        case "kpi":
                            KpiRenderer.Render(sb, comp, spanCls, rs.Value);
                            break;
                        case "chart":
                            ChartRenderer.Render(sb, comp, spanCls, rs.Value);
                            break;
                        case "table":
                            TableRenderer.Render(sb, comp, spanCls, rs.Value);
                            break;
                        default:
                            PlaceholderRenderer.RenderRemovedWidget(sb, comp, spanCls);
                            break;
                    }
                }

                sb.AppendLine("</div>");
                sb.AppendLine("</div>");
            }

            DashboardShellRenderer.RenderModal(sb);
            DashboardShellRenderer.RenderScripts(sb);
            DashboardShellRenderer.EndHtml(sb);

            return sb.ToString();
        }

        // Plan 05 Faz 2: Her CalculatedField için AST parse 1 kez, scope'a giren her satırda
        // evaluate edip row dict'e yeni anahtar (cf.Name) ekler. ResultScope null = tüm RS'lere
        // uygula; string verilirse resultContract key'i çözülür → ilgili RS index.
        //
        // Hata politikası (silent_failure_hunter notu): satır-bazlı eval hatası
        // (tanımsız kolon, type mismatch) tek satırı null'a düşürür ama dashboard'u
        // çöktürmez. Plan 05 Done Criteria #5: "computed kolon evaluate fail → cell null".
        // Save-time formula sözdizim hatası DashboardConfigValidator'da yakalanır,
        // bu noktaya gelmez. TryParse fail eden formula da silent skip — config tamamen
        // bozulmuş senaryosu (validator atlanmış).
        private static void EnrichWithCalculatedFields(
            DashboardConfig config,
            List<List<Dictionary<string, object>>> resultSets)
        {
            if (config.CalculatedFields == null || config.CalculatedFields.Count == 0) return;
            if (resultSets.Count == 0) return;

            foreach (var cf in config.CalculatedFields)
            {
                if (string.IsNullOrWhiteSpace(cf.Name) || string.IsNullOrWhiteSpace(cf.Formula)) continue;
                if (!FormulaParser.TryParse(cf.Formula, out var node, out _, out _) || node == null) continue;

                var ev = new FormulaEvaluator(node);

                foreach (var rsIdx in ResolveScope(config, cf.ResultScope, resultSets.Count))
                {
                    var rs = resultSets[rsIdx];
                    foreach (var row in rs)
                    {
                        try
                        {
                            var val = ev.Evaluate(name =>
                                row.TryGetValue(name, out var v)
                                    ? (v is DBNull ? null : v)
                                    : throw new FormulaEvaluationException($"Tanımsız kolon: {name}"));
                            row[cf.Name] = val ?? (object)DBNull.Value;
                        }
                        catch (FormulaEvaluationException)
                        {
                            // Satır-bazlı eval hatası — cell null. Bütün dashboard çökmesi tercih edilemez.
                            row[cf.Name] = DBNull.Value;
                        }
                    }
                }
            }
        }

        private static IEnumerable<int> ResolveScope(DashboardConfig config, string? scope, int rsCount)
        {
            if (string.IsNullOrEmpty(scope))
            {
                for (var i = 0; i < rsCount; i++) yield return i;
                yield break;
            }

            if (config.ResultContract != null
                && config.ResultContract.TryGetValue(scope, out var entry)
                && entry != null
                && entry.ResultSet >= 0
                && entry.ResultSet < rsCount)
            {
                yield return entry.ResultSet;
            }
            // unknown scope → no-op (validator save-time'da yakalar; renderer sessiz)
        }
    }
}
