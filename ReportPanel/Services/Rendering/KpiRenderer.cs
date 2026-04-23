using System.Text;
using System.Text.Json;
using ReportPanel.Models;

namespace ReportPanel.Services.Rendering
{
    // M-11 F-2: KPI widget HTML. data-kpi attribute ile client-side aggVal() JS'i ile veri bağlanır.
    // F-5'te 4 variant (basic/delta/sparkline/progress) eklenecek — şu an sadece basic.
    internal static class KpiRenderer
    {
        public static void Render(StringBuilder sb, DashboardComponent comp, string spanCls, int rs)
        {
            var c = RenderContext.GetColor(comp.Color);
            var kpiData = JsonSerializer.Serialize(new
            {
                rs,
                agg = comp.Agg,
                col = comp.Column ?? "",
                cond = comp.Condition ?? ""
            });
            kpiData = kpiData.Replace("\"", "&quot;");

            sb.AppendLine($"<div class='bg-white rounded-xl border border-gray-200 shadow-sm p-5{spanCls}'>");
            sb.AppendLine($"  <div class='flex items-center justify-between mb-3'>");
            sb.AppendLine($"    <h3 class='text-xs font-semibold text-gray-500 uppercase tracking-wide'>{RenderContext.Esc(comp.Title)}</h3>");
            sb.AppendLine($"    <div class='w-9 h-9 {c.Bg} rounded-lg flex items-center justify-center'>");
            sb.AppendLine($"      <i class='{RenderContext.Esc(comp.Icon)} text-white text-sm'></i>");
            sb.AppendLine($"    </div>");
            sb.AppendLine($"  </div>");
            sb.AppendLine($"  <div class='text-3xl font-bold {c.Text}' data-kpi='{kpiData}'>—</div>");
            if (!string.IsNullOrWhiteSpace(comp.Subtitle))
                sb.AppendLine($"  <div class='text-xs text-gray-400 mt-1'>{RenderContext.Esc(comp.Subtitle)}</div>");
            sb.AppendLine("</div>");
        }
    }
}
