using System.Text;
using System.Text.Json;
using ReportPanel.Models;

namespace ReportPanel.Services
{
    public static class DashboardRenderer
    {
        private static readonly Dictionary<string, (string Bg, string Text, string Border, string Light)> ColorMap = new()
        {
            ["blue"]   = ("bg-blue-600",   "text-blue-600",   "border-blue-200", "bg-blue-50"),
            ["green"]  = ("bg-emerald-600", "text-emerald-600","border-emerald-200","bg-emerald-50"),
            ["red"]    = ("bg-red-600",     "text-red-600",    "border-red-200",  "bg-red-50"),
            ["yellow"] = ("bg-amber-500",   "text-amber-600",  "border-amber-200","bg-amber-50"),
            ["gray"]   = ("bg-gray-600",    "text-gray-600",   "border-gray-200", "bg-gray-50"),
            ["indigo"] = ("bg-indigo-600",  "text-indigo-600", "border-indigo-200","bg-indigo-50"),
            ["purple"] = ("bg-purple-600",  "text-purple-600", "border-purple-200","bg-purple-50"),
        };

        private static readonly Dictionary<string, string> ChartColorHex = new()
        {
            ["blue"]   = "#3b82f6",
            ["green"]  = "#10b981",
            ["red"]    = "#ef4444",
            ["yellow"] = "#f59e0b",
            ["gray"]   = "#6b7280",
            ["indigo"] = "#6366f1",
            ["purple"] = "#a855f7",
        };

        public static string Render(DashboardConfig config, List<List<Dictionary<string, object>>> resultSets)
        {
            var sb = new StringBuilder();

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

            // Inject data as a single array under window.__RS (eval kullanimi kaldirildi)
            sb.AppendLine("<script>");
            sb.Append("window.__RS = [");
            for (var i = 0; i < resultSets.Count; i++)
            {
                if (i > 0) sb.Append(',');
                var json = JsonSerializer.Serialize(resultSets[i]);
                // </script> break-out (hem `</script>` hem `</SCRIPT>` vb. case-insensitive) ve HTML comment ornuntuleri kacirilir
                json = System.Text.RegularExpressions.Regex.Replace(json, "</(script)", "<\\/$1", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                json = json.Replace("<!--", "<\\!--");
                sb.Append(json);
            }
            sb.AppendLine("];");
            sb.AppendLine("</script>");

            // Tabs header
            if (config.Tabs.Count > 1)
            {
                sb.AppendLine("<div class='flex gap-1 mb-5 border-b border-gray-200'>");
                for (var t = 0; t < config.Tabs.Count; t++)
                {
                    var active = t == 0 ? " active" : "";
                    sb.AppendLine($"<div class='tab px-4 py-2.5 text-sm font-semibold text-gray-500 border-b-2 border-transparent{active}' data-tab='{t}' onclick='switchTab({t})'>{Esc(config.Tabs[t].Title)}</div>");
                }
                sb.AppendLine("</div>");
            }

            // Tab contents
            var gridCols = config.Layout == "compact" ? "grid-cols-2" : config.Layout == "wide" ? "grid-cols-1" : "grid-cols-4";

            for (var t = 0; t < config.Tabs.Count; t++)
            {
                var display = t == 0 ? "" : " style='display:none'";
                sb.AppendLine($"<div class='tab-content' id='tab-{t}'{display}>");
                sb.AppendLine($"<div class='grid {gridCols} gap-4'>");

                foreach (var comp in config.Tabs[t].Components)
                {
                    var spanCls = comp.Span > 1 ? $" col-span-{comp.Span}" : "";
                    switch (comp.Type)
                    {
                        case "kpi":
                            RenderKpi(sb, comp, spanCls);
                            break;
                        case "chart":
                            RenderChart(sb, comp, spanCls);
                            break;
                        case "table":
                            RenderTable(sb, comp, spanCls);
                            break;
                    }
                }

                sb.AppendLine("</div>");
                sb.AppendLine("</div>");
            }

            // Modal
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

            // Scripts
            sb.AppendLine("<script>");

            // Tab switching
            sb.AppendLine(@"
function switchTab(idx) {
  document.querySelectorAll('.tab').forEach(t => t.classList.remove('active'));
  document.querySelectorAll('.tab-content').forEach(c => c.style.display = 'none');
  document.querySelectorAll('.tab')[idx].classList.add('active');
  document.getElementById('tab-' + idx).style.display = 'block';
}");

            // Modal - DOM API ile insa edildi (XSS guvenli: key ve value textContent ile yaziliyor)
            sb.AppendLine(@"
function showDetail(row) {
  document.getElementById('modalTitle').textContent = 'Satır Detayı';
  var body = document.getElementById('modalBody');
  body.textContent = '';
  var table = document.createElement('table');
  table.className = 'w-full text-sm';
  for (var key in row) {
    if (!Object.prototype.hasOwnProperty.call(row, key)) continue;
    var tr = document.createElement('tr');
    tr.className = 'border-b border-gray-100';
    var tdKey = document.createElement('td');
    tdKey.className = 'py-2 pr-4 text-gray-500 font-medium';
    tdKey.textContent = key;
    var tdVal = document.createElement('td');
    tdVal.className = 'py-2 text-gray-900';
    var raw = row[key];
    tdVal.textContent = (raw == null || raw === '') ? '—' : fmtNum(raw);
    tr.appendChild(tdKey);
    tr.appendChild(tdVal);
    table.appendChild(tr);
  }
  body.appendChild(table);
  document.getElementById('detailModal').classList.add('open');
}
function closeModal() { document.getElementById('detailModal').classList.remove('open'); }
document.addEventListener('keydown', e => { if(e.key==='Escape') closeModal(); });");

            // Sayı formatlama (Türkçe: 1.234.567,89)
            sb.AppendLine(@"
function fmtNum(v) {
  if (v == null || v === '' || v === '—') return '—';
  var n = parseFloat(v);
  if (isNaN(n)) return v;
  if (Math.abs(n) >= 1000) return n.toLocaleString('tr-TR', {minimumFractionDigits: 0, maximumFractionDigits: 2});
  if (n % 1 !== 0) return n.toLocaleString('tr-TR', {minimumFractionDigits: 1, maximumFractionDigits: 2});
  return n.toString();
}");

            // KPI aggregation helper
            sb.AppendLine(@"
function aggVal(rs, agg, col, cond) {
  var data = (window.__RS && window.__RS[rs]) ? window.__RS[rs] : [];
  if (cond === 'notNull') data = data.filter(r => r[col] != null && r[col] !== '' && r[col] !== '—');
  if (cond === 'isNull') data = data.filter(r => r[col] == null || r[col] === '' || r[col] === '—');
  if (agg === 'count' || agg === 'countWhere') return data.length;
  if (!col) return data.length;
  var vals = data.map(r => parseFloat(r[col])).filter(v => !isNaN(v));
  if (agg === 'sum') return vals.reduce((a,b) => a+b, 0);
  if (agg === 'avg') return vals.length ? (vals.reduce((a,b) => a+b, 0) / vals.length).toFixed(1) : 0;
  if (agg === 'min') return vals.length ? Math.min(...vals) : 0;
  if (agg === 'max') return vals.length ? Math.max(...vals) : 0;
  if (agg === 'first') return data.length ? (data[0][col] ?? '—') : '—';
  return data.length;
}");

            // Initialize KPI values
            sb.AppendLine("document.querySelectorAll('[data-kpi]').forEach(el => {");
            sb.AppendLine("  var cfg = JSON.parse(el.dataset.kpi);");
            sb.AppendLine("  el.textContent = fmtNum(aggVal(cfg.rs, cfg.agg, cfg.col, cfg.cond));");
            sb.AppendLine("});");

            // Initialize charts
            sb.AppendLine("document.querySelectorAll('[data-chart]').forEach(el => {");
            sb.AppendLine("  var cfg = JSON.parse(el.dataset.chart);");
            sb.AppendLine("  var data = (window.__RS && window.__RS[cfg.rs]) ? window.__RS[cfg.rs] : [];");
            sb.AppendLine("  var labels = data.map(r => r[cfg.labelCol] ?? '');");
            sb.AppendLine("  var datasets = cfg.datasets.map(ds => ({");
            sb.AppendLine("    label: ds.label,");
            sb.AppendLine("    data: data.map(r => parseFloat(r[ds.col]) || 0),");
            sb.AppendLine("    borderColor: ds.hex,");
            sb.AppendLine("    backgroundColor: ds.hex + '22',");
            sb.AppendLine("    fill: cfg.type === 'line',");
            sb.AppendLine("    tension: 0.3,");
            sb.AppendLine("    pointRadius: 3");
            sb.AppendLine("  }));");
            sb.AppendLine("  new Chart(el, {");
            sb.AppendLine("    type: cfg.type === 'doughnut' ? 'doughnut' : cfg.type === 'pie' ? 'pie' : cfg.type,");
            sb.AppendLine("    data: { labels: labels, datasets: datasets },");
            sb.AppendLine("    options: { plugins: { legend: { labels: { color: '#6b7280' } } },");
            sb.AppendLine("      scales: cfg.type === 'doughnut' || cfg.type === 'pie' ? {} : { x: { ticks: { color: '#9ca3af' }, grid: { color: '#f3f4f6' } }, y: { ticks: { color: '#9ca3af' }, grid: { color: '#f3f4f6' } } } }");
            sb.AppendLine("  });");
            sb.AppendLine("});");

            // Initialize tables - DOM API ile guvenli insa (XSS korumasi: tum deger/etiketler textContent ile)
            sb.AppendLine(@"
document.querySelectorAll('[data-tbl]').forEach(function(el) {
  var cfg = JSON.parse(el.dataset.tbl);
  var data = (window.__RS && window.__RS[cfg.rs]) ? window.__RS[cfg.rs] : [];
  el.textContent = '';

  // Header
  var thead = document.createElement('thead');
  var headRow = document.createElement('tr');
  cfg.cols.forEach(function(c) {
    var th = document.createElement('th');
    th.className = 'px-4 py-2.5 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider' + (c.align === 'right' ? ' text-right' : '');
    th.textContent = c.label || '';
    headRow.appendChild(th);
  });
  thead.appendChild(headRow);
  el.appendChild(thead);

  // Body
  var tbody = document.createElement('tbody');
  data.forEach(function(row, i) {
    var tr = document.createElement('tr');
    tr.className = (i % 2 === 0 ? 'bg-white' : 'bg-gray-50/50') + (cfg.click ? ' clickable' : '');
    if (cfg.click) {
      // onclick inline JSON yerine closure - XSS-safe
      tr.addEventListener('click', function() { showDetail(row); });
    }
    cfg.cols.forEach(function(c) {
      var td = document.createElement('td');
      var colorCls = c.color ? ' text-' + c.color + '-600 font-semibold' : '';
      td.className = 'px-4 py-2.5 text-sm text-gray-700' + (c.align === 'right' ? ' text-right' : '') + colorCls;
      var rawVal = row[c.key];
      if (rawVal == null || rawVal === '') {
        td.textContent = '—';
      } else if (c.align === 'right') {
        td.textContent = fmtNum(rawVal);
      } else {
        td.textContent = String(rawVal);
      }
      tr.appendChild(td);
    });
    tbody.appendChild(tr);
  });
  el.appendChild(tbody);
});");

            sb.AppendLine("</script>");
            sb.AppendLine("</body></html>");

            return sb.ToString();
        }

        private static void RenderKpi(StringBuilder sb, DashboardComponent comp, string spanCls)
        {
            var c = GetColor(comp.Color);
            var kpiData = JsonSerializer.Serialize(new { rs = comp.ResultSet, agg = comp.Agg, col = comp.Column ?? "", cond = comp.Condition ?? "" });
            kpiData = kpiData.Replace("\"", "&quot;");

            sb.AppendLine($"<div class='bg-white rounded-xl border border-gray-200 shadow-sm p-5{spanCls}'>");
            sb.AppendLine($"  <div class='flex items-center justify-between mb-3'>");
            sb.AppendLine($"    <h3 class='text-xs font-semibold text-gray-500 uppercase tracking-wide'>{Esc(comp.Title)}</h3>");
            sb.AppendLine($"    <div class='w-9 h-9 {c.Bg} rounded-lg flex items-center justify-center'>");
            sb.AppendLine($"      <i class='{Esc(comp.Icon)} text-white text-sm'></i>");
            sb.AppendLine($"    </div>");
            sb.AppendLine($"  </div>");
            sb.AppendLine($"  <div class='text-3xl font-bold {c.Text}' data-kpi='{kpiData}'>—</div>");
            if (!string.IsNullOrWhiteSpace(comp.Subtitle))
                sb.AppendLine($"  <div class='text-xs text-gray-400 mt-1'>{Esc(comp.Subtitle)}</div>");
            sb.AppendLine("</div>");
        }

        private static void RenderChart(StringBuilder sb, DashboardComponent comp, string spanCls)
        {
            var chartId = "chart_" + Guid.NewGuid().ToString("N")[..8];
            var datasets = (comp.Datasets ?? new()).Select(ds => new
            {
                col = ds.Column,
                label = ds.Label,
                hex = ChartColorHex.GetValueOrDefault(ds.Color, "#3b82f6")
            });
            var chartData = JsonSerializer.Serialize(new
            {
                rs = comp.ResultSet,
                type = comp.ChartType,
                labelCol = comp.LabelColumn ?? "",
                datasets
            });
            chartData = chartData.Replace("\"", "&quot;");

            sb.AppendLine($"<div class='bg-white rounded-xl border border-gray-200 shadow-sm p-5{spanCls}'>");
            sb.AppendLine($"  <h3 class='text-xs font-semibold text-gray-500 uppercase tracking-wide mb-3'>{Esc(comp.Title)}</h3>");
            sb.AppendLine($"  <canvas id='{chartId}' data-chart='{chartData}'></canvas>");
            sb.AppendLine("</div>");
        }

        private static void RenderTable(StringBuilder sb, DashboardComponent comp, string spanCls)
        {
            var cols = (comp.Columns ?? new()).Select(c => new { key = c.Key, label = c.Label, align = c.Align, color = c.Color ?? "" });
            var tblData = JsonSerializer.Serialize(new
            {
                rs = comp.ResultSet,
                cols,
                click = comp.ClickDetail
            });
            tblData = tblData.Replace("\"", "&quot;");

            sb.AppendLine($"<div class='bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden{spanCls}'>");
            sb.AppendLine($"  <div class='px-5 py-3 border-b border-gray-100'>");
            sb.AppendLine($"    <h3 class='text-xs font-semibold text-gray-500 uppercase tracking-wide'>{Esc(comp.Title)}</h3>");
            sb.AppendLine($"  </div>");
            sb.AppendLine($"  <div class='overflow-x-auto'>");
            sb.AppendLine($"    <table class='w-full text-sm' data-tbl='{tblData}'></table>");
            sb.AppendLine($"  </div>");
            sb.AppendLine("</div>");
        }

        private static (string Bg, string Text, string Border, string Light) GetColor(string color)
        {
            return ColorMap.GetValueOrDefault(color, ColorMap["blue"]);
        }

        private static string Esc(string text) => System.Net.WebUtility.HtmlEncode(text ?? "");
    }
}
