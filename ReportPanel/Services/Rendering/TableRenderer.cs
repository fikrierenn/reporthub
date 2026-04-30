using System.Text;
using System.Text.Json;
using ReportPanel.Models;

namespace ReportPanel.Services.Rendering
{
    // M-11 F-6 (ADR-008): Table widget HTML + tableOptions + kolon format/conditionalFormat.
    // Client-side DOM builder JS (shell) her kolonu textContent ile insert eder (XSS-safe),
    // format/conditionalFormat modlarini cell-level uygular.
    //
    // Bos columns durumunda shell JS SP'nin ilk satir key'lerinden auto-detect yapar
    // (ADR-009 Migration 18 Adim B uyumlu).
    internal static class TableRenderer
    {
        public static void Render(StringBuilder sb, DashboardComponent comp, string spanCls, int rs)
        {
            var cols = (comp.Columns ?? new()).Select(c => new
            {
                key = c.Key,
                label = c.Label,
                align = c.Align,
                color = c.Color ?? "",
                format = c.Format ?? "auto",
                computed = c.Computed == true, // Plan 05.B: client-side "hesaplı" badge
                condFormat = c.ConditionalFormat == null ? null : new
                {
                    mode = c.ConditionalFormat.Mode,
                    color = c.ConditionalFormat.Color ?? ""
                }
            });

            var opts = comp.TableOptions ?? new TableOptions();
            var tblData = JsonSerializer.Serialize(new
            {
                rs,
                cols,
                click = comp.ClickDetail,
                opts = new
                {
                    totalRow = opts.TotalRow,
                    stripe = opts.Stripe,
                    stickyHeader = opts.StickyHeader,
                    clientSearch = opts.ClientSearch,
                    pageSize = opts.PageSize
                }
            });
            tblData = tblData.Replace("\"", "&quot;");

            sb.AppendLine($"<div class='bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden{spanCls}'>");
            sb.AppendLine($"  <div class='px-5 py-3 border-b border-gray-100 flex items-center justify-between gap-2'>");
            sb.AppendLine($"    <h3 class='text-xs font-semibold text-gray-500 uppercase tracking-wide'>{RenderContext.Esc(comp.Title)}</h3>");
            if (opts.ClientSearch)
            {
                sb.AppendLine($"    <input type='search' placeholder='Ara...' data-tbl-search class='text-xs px-2 py-1 border border-gray-200 rounded focus:border-blue-400 focus:outline-none w-40'>");
            }
            sb.AppendLine($"  </div>");
            sb.AppendLine($"  <div class='overflow-x-auto{(opts.StickyHeader ? " max-h-96" : "")}'>");
            sb.AppendLine($"    <table class='w-full text-sm' data-tbl='{tblData}'></table>");
            sb.AppendLine($"  </div>");
            if (opts.PageSize > 0)
            {
                sb.AppendLine($"  <div class='px-5 py-2 border-t border-gray-100 flex items-center justify-between text-xs text-gray-500' data-tbl-pager>");
                sb.AppendLine($"    <span data-tbl-page-info>—</span>");
                sb.AppendLine($"    <div class='flex gap-1'>");
                sb.AppendLine($"      <button class='px-2 py-0.5 border border-gray-200 rounded hover:bg-gray-50' data-tbl-prev>&larr;</button>");
                sb.AppendLine($"      <button class='px-2 py-0.5 border border-gray-200 rounded hover:bg-gray-50' data-tbl-next>&rarr;</button>");
                sb.AppendLine($"    </div>");
                sb.AppendLine($"  </div>");
            }
            sb.AppendLine("</div>");
        }
    }
}
