using System.Text;
using ReportPanel.Models;

namespace ReportPanel.Services.Rendering
{
    // M-11 F-2: Widget fallback placeholder'ları.
    // - MissingResult: ADR-007 binding çözümlenmedi (unknown contract / out-of-bounds / binding yok)
    // - RemovedWidget: ADR-007 bilinmeyen widget type (future-compat, dashboard çökmez)
    internal static class PlaceholderRenderer
    {
        public static void RenderMissingResult(StringBuilder sb, DashboardComponent comp, string spanCls)
        {
            string bindInfo;
            if (!string.IsNullOrEmpty(comp.Result))
                bindInfo = $"result: &quot;{RenderContext.Esc(comp.Result)}&quot;";
            else if (comp.ResultSet.HasValue)
                bindInfo = $"resultSet: {comp.ResultSet.Value}";
            else
                bindInfo = "(binding yok)";

            sb.AppendLine($"<div class='bg-orange-50 border border-orange-200 rounded-xl p-5{spanCls}'>");
            sb.AppendLine($"  <div class='flex items-start gap-3'>");
            sb.AppendLine($"    <i class='fas fa-unlink text-orange-500 mt-1'></i>");
            sb.AppendLine($"    <div>");
            sb.AppendLine($"      <h3 class='text-sm font-semibold text-orange-800'>Veri bağlantısı çözümlenemedi</h3>");
            sb.AppendLine($"      <p class='text-xs text-orange-700 mt-1'>Widget: <code class='bg-orange-100 px-1 rounded'>{RenderContext.Esc(string.IsNullOrWhiteSpace(comp.Title) ? comp.Type : comp.Title)}</code> &middot; {bindInfo}</p>");
            if (!string.IsNullOrWhiteSpace(comp.Id))
                sb.AppendLine($"      <p class='text-xs text-orange-600 mt-1'>Id: <code class='bg-orange-100 px-1 rounded'>{RenderContext.Esc(comp.Id)}</code></p>");
            sb.AppendLine($"    </div>");
            sb.AppendLine($"  </div>");
            sb.AppendLine($"</div>");
        }

        public static void RenderRemovedWidget(StringBuilder sb, DashboardComponent comp, string spanCls)
        {
            sb.AppendLine($"<div class='bg-yellow-50 border border-yellow-200 rounded-xl p-5{spanCls}'>");
            sb.AppendLine($"  <div class='flex items-start gap-3'>");
            sb.AppendLine($"    <i class='fas fa-exclamation-triangle text-yellow-500 mt-1'></i>");
            sb.AppendLine($"    <div>");
            sb.AppendLine($"      <h3 class='text-sm font-semibold text-yellow-800'>Bilinmeyen bileşen tipi</h3>");
            sb.AppendLine($"      <p class='text-xs text-yellow-700 mt-1'>Tip: <code class='bg-yellow-100 px-1 rounded'>{RenderContext.Esc(comp.Type)}</code>");
            if (!string.IsNullOrWhiteSpace(comp.Id))
                sb.Append($" &middot; Id: <code class='bg-yellow-100 px-1 rounded'>{RenderContext.Esc(comp.Id)}</code>");
            sb.AppendLine("</p>");
            if (!string.IsNullOrWhiteSpace(comp.Title))
                sb.AppendLine($"      <p class='text-xs text-yellow-600 mt-1'>Başlık: {RenderContext.Esc(comp.Title)}</p>");
            sb.AppendLine($"    </div>");
            sb.AppendLine($"  </div>");
            sb.AppendLine($"</div>");
        }
    }
}
