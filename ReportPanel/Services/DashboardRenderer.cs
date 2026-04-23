using System.Text;
using ReportPanel.Models;
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
    }
}
