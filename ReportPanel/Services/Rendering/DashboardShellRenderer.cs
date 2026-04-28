using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ReportPanel.Models;

namespace ReportPanel.Services.Rendering
{
    // M-11 F-2: HTML iskelet + resultSets inject + tabs header + modal + client-side init scripts.
    // Widget-agnostik — sadece shell. Per-widget HTML per-widget renderer'larda.
    // JS: switchTab, showDetail/closeModal, fmtNum, aggVal (KPI), chart init, table init (+ auto-detect cols fallback).
    internal static partial class DashboardShellRenderer
    {
        public static void BeginHtml(StringBuilder sb)
        {
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang='tr'>");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset='UTF-8'>");
            sb.AppendLine("<meta name='viewport' content='width=device-width, initial-scale=1.0'>");
            sb.AppendLine("<script src='https://cdn.tailwindcss.com'></script>");
            sb.AppendLine("<script src='https://cdn.jsdelivr.net/npm/chart.js@4.4.0/dist/chart.umd.min.js'></script>");
            sb.AppendLine("<link rel='stylesheet' href='https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css'>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: -apple-system, 'Segoe UI', Roboto, sans-serif; background: #f9fafb; margin: 0; padding: 20px; }");
            sb.AppendLine(".tab { cursor:pointer; transition: all .15s; }");
            sb.AppendLine(".tab:hover { color: #1d4ed8; }");
            sb.AppendLine(".tab.active { color: #2563eb; border-bottom-color: #2563eb; }");
            sb.AppendLine("tr.clickable { cursor: pointer; transition: background .12s; }");
            sb.AppendLine("tr.clickable:hover { background: #f0f9ff; }");
            sb.AppendLine(".modal-bg { display:none; position:fixed; inset:0; background:rgba(0,0,0,.5); z-index:100; align-items:center; justify-content:center; padding:30px; }");
            sb.AppendLine(".modal-bg.open { display:flex; }");
            sb.AppendLine("canvas { max-height: 260px !important; }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
        }

        public static void InjectResultSets(StringBuilder sb, List<List<Dictionary<string, object>>> resultSets)
        {
            sb.AppendLine("<script>");
            sb.Append("window.__RS = [");
            for (var i = 0; i < resultSets.Count; i++)
            {
                if (i > 0) sb.Append(',');
                var json = JsonSerializer.Serialize(resultSets[i]);
                // </script> break-out (case-insensitive) ve HTML comment örüntüleri kaçırılır
                json = Regex.Replace(json, "</(script)", "<\\/$1", RegexOptions.IgnoreCase);
                json = json.Replace("<!--", "<\\!--");
                sb.Append(json);
            }
            sb.AppendLine("];");
            sb.AppendLine("</script>");
        }

        public static void RenderTabsHeader(StringBuilder sb, DashboardConfig config)
        {
            if (config.Tabs.Count <= 1) return;
            sb.AppendLine("<div class='flex gap-1 mb-5 border-b border-gray-200'>");
            for (var t = 0; t < config.Tabs.Count; t++)
            {
                var active = t == 0 ? " active" : "";
                sb.AppendLine($"<div class='tab px-4 py-2.5 text-sm font-semibold text-gray-500 border-b-2 border-transparent{active}' data-tab='{t}' onclick='switchTab({t})'>{RenderContext.Esc(config.Tabs[t].Title)}</div>");
            }
            sb.AppendLine("</div>");
        }

        // ADR-007 Faz 1: required detect (enforce Faz 4). Eksik zorunlu veri banner.
        public static void RenderRequiredMissingBanner(StringBuilder sb, DashboardConfig config, List<List<Dictionary<string, object>>> resultSets)
        {
            if (config.ResultContract == null || config.ResultContract.Count == 0) return;

            var missingRequired = new List<string>();
            foreach (var kv in config.ResultContract)
            {
                if (!kv.Value.Required) continue;
                var idx = kv.Value.ResultSet;
                if (idx < 0 || idx >= resultSets.Count || resultSets[idx].Count == 0)
                    missingRequired.Add(kv.Key);
            }
            if (missingRequired.Count == 0) return;

            sb.AppendLine("<div class='bg-yellow-50 border border-yellow-300 rounded-lg p-3 mb-4 flex items-start gap-2'>");
            sb.AppendLine("  <i class='fas fa-exclamation-triangle text-yellow-600 mt-0.5'></i>");
            sb.AppendLine($"  <span class='text-sm text-yellow-800'>Eksik zorunlu veri: {RenderContext.Esc(string.Join(", ", missingRequired))}. Dashboard kısmi gösteriliyor.</span>");
            sb.AppendLine("</div>");
        }

        public static string GridColsClass(string? layout) =>
            layout == "compact" ? "grid-cols-2" : layout == "wide" ? "grid-cols-1" : "grid-cols-4";

        public static void RenderModal(StringBuilder sb)
        {
            sb.AppendLine(@"
<div class='modal-bg' id='detailModal' onclick=""if(event.target.id==='detailModal')closeModal()"">
  <div class='bg-white rounded-2xl shadow-xl max-w-3xl w-full max-h-[90vh] overflow-hidden flex flex-col'>
    <div class='px-5 py-4 border-b border-gray-200 flex justify-between items-center'>
      <h3 class='text-lg font-bold text-gray-900' id='modalTitle'>Detay</h3>
      <button onclick='closeModal()' class='text-gray-400 hover:text-gray-700 text-2xl leading-none'>&times;</button>
    </div>
    <div class='p-5 overflow-auto' id='modalBody'></div>
  </div>
</div>");
        }

        public static void EndHtml(StringBuilder sb)
        {
            sb.AppendLine("</body></html>");
        }
    }
}
