using System.Text;

namespace ReportPanel.Services.Rendering
{
    // M-11 ADR-008 F-4: 10 chart variant init (line/area/bar/hbar/stacked/pie/doughnut/radar/polarArea/scatter).
    // Chart.js 4 native, plugin yok.
    // 4 Mayıs 2026: DashboardClientScripts.cs hard-limit aşımı split.
    internal static partial class DashboardShellRenderer
    {
        private static void EmitChartInit(StringBuilder sb)
        {
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
        }
    }
}
