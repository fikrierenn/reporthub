using System.Text;
using System.Text.Json;
using ReportPanel.Models;

namespace ReportPanel.Services.Rendering
{
    // M-11 F-2: Table widget HTML. data-tbl attribute ile client-side DOM builder JS'i
    // satırları textContent ile güvenli insert eder (XSS koruması). Boş columns durumunda
    // shell JS auto-detect yapar (ADR-009 Migration 18 Adım B ile uyumlu).
    internal static class TableRenderer
    {
        public static void Render(StringBuilder sb, DashboardComponent comp, string spanCls, int rs)
        {
            var cols = (comp.Columns ?? new()).Select(c => new
            {
                key = c.Key,
                label = c.Label,
                align = c.Align,
                color = c.Color ?? ""
            });
            var tblData = JsonSerializer.Serialize(new
            {
                rs,
                cols,
                click = comp.ClickDetail
            });
            tblData = tblData.Replace("\"", "&quot;");

            sb.AppendLine($"<div class='bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden{spanCls}'>");
            sb.AppendLine($"  <div class='px-5 py-3 border-b border-gray-100'>");
            sb.AppendLine($"    <h3 class='text-xs font-semibold text-gray-500 uppercase tracking-wide'>{RenderContext.Esc(comp.Title)}</h3>");
            sb.AppendLine($"  </div>");
            sb.AppendLine($"  <div class='overflow-x-auto'>");
            sb.AppendLine($"    <table class='w-full text-sm' data-tbl='{tblData}'></table>");
            sb.AppendLine($"  </div>");
            sb.AppendLine("</div>");
        }
    }
}
