using System.Text;
using System.Text.Json;
using ReportPanel.Models;

namespace ReportPanel.Services.Rendering
{
    // M-11 F-2: Chart widget HTML. data-chart attribute ile Chart.js init JS'i (shell'de)
    // canvas'a bağlanır. F-4'te 10 tip eklenecek — şu an line/bar/pie/doughnut.
    internal static class ChartRenderer
    {
        public static void Render(StringBuilder sb, DashboardComponent comp, string spanCls, int rs)
        {
            var chartId = "chart_" + Guid.NewGuid().ToString("N")[..8];
            var datasets = (comp.Datasets ?? new()).Select(ds => new
            {
                col = ds.Column,
                label = ds.Label,
                hex = RenderContext.ChartColorHex.GetValueOrDefault(ds.Color, "#3b82f6")
            });
            var chartData = JsonSerializer.Serialize(new
            {
                rs,
                type = comp.ChartType,
                labelCol = comp.LabelColumn ?? "",
                datasets
            });
            chartData = chartData.Replace("\"", "&quot;");

            sb.AppendLine($"<div class='bg-white rounded-xl border border-gray-200 shadow-sm p-5{spanCls}'>");
            sb.AppendLine($"  <h3 class='text-xs font-semibold text-gray-500 uppercase tracking-wide mb-3'>{RenderContext.Esc(comp.Title)}</h3>");
            sb.AppendLine($"  <canvas id='{chartId}' data-chart='{chartData}'></canvas>");
            sb.AppendLine("</div>");
        }
    }
}
