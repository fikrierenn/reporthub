using System.Text;
using Mosaik.Models;
using Mosaik.Services.Eval;
using Mosaik.Services.Rendering;

namespace Mosaik.Services
{
    // M-11 F-2: Dashboard render orkestrasyonu. Önceki 422 satırlık monolithic class
    // Rendering/ altına split edildi (RenderContext + KpiRenderer + ChartRenderer +
    // TableRenderer + PlaceholderRenderer + DashboardShellRenderer). Bu dosya sadece
    // orchestration: shell → widgets → scripts → end.
    //
    // F-7: DI instance class (IDashboardRenderer). ReportsController inject eder.
    public class DashboardRenderer : IDashboardRenderer
    {
        public string Render(DashboardConfig config, List<List<Dictionary<string, object>>> resultSets)
        {
            var sb = new StringBuilder();

            // Hesaplı kolonları InjectResultSets'ten ÖNCE çalıştır — serialize sonrası
            // row dict değişiklikleri window.__RS'e yansımaz.
            for (var t = 0; t < config.Tabs.Count; t++)
                foreach (var comp in config.Tabs[t].Components)
                {
                    if (comp.Type != "table") continue;
                    var rs = config.ResolveResultSet(comp, resultSets.Count);
                    if (rs is not null) EnrichTableFormulas(comp, resultSets[rs.Value]);
                }

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

        // Plan 05.B: Tablo widget'ın bağlı RS satırlarını, kolon-bazlı formula'larla
        // zenginleştirir. Sadece formula sahibi kolonlar için satır-bazlı eval; row[col.Key]
        // sonuçla yazılır (yoksa eklenir, varsa override edilir — kullanıcının açık tercihi).
        //
        // Hata politikası: satır-bazlı eval fail → DBNull cell (dashboard çökmez). Save-time
        // validator FormulaParser.TryParse ile sözdizim hatasını yakalar; bu noktaya
        // sözdizim açısından geçerli formula gelir, ama tanımsız kolon / type mismatch
        // runtime'da görülebilir.
        private static void EnrichTableFormulas(DashboardComponent comp, List<Dictionary<string, object>> rs)
        {
            if (comp.Columns == null || comp.Columns.Count == 0) return;

            foreach (var col in comp.Columns)
            {
                if (string.IsNullOrWhiteSpace(col.Formula) || string.IsNullOrWhiteSpace(col.Key)) continue;
                if (!FormulaParser.TryParse(col.Formula, out var node, out _, out _) || node == null) continue;

                var ev = new FormulaEvaluator(node);
                foreach (var row in rs)
                {
                    try
                    {
                        var val = ev.Evaluate(name =>
                        {
                            if (row.TryGetValue(name, out var v))
                                return v is DBNull ? null : v;
                            var match = row.Keys.FirstOrDefault(k => string.Equals(k, name, StringComparison.OrdinalIgnoreCase));
                            if (match != null && row.TryGetValue(match, out var v2))
                                return v2 is DBNull ? null : v2;
                            throw new FormulaEvaluationException($"Tanımsız kolon: {name}");
                        });
                        row[col.Key] = val ?? (object)DBNull.Value;
                    }
                    catch (FormulaEvaluationException)
                    {
                        row[col.Key] = DBNull.Value;
                    }
                }
            }
        }
    }
}
