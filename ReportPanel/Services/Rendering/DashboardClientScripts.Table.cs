using System.Text;

namespace ReportPanel.Services.Rendering
{
    // M-11 ADR-008 F-6: Tablo widget init (kolon format + conditional format + arama + sayfalama).
    // Plan 05.B: hesaplı kolon formula enrichment renderer-side, client sadece display.
    // 4 Mayıs 2026: CSV indir butonu (UTF-8 BOM + ; delimiter) + DashboardClientScripts.cs
    // hard-limit aşımı split.
    internal static partial class DashboardShellRenderer
    {
        private static void EmitTableInit(StringBuilder sb)
        {
            // Kolon format formatter — null ise align+val tip tabanli fallback.
            sb.AppendLine(@"
function fmtCell(rawVal, fmt) {
  if (rawVal == null || rawVal === '') return '—';
  var n = parseFloat(rawVal);
  if (fmt === 'currency')       return isNaN(n) ? String(rawVal) : '₺ ' + n.toLocaleString('tr-TR', { maximumFractionDigits: 2 });
  if (fmt === 'number')         return isNaN(n) ? String(rawVal) : n.toLocaleString('tr-TR', { maximumFractionDigits: 2 });
  if (fmt === 'percent')        return isNaN(n) ? String(rawVal) : n.toFixed(1).replace('.', ',') + '%';
  if (fmt === 'date') {
    try { var d = new Date(rawVal); return isNaN(d.getTime()) ? String(rawVal) : d.toLocaleDateString('tr-TR'); }
    catch (e) { return String(rawVal); }
  }
  if (fmt === 'text')           return String(rawVal);
  // auto: sayi ise fmtNum, degilse string
  return isNaN(n) ? String(rawVal) : fmtNum(n);
}

// Her data-tbl icin init
document.querySelectorAll('[data-tbl]').forEach(function(tableEl) {
  var cfg = JSON.parse(tableEl.dataset.tbl);
  var data = (window.__RS && window.__RS[cfg.rs]) ? window.__RS[cfg.rs] : [];
  var opts = cfg.opts || {};

  // cols bos kaldiysa SP ilk satir key'lerinden auto-detect (ADR-009)
  if ((!cfg.cols || cfg.cols.length === 0) && data.length > 0) {
    cfg.cols = Object.keys(data[0]).map(function(k) {
      return { key: k, label: k, align: (typeof data[0][k] === 'number' ? 'right' : 'left'), color: '', format: 'auto' };
    });
  }

  // Kolon bazli min/max (dataBar + colorScale icin normalize)
  var colStats = {};
  cfg.cols.forEach(function(c) {
    if (!c.condFormat || (c.condFormat.mode !== 'dataBar' && c.condFormat.mode !== 'colorScale')) return;
    var vals = data.map(function(r) { return parseFloat(r[c.key]); }).filter(function(v) { return !isNaN(v); });
    colStats[c.key] = vals.length ? { min: Math.min.apply(null, vals), max: Math.max.apply(null, vals) } : null;
  });

  var container = tableEl.parentElement; // <div class='overflow-x-auto...'>
  var widgetRoot = container.parentElement; // <div class='bg-white rounded-xl ...'>
  var searchInput = widgetRoot.querySelector('[data-tbl-search]');
  var pager = widgetRoot.querySelector('[data-tbl-pager]');
  var pageInfo = widgetRoot.querySelector('[data-tbl-page-info]');
  var btnPrev = widgetRoot.querySelector('[data-tbl-prev]');
  var btnNext = widgetRoot.querySelector('[data-tbl-next]');
  var btnExport = widgetRoot.querySelector('[data-tbl-export]');

  var state = { page: 0, filter: '' };
  var pageSize = opts.pageSize > 0 ? opts.pageSize : 0;

  function filteredRows() {
    if (!state.filter) return data;
    var f = state.filter.toLowerCase();
    return data.filter(function(r) {
      return cfg.cols.some(function(c) { var v = r[c.key]; return v != null && String(v).toLowerCase().indexOf(f) !== -1; });
    });
  }

  function renderTable() {
    tableEl.textContent = '';
    var rows = filteredRows();
    var totalRows = rows.length;
    var showRows = rows;
    if (pageSize > 0 && totalRows > pageSize) {
      var maxPage = Math.max(0, Math.ceil(totalRows / pageSize) - 1);
      if (state.page > maxPage) state.page = maxPage;
      var start = state.page * pageSize;
      showRows = rows.slice(start, start + pageSize);
      if (pageInfo) pageInfo.textContent = (start + 1) + '–' + (start + showRows.length) + ' / ' + totalRows;
    } else if (pageInfo) {
      pageInfo.textContent = totalRows + ' satır';
    }

    // Header
    var thead = document.createElement('thead');
    var headRow = document.createElement('tr');
    cfg.cols.forEach(function(c) {
      var th = document.createElement('th');
      var alignCls = c.align === 'right' ? ' text-right' : (c.align === 'center' ? ' text-center' : ' text-left');
      th.className = 'px-4 py-2.5 text-xs font-semibold text-gray-500 uppercase tracking-wider' + alignCls + (opts.stickyHeader ? ' sticky top-0 bg-gray-50' : '');
      th.textContent = c.label || '';
      headRow.appendChild(th);
    });
    thead.appendChild(headRow);
    tableEl.appendChild(thead);

    // Body
    var tbody = document.createElement('tbody');
    showRows.forEach(function(row, i) {
      var tr = document.createElement('tr');
      var stripeCls = opts.stripe !== false && i % 2 === 1 ? ' bg-gray-50/50' : '';
      tr.className = 'border-t border-gray-100' + stripeCls + (cfg.click ? ' clickable' : '');
      if (cfg.click) tr.addEventListener('click', function() { showDetail(row); });

      cfg.cols.forEach(function(c) {
        var td = document.createElement('td');
        var alignCls = c.align === 'right' ? ' text-right' : (c.align === 'center' ? ' text-center' : '');
        var colorCls = c.color ? ' text-' + c.color + '-600 font-semibold' : '';
        td.className = 'px-4 py-2.5 text-sm text-gray-700 relative' + alignCls + colorCls;

        var rawVal = row[c.key];
        var fmtted = fmtCell(rawVal, c.format || 'auto');
        var n = parseFloat(rawVal);

        // Kosullu format
        if (c.condFormat && !isNaN(n)) {
          applyConditionalFormat(td, c.condFormat, n, colStats[c.key], fmtted);
        } else {
          td.textContent = fmtted;
        }
        tr.appendChild(td);
      });
      tbody.appendChild(tr);
    });

    // Total row (opts.totalRow)
    if (opts.totalRow && rows.length > 0) {
      var totalTr = document.createElement('tr');
      totalTr.className = 'border-t-2 border-gray-300 bg-gray-50 font-semibold';
      cfg.cols.forEach(function(c, idx) {
        var td = document.createElement('td');
        var alignCls = c.align === 'right' ? ' text-right' : '';
        td.className = 'px-4 py-2 text-sm text-gray-900' + alignCls;
        if (idx === 0) {
          td.textContent = 'Toplam';
        } else {
          var vals = rows.map(function(r) { return parseFloat(r[c.key]); }).filter(function(v) { return !isNaN(v); });
          if (vals.length && c.align === 'right') {
            var sum = vals.reduce(function(a, b) { return a + b; }, 0);
            td.textContent = fmtCell(sum, c.format || 'auto');
          } else {
            td.textContent = '';
          }
        }
        totalTr.appendChild(td);
      });
      tbody.appendChild(totalTr);
    }

    tableEl.appendChild(tbody);
  }

  // Kosullu format uygulayicisi
  function applyConditionalFormat(td, condFormat, n, stats, fmtted) {
    var mode = condFormat.mode;

    if (mode === 'negativeRed') {
      td.textContent = fmtted;
      if (n < 0) td.className += ' text-red-600 font-semibold';
      return;
    }

    if (mode === 'iconUpDown') {
      td.textContent = '';
      var span = document.createElement('span');
      span.className = 'inline-flex items-center gap-1 ' + (n >= 0 ? 'text-emerald-600' : 'text-red-600');
      span.textContent = (n >= 0 ? '↑ ' : '↓ ') + fmtted;
      td.appendChild(span);
      return;
    }

    if (mode === 'colorScale' && stats) {
      td.textContent = fmtted;
      var range = stats.max - stats.min || 1;
      var pct = Math.max(0, Math.min(1, (n - stats.min) / range));
      // yesil (0) -> sari (0.5) -> kirmizi (1) — ters cevir: high=good yesil
      var hue = 120 - pct * 120; // 120 yesil, 0 kirmizi
      td.style.backgroundColor = 'hsl(' + hue + ', 70%, 90%)';
      return;
    }

    if (mode === 'dataBar' && stats) {
      td.textContent = '';
      var range = stats.max - stats.min || 1;
      var pct = Math.max(0, Math.min(100, ((n - stats.min) / range) * 100));
      var bar = document.createElement('div');
      bar.style.position = 'absolute';
      bar.style.inset = '4px auto 4px 4px';
      bar.style.width = 'calc(' + pct.toFixed(1) + '% - 8px)';
      bar.style.background = condFormat.color ? 'var(--brand-' + condFormat.color + '-200, rgba(59, 130, 246, 0.15))' : 'rgba(59, 130, 246, 0.15)';
      bar.style.borderRadius = '2px';
      bar.style.zIndex = '0';
      td.appendChild(bar);
      var textSpan = document.createElement('span');
      textSpan.style.position = 'relative';
      textSpan.style.zIndex = '1';
      textSpan.textContent = fmtted;
      td.appendChild(textSpan);
      return;
    }

    // none / unknown -> plain
    td.textContent = fmtted;
  }

  // Client-side arama
  if (searchInput) {
    searchInput.addEventListener('input', function(e) {
      state.filter = e.target.value;
      state.page = 0;
      renderTable();
    });
  }

  // Sayfalama
  if (btnPrev) btnPrev.addEventListener('click', function() { if (state.page > 0) { state.page--; renderTable(); } });
  if (btnNext) btnNext.addEventListener('click', function() {
    var rows = filteredRows();
    var maxPage = Math.max(0, Math.ceil(rows.length / pageSize) - 1);
    if (state.page < maxPage) { state.page++; renderTable(); }
  });

  // CSV indir — UTF-8 BOM + ; delimiter (Türk Excel uyumlu). Filtreli satırlar inilir.
  if (btnExport) btnExport.addEventListener('click', function() {
    var rows = filteredRows();
    function esc(v) {
      if (v == null) return '';
      var s = String(v);
      if (s.indexOf(';') !== -1 || s.indexOf('""') !== -1 || s.indexOf('\n') !== -1 || s.indexOf('\r') !== -1) {
        s = '""' + s.replace(/""/g, '""""') + '""';
      }
      return s;
    }
    var lines = [cfg.cols.map(function(c) { return esc(c.label || c.key); }).join(';')];
    rows.forEach(function(r) {
      lines.push(cfg.cols.map(function(c) { return esc(r[c.key]); }).join(';'));
    });
    var csv = '﻿' + lines.join('\r\n');
    var blob = new Blob([csv], { type: 'text/csv;charset=utf-8' });
    var url = URL.createObjectURL(blob);
    var d = new Date();
    var pad = function(n) { return n < 10 ? '0' + n : '' + n; };
    var stamp = d.getFullYear() + pad(d.getMonth()+1) + pad(d.getDate()) + '_' + pad(d.getHours()) + pad(d.getMinutes()) + pad(d.getSeconds());
    var safeTitle = (cfg.title || 'tablo').replace(/[^\wÀ-ſ]+/g, '_').replace(/^_+|_+$/g, '');
    var a = document.createElement('a');
    a.href = url; a.download = safeTitle + '_' + stamp + '.csv';
    document.body.appendChild(a); a.click(); document.body.removeChild(a);
    setTimeout(function() { URL.revokeObjectURL(url); }, 1000);
  });

  renderTable();
});");
        }
    }
}
