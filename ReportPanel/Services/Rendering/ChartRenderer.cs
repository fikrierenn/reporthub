using System.Text;
using System.Text.Json;
using ReportPanel.Models;

namespace ReportPanel.Services.Rendering
{
    // M-11 F-4 (ADR-008): Chart widget HTML + 10 variant destegi.
    //
    // Variant precedence: comp.Variant > comp.ChartType (legacy v1 fallback).
    // Migration 18 v1->v2 Adim A zaten chartType'i variant'a kopyaladi, ama
    // yeni config'ler variant kullanir, eski deserialize edilmis v1 configler
    // (JSON bozuk + tolere edilen edge case) chartType'a duser.
    //
    // Desteklenen 10 variant (Chart.js 4 native, PLUGIN YOK):
    //   line, area, bar, hbar, stacked, pie, doughnut, radar, polarArea, scatter
    // Client-side init (DashboardShellRenderer.RenderScripts) bu variant'a gore
    // Chart.js type + options map'i secer.
    internal static class ChartRenderer
    {
        public static void Render(StringBuilder sb, DashboardComponent comp, string spanCls, int rs)
        {
            var chartId = "chart_" + Guid.NewGuid().ToString("N")[..8];
            var variant = !string.IsNullOrEmpty(comp.Variant) ? comp.Variant : comp.ChartType;
            if (string.IsNullOrEmpty(variant)) variant = "bar";

            var datasets = (comp.Datasets ?? new()).Select(ds => new
            {
                col = ds.Column,
                label = ds.Label,
                hex = RenderContext.ChartColorHex.GetValueOrDefault(ds.Color, "#3b82f6")
            });

            var axis = comp.AxisOptions ?? new AxisOptions();
            var chartData = JsonSerializer.Serialize(new
            {
                rs,
                variant,
                labelCol = comp.LabelColumn ?? "",
                datasets,
                numberFormat = comp.NumberFormat ?? "auto",
                axis = new
                {
                    showLegend = axis.ShowLegend,
                    showGrid = axis.ShowGrid,
                    beginAtZero = axis.BeginAtZero,
                    tooltip = axis.Tooltip,
                    dataLabels = axis.DataLabels,
                    smooth = axis.Smooth
                }
            });
            chartData = chartData.Replace("\"", "&quot;");

            sb.AppendLine($"<div class='bg-white rounded-xl border border-gray-200 shadow-sm p-5{spanCls}'>");
            sb.AppendLine($"  <h3 class='text-xs font-semibold text-gray-500 uppercase tracking-wide mb-3'>{RenderContext.Esc(comp.Title)}</h3>");
            sb.AppendLine($"  <canvas id='{chartId}' data-chart='{chartData}'></canvas>");
            sb.AppendLine("</div>");
        }
    }
}
