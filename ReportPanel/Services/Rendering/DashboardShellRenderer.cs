using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ReportPanel.Models;

namespace ReportPanel.Services.Rendering
{
    // M-11 F-2: HTML iskelet + resultSets inject + tabs header + modal + client-side init scripts.
    // Widget-agnostik — sadece shell. Per-widget HTML per-widget renderer'larda.
    // JS: switchTab, showDetail/closeModal, fmtNum, aggVal (KPI), chart init, table init (+ auto-detect cols fallback).
    internal static class DashboardShellRenderer
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

        public static void RenderScripts(StringBuilder sb)
        {
            sb.AppendLine("<script>");

            // Tab switching
            sb.AppendLine(@"
function switchTab(idx) {
  document.querySelectorAll('.tab').forEach(t => t.classList.remove('active'));
  document.querySelectorAll('.tab-content').forEach(c => c.style.display = 'none');
  document.querySelectorAll('.tab')[idx].classList.add('active');
  document.getElementById('tab-' + idx).style.display = 'block';
}");

            // Modal (DOM API + textContent — XSS güvenli)
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

            // Sayı formatlama (tr-TR)
            sb.AppendLine(@"
function fmtNum(v) {
  if (v == null || v === '' || v === '—') return '—';
  var n = parseFloat(v);
  if (isNaN(n)) return v;
  if (Math.abs(n) >= 1000) return n.toLocaleString('tr-TR', {minimumFractionDigits: 0, maximumFractionDigits: 2});
  if (n % 1 !== 0) return n.toLocaleString('tr-TR', {minimumFractionDigits: 1, maximumFractionDigits: 2});
  return n.toString();
}");

            // KPI aggregation
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

            // KPI number formatter (ADR-008 F-4: numberFormat alan destegi)
            sb.AppendLine(@"
function fmtKpi(v, fmt) {
  if (v == null || v === '' || v === '—') return '—';
  var n = parseFloat(v);
  if (isNaN(n)) return v;
  if (fmt === 'currency') return '₺ ' + n.toLocaleString('tr-TR', { maximumFractionDigits: 0 });
  if (fmt === 'currency-short') {
    if (Math.abs(n) >= 1e6) return '₺ ' + (n/1e6).toFixed(1).replace('.', ',') + 'M';
    if (Math.abs(n) >= 1e3) return '₺ ' + (n/1e3).toFixed(1).replace('.', ',') + 'K';
    return '₺ ' + n;
  }
  if (fmt === 'percent')  return n.toFixed(1).replace('.', ',') + '%';
  if (fmt === 'decimal2') return n.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  return fmtNum(n);
}");

            // KPI init
            sb.AppendLine("document.querySelectorAll('[data-kpi]').forEach(el => {");
            sb.AppendLine("  var cfg = JSON.parse(el.dataset.kpi);");
            sb.AppendLine("  var val = aggVal(cfg.rs, cfg.agg, cfg.col, cfg.cond);");
            sb.AppendLine("  el.textContent = fmtKpi(val, cfg.numberFormat || 'auto');");
            sb.AppendLine("});");

            // Chart init — ADR-008 F-4: 10 variant destegi (line/area/bar/hbar/stacked/pie/doughnut/radar/polarArea/scatter)
            // Chart.js 4 native, plugin YOK.
            sb.AppendLine(@"
document.querySelectorAll('[data-chart]').forEach(function(el) {
  var cfg = JSON.parse(el.dataset.chart);
  var data = (window.__RS && window.__RS[cfg.rs]) ? window.__RS[cfg.rs] : [];
  var labels = data.map(function(r) { return r[cfg.labelCol] != null ? r[cfg.labelCol] : ''; });
  var variant = cfg.variant || 'bar';
  var axis = cfg.axis || {};
  var numFormat = cfg.numberFormat || 'auto';

  // Variant -> Chart.js type + extra options
  var chartType = 'bar', extra = {}, patch = {};
  switch (variant) {
    case 'line':      chartType = 'line';      patch = { fill: false, tension: axis.smooth === false ? 0 : 0.3, pointRadius: 3 }; break;
    case 'area':      chartType = 'line';      patch = { fill: true,  tension: axis.smooth === false ? 0 : 0.3, pointRadius: 2 }; break;
    case 'bar':       chartType = 'bar';       patch = { borderRadius: 3, borderWidth: 0 }; break;
    case 'hbar':      chartType = 'bar';       extra.indexAxis = 'y'; patch = { borderRadius: 3, borderWidth: 0 }; break;
    case 'stacked':   chartType = 'bar';       patch = { borderRadius: 2, borderWidth: 0 }; extra.stacked = true; break;
    case 'pie':       chartType = 'pie';       break;
    case 'doughnut':  chartType = 'doughnut';  extra.cutout = '62%'; break;
    case 'radar':     chartType = 'radar';     patch = { fill: true, tension: 0.2 }; break;
    case 'polarArea': chartType = 'polarArea'; break;
    case 'scatter':   chartType = 'scatter';   break;
    default:          chartType = 'bar';
  }

  // Dataset'leri variant'a göre inşa et
  var datasets;
  var palette = ['#3b82f6','#10b981','#ef4444','#f59e0b','#6b7280','#6366f1','#a855f7','#ec4899','#14b8a6','#f97316'];
  if (variant === 'scatter') {
    // scatter -> {x, y} formatı
    datasets = cfg.datasets.map(function(ds) {
      return {
        label: ds.label,
        data: data.map(function(r) { return { x: parseFloat(r[cfg.labelCol]) || 0, y: parseFloat(r[ds.col]) || 0 }; }),
        backgroundColor: ds.hex,
        borderColor: ds.hex,
        pointRadius: 5
      };
    });
  } else if (variant === 'pie' || variant === 'doughnut' || variant === 'polarArea') {
    // Tek dataset, her nokta farklı renk
    var ds0 = cfg.datasets[0] || { col: Object.keys(data[0] || {})[0] || 'v', label: '' };
    datasets = [{
      label: ds0.label || '',
      data: data.map(function(r) { return parseFloat(r[ds0.col]) || 0; }),
      backgroundColor: data.map(function(_, i) { return palette[i % palette.length]; }),
      borderWidth: 0
    }];
  } else {
    // Standart bar/line/area/hbar/stacked/radar
    datasets = cfg.datasets.map(function(ds) {
      var out = {
        label: ds.label,
        data: data.map(function(r) { return parseFloat(r[ds.col]) || 0; }),
        borderColor: ds.hex,
        backgroundColor: ds.hex + (variant === 'area' || variant === 'radar' ? '33' : '22')
      };
      for (var k in patch) out[k] = patch[k];
      return out;
    });
  }

  // Sayı formatı (Y ekseni tick + tooltip)
  function fmtChart(v) {
    var n = typeof v === 'number' ? v : parseFloat(v);
    if (isNaN(n)) return v;
    if (numFormat === 'currency') return '₺ ' + n.toLocaleString('tr-TR', { maximumFractionDigits: 0 });
    if (numFormat === 'currency-short') {
      if (Math.abs(n) >= 1e6) return '₺ ' + (n/1e6).toFixed(1).replace('.', ',') + 'M';
      if (Math.abs(n) >= 1e3) return '₺ ' + (n/1e3).toFixed(1).replace('.', ',') + 'K';
      return '₺ ' + n;
    }
    if (numFormat === 'percent')  return n.toFixed(1).replace('.', ',') + '%';
    if (numFormat === 'decimal2') return n.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    return Math.abs(n) >= 1000 ? (n/1000).toLocaleString('tr-TR', { maximumFractionDigits: 1 }) + 'k' : n;
  }

  // Scales (dairesel olmayan variant'lar için)
  var isCircular = variant === 'pie' || variant === 'doughnut' || variant === 'polarArea' || variant === 'radar';
  var scales = {};
  if (!isCircular) {
    var yAxis = { beginAtZero: axis.beginAtZero === true, ticks: { color: '#9ca3af', callback: fmtChart }, grid: { color: axis.showGrid === false ? 'transparent' : '#f3f4f6' } };
    var xAxis = { ticks: { color: '#9ca3af' }, grid: { display: false } };
    if (variant === 'hbar') { scales = { x: yAxis, y: xAxis }; }
    else { scales = { x: xAxis, y: yAxis }; }
    if (extra.stacked) { scales.x.stacked = true; scales.y.stacked = true; }
  }

  var options = {
    responsive: true,
    plugins: {
      legend: { display: axis.showLegend === false ? false : true, labels: { color: '#6b7280' } },
      tooltip: { enabled: axis.tooltip === false ? false : true,
        callbacks: { label: function(ctx) {
          var lbl = ctx.dataset.label ? ctx.dataset.label + ': ' : '';
          var val = variant === 'scatter' ? ('(' + fmtChart(ctx.parsed.x) + ', ' + fmtChart(ctx.parsed.y) + ')') : fmtChart(ctx.parsed.y != null ? ctx.parsed.y : ctx.parsed);
          return lbl + val;
        } } }
    },
    scales: scales
  };
  if (extra.indexAxis) options.indexAxis = extra.indexAxis;
  if (extra.cutout)    options.cutout    = extra.cutout;

  new Chart(el, { type: chartType, data: { labels: labels, datasets: datasets }, options: options });
});");

            // Table init (DOM API + textContent, auto-detect cols fallback)
            sb.AppendLine(@"
document.querySelectorAll('[data-tbl]').forEach(function(el) {
  var cfg = JSON.parse(el.dataset.tbl);
  var data = (window.__RS && window.__RS[cfg.rs]) ? window.__RS[cfg.rs] : [];
  el.textContent = '';

  // ADR-009 · Migration 18 Adim B: cols bos kaldiysa SP'nin ilk satir key'lerinden auto-detect.
  // Tablo raporlari default skeleton'da columns:[] ile gelir — admin builder'da duzenleyene kadar.
  if ((!cfg.cols || cfg.cols.length === 0) && data.length > 0) {
    cfg.cols = Object.keys(data[0]).map(function(k) {
      return { key: k, label: k, align: (typeof data[0][k] === 'number' ? 'right' : 'left'), color: '' };
    });
  }

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
        }

        public static void EndHtml(StringBuilder sb)
        {
            sb.AppendLine("</body></html>");
        }
    }
}
