using System.Text;

namespace ReportPanel.Services.Rendering
{
    // M-11 ADR-008 F-5: KPI 4 variant init (basic/delta/sparkline/progress).
    // 4 Mayıs 2026: DashboardClientScripts.cs hard-limit aşımı split.
    internal static partial class DashboardShellRenderer
    {
        private static void EmitKpiInit(StringBuilder sb)
        {
            sb.AppendLine(@"
document.querySelectorAll('[data-kpi]').forEach(function(el) {
  var cfg = JSON.parse(el.dataset.kpi);
  var variant = cfg.variant || 'basic';
  var val = computeKpiBase(cfg);
  var fmt = cfg.numberFormat || 'auto';

  if (variant === 'basic') {
    el.textContent = fmtKpi(val, fmt);
    return;
  }

  // Diger variant'larda el container — icindeki data-kpi-* element'leri doldur.
  if (variant === 'delta') {
    var valEl = el.querySelector('[data-kpi-value]');
    var deltaEl = el.querySelector('[data-kpi-delta]');
    if (valEl) valEl.textContent = fmtKpi(val, fmt);
    if (deltaEl && cfg.compareColumn) {
      var cmp = aggVal(cfg.rs, cfg.agg, cfg.compareColumn, cfg.cond);
      var nVal = parseFloat(val), nCmp = parseFloat(cmp);
      if (!isNaN(nVal) && !isNaN(nCmp) && nCmp !== 0) {
        var pct = ((nVal - nCmp) / Math.abs(nCmp)) * 100;
        var up = pct >= 0;
        deltaEl.textContent = '';
        // SVG ok
        var svgNs = 'http://www.w3.org/2000/svg';
        var svg = document.createElementNS(svgNs, 'svg');
        svg.setAttribute('width', '10'); svg.setAttribute('height', '10'); svg.setAttribute('viewBox', '0 0 10 10');
        var path = document.createElementNS(svgNs, 'path');
        path.setAttribute('d', up ? 'M1 7l4-4 4 4' : 'M1 3l4 4 4-4');
        path.setAttribute('stroke', 'currentColor'); path.setAttribute('stroke-width', '1.6');
        path.setAttribute('fill', 'none'); path.setAttribute('stroke-linecap', 'round');
        svg.appendChild(path);
        deltaEl.appendChild(svg);
        var pctSpan = document.createElement('span');
        pctSpan.textContent = ' ' + Math.abs(pct).toFixed(1).replace('.', ',') + '%';
        deltaEl.appendChild(pctSpan);
        deltaEl.className = 'text-xs font-semibold inline-flex items-center gap-1 ' + (up ? 'text-emerald-600' : 'text-red-600');
      } else {
        deltaEl.textContent = '—';
      }
    }
    return;
  }

  if (variant === 'sparkline') {
    var valEl = el.querySelector('[data-kpi-value]');
    var sparkEl = el.querySelector('[data-kpi-spark]');
    if (valEl) valEl.textContent = fmtKpi(val, fmt);
    if (sparkEl && cfg.trendValueCol) {
      var data = (window.__RS && window.__RS[cfg.rs]) ? window.__RS[cfg.rs] : [];
      var points = data.map(function(r) { return parseFloat(r[cfg.trendValueCol]) || 0; });
      if (points.length >= 2) {
        var min = Math.min.apply(null, points);
        var max = Math.max.apply(null, points);
        var range = (max - min) || 1;
        var w = 80, h = 26;
        var step = w / (points.length - 1);
        var coords = points.map(function(v, i) { return [i * step, h - ((v - min) / range) * (h - 4) - 2]; });
        var pathD = 'M' + coords.map(function(c) { return c[0].toFixed(1) + ',' + c[1].toFixed(1); }).join(' L');
        var fillD = pathD + ' L' + w + ',' + h + ' L0,' + h + ' Z';
        var svgNs = 'http://www.w3.org/2000/svg';
        sparkEl.textContent = '';
        var fill = document.createElementNS(svgNs, 'path');
        fill.setAttribute('d', fillD); fill.setAttribute('fill', cfg.colorHex + '22'); fill.setAttribute('stroke', 'none');
        var line = document.createElementNS(svgNs, 'path');
        line.setAttribute('d', pathD); line.setAttribute('fill', 'none');
        line.setAttribute('stroke', cfg.colorHex || '#3b82f6'); line.setAttribute('stroke-width', '1.5');
        sparkEl.appendChild(fill);
        sparkEl.appendChild(line);
      }
    }
    return;
  }

  if (variant === 'progress') {
    var valEl = el.querySelector('[data-kpi-value]');
    var textEl = el.querySelector('[data-kpi-progress-text]');
    var barEl = el.querySelector('[data-kpi-progress-bar]');
    var target = cfg.targetValue != null ? parseFloat(cfg.targetValue)
               : (cfg.targetColumn ? parseFloat(aggVal(cfg.rs, 'first', cfg.targetColumn, null)) : NaN);
    var nVal = parseFloat(val);
    if (!isNaN(nVal) && !isNaN(target) && target !== 0) {
      var pct = Math.max(0, Math.min(100, (nVal / target) * 100));
      if (valEl) valEl.textContent = pct.toFixed(0) + '%';
      if (textEl) textEl.textContent = fmtKpi(nVal, fmt) + ' / ' + fmtKpi(target, fmt);
      if (barEl) barEl.style.width = pct.toFixed(1) + '%';
    } else {
      if (valEl) valEl.textContent = fmtKpi(val, fmt);
      if (textEl) textEl.textContent = 'hedef yok';
    }
    return;
  }
});");
        }
    }
}
