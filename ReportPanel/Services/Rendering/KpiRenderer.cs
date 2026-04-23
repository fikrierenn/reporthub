using System.Text;
using System.Text.Json;
using ReportPanel.Models;

namespace ReportPanel.Services.Rendering
{
    // M-11 F-5 (ADR-008): 4 KPI variant — basic/delta/sparkline/progress.
    //
    // C# tarafi her variant'a ozgul HTML iskeleti emit eder; icerigi client-side JS
    // (DashboardShellRenderer.RenderScripts) DOM API + textContent ile doldurur
    // (XSS-safe). Variant null/bos ise 'basic' varsayilir (v1 geriye uyum).
    internal static class KpiRenderer
    {
        public static void Render(StringBuilder sb, DashboardComponent comp, string spanCls, int rs)
        {
            var variant = string.IsNullOrEmpty(comp.Variant) ? "basic" : comp.Variant;

            // Bilinmeyen variant'a dusmesin — validator yakalamistir, burada guvenli fallback.
            if (variant != "basic" && variant != "delta" && variant != "sparkline" && variant != "progress")
                variant = "basic";

            switch (variant)
            {
                case "delta":     RenderDelta(sb, comp, spanCls, rs); return;
                case "sparkline": RenderSparkline(sb, comp, spanCls, rs); return;
                case "progress":  RenderProgress(sb, comp, spanCls, rs); return;
                default:          RenderBasic(sb, comp, spanCls, rs); return;
            }
        }

        // Basic: mevcut v1 iskeleti (buyuk sayi + ikon). Davranis birebir korunur.
        private static void RenderBasic(StringBuilder sb, DashboardComponent comp, string spanCls, int rs)
        {
            var c = RenderContext.GetColor(comp.Color);
            var kpiData = JsonSerializer.Serialize(new
            {
                rs,
                variant = "basic",
                agg = comp.Agg,
                col = comp.Column ?? "",
                cond = comp.Condition ?? "",
                numberFormat = comp.NumberFormat ?? "auto"
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

        // Delta: buyuk sayi + ↑/↓ % + karsilastirma etiketi.
        // data-kpi-delta JSON'unda compareColumn + compareLabel.
        private static void RenderDelta(StringBuilder sb, DashboardComponent comp, string spanCls, int rs)
        {
            var c = RenderContext.GetColor(comp.Color);
            var compareLabel = comp.Delta?.CompareLabel ?? "vs önceki";
            var kpiData = JsonSerializer.Serialize(new
            {
                rs,
                variant = "delta",
                agg = comp.Agg,
                col = comp.Column ?? "",
                cond = comp.Condition ?? "",
                numberFormat = comp.NumberFormat ?? "auto",
                compareColumn = comp.Delta?.CompareColumn ?? "",
                compareLabel
            });
            kpiData = kpiData.Replace("\"", "&quot;");

            sb.AppendLine($"<div class='bg-white rounded-xl border border-gray-200 shadow-sm p-5{spanCls}'>");
            sb.AppendLine($"  <div class='flex items-center justify-between mb-3'>");
            sb.AppendLine($"    <h3 class='text-xs font-semibold text-gray-500 uppercase tracking-wide'>{RenderContext.Esc(comp.Title)}</h3>");
            sb.AppendLine($"    <div class='w-9 h-9 {c.Bg} rounded-lg flex items-center justify-center'>");
            sb.AppendLine($"      <i class='{RenderContext.Esc(comp.Icon)} text-white text-sm'></i>");
            sb.AppendLine($"    </div>");
            sb.AppendLine($"  </div>");
            sb.AppendLine($"  <div class='flex items-baseline gap-2' data-kpi='{kpiData}'>");
            sb.AppendLine($"    <div class='text-3xl font-bold {c.Text}' data-kpi-value>—</div>");
            sb.AppendLine($"    <div class='text-xs font-semibold' data-kpi-delta></div>");
            sb.AppendLine($"  </div>");
            sb.AppendLine($"  <div class='text-xs text-gray-400 mt-1' data-kpi-compare-label>{RenderContext.Esc(compareLabel)}</div>");
            sb.AppendLine("</div>");
        }

        // Sparkline: sayi + mini SVG trend. trend.labelColumn + valueColumn kullanilir.
        private static void RenderSparkline(StringBuilder sb, DashboardComponent comp, string spanCls, int rs)
        {
            var c = RenderContext.GetColor(comp.Color);
            var kpiData = JsonSerializer.Serialize(new
            {
                rs,
                variant = "sparkline",
                agg = comp.Agg,
                col = comp.Column ?? "",
                cond = comp.Condition ?? "",
                numberFormat = comp.NumberFormat ?? "auto",
                trendLabelCol = comp.Trend?.LabelColumn ?? "",
                trendValueCol = comp.Trend?.ValueColumn ?? "",
                colorHex = RenderContext.ChartColorHex.GetValueOrDefault(comp.Color, "#3b82f6")
            });
            kpiData = kpiData.Replace("\"", "&quot;");

            sb.AppendLine($"<div class='bg-white rounded-xl border border-gray-200 shadow-sm p-5{spanCls}'>");
            sb.AppendLine($"  <div class='flex items-center justify-between mb-3'>");
            sb.AppendLine($"    <h3 class='text-xs font-semibold text-gray-500 uppercase tracking-wide'>{RenderContext.Esc(comp.Title)}</h3>");
            sb.AppendLine($"    <div class='w-9 h-9 {c.Bg} rounded-lg flex items-center justify-center'>");
            sb.AppendLine($"      <i class='{RenderContext.Esc(comp.Icon)} text-white text-sm'></i>");
            sb.AppendLine($"    </div>");
            sb.AppendLine($"  </div>");
            sb.AppendLine($"  <div class='flex items-end justify-between gap-3' data-kpi='{kpiData}'>");
            sb.AppendLine($"    <div class='text-3xl font-bold {c.Text}' data-kpi-value>—</div>");
            sb.AppendLine($"    <svg viewBox='0 0 80 26' width='80' height='26' data-kpi-spark></svg>");
            sb.AppendLine($"  </div>");
            if (!string.IsNullOrWhiteSpace(comp.Subtitle))
                sb.AppendLine($"  <div class='text-xs text-gray-400 mt-1'>{RenderContext.Esc(comp.Subtitle)}</div>");
            sb.AppendLine("</div>");
        }

        // Progress: yuzde + hedef bari + "X/Y" metni. progress.targetColumn veya targetValue.
        private static void RenderProgress(StringBuilder sb, DashboardComponent comp, string spanCls, int rs)
        {
            var c = RenderContext.GetColor(comp.Color);
            var kpiData = JsonSerializer.Serialize(new
            {
                rs,
                variant = "progress",
                agg = comp.Agg,
                col = comp.Column ?? "",
                cond = comp.Condition ?? "",
                numberFormat = comp.NumberFormat ?? "auto",
                targetColumn = comp.Progress?.TargetColumn ?? "",
                targetValue = comp.Progress?.TargetValue,
                colorHex = RenderContext.ChartColorHex.GetValueOrDefault(comp.Color, "#3b82f6")
            });
            kpiData = kpiData.Replace("\"", "&quot;");

            sb.AppendLine($"<div class='bg-white rounded-xl border border-gray-200 shadow-sm p-5{spanCls}'>");
            sb.AppendLine($"  <div class='flex items-center justify-between mb-3'>");
            sb.AppendLine($"    <h3 class='text-xs font-semibold text-gray-500 uppercase tracking-wide'>{RenderContext.Esc(comp.Title)}</h3>");
            sb.AppendLine($"    <div class='w-9 h-9 {c.Bg} rounded-lg flex items-center justify-center'>");
            sb.AppendLine($"      <i class='{RenderContext.Esc(comp.Icon)} text-white text-sm'></i>");
            sb.AppendLine($"    </div>");
            sb.AppendLine($"  </div>");
            sb.AppendLine($"  <div data-kpi='{kpiData}'>");
            sb.AppendLine($"    <div class='flex items-baseline justify-between'>");
            sb.AppendLine($"      <div class='text-3xl font-bold {c.Text}' data-kpi-value>—</div>");
            sb.AppendLine($"      <div class='text-xs text-gray-500' data-kpi-progress-text></div>");
            sb.AppendLine($"    </div>");
            sb.AppendLine($"    <div class='w-full h-1.5 bg-gray-200 rounded-full mt-2 overflow-hidden'>");
            sb.AppendLine($"      <div class='h-full rounded-full transition-all' data-kpi-progress-bar style='width:0%; background:{RenderContext.ChartColorHex.GetValueOrDefault(comp.Color, "#3b82f6")}'></div>");
            sb.AppendLine($"    </div>");
            sb.AppendLine($"  </div>");
            if (!string.IsNullOrWhiteSpace(comp.Subtitle))
                sb.AppendLine($"  <div class='text-xs text-gray-400 mt-1'>{RenderContext.Esc(comp.Subtitle)}</div>");
            sb.AppendLine("</div>");
        }
    }
}
