// builder-v2/builder-table-render.js — F09 Faz 4: Tablo brand kart + conditional format paritesi.
// Object.assign ile builder.js main IIFE'sinde compose edilir.
//
// Server `EmitTableInit` (DashboardClientScripts.Table.cs) JS pariteden alt-küme:
//   ✓ Brand kart shell (bg-white rounded-xl border + uppercase başlık)
//   ✓ Kolon format (auto/number/currency/percent/date/text)
//   ✓ Conditional format 4 mod (negativeRed/iconUpDown/colorScale/dataBar)
//   ✓ Total row (sayı kolonları için sum)
//   ✓ Stripe + sticky header
//   ✗ Search/Pager/CSV — Tam Önizle iframe (Run sayfası) zaten sağlıyor; builder
//     canvas WYSIWYG için interaktif feature'lar gereksiz scope (ADR-008).
//
// Render string-based (innerHTML) — conditional format için inline style/class kullanır,
// text content her zaman escape edilir (XSS-safe). Server-side DOM API yaklaşımı yerine
// string yaklaşımı, ek mount lifecycle ihtiyacı doğurmaz (Chart.js gibi instance yok).

(function () {
    "use strict";
    window.__builderV2 = window.__builderV2 || {};

    var ROW_LIMIT = 50;

    function fmtCell(rawVal, fmt) {
        if (rawVal == null || rawVal === '') return '—';
        var n = parseFloat(rawVal);
        if (fmt === 'currency') return isNaN(n) ? String(rawVal) : '₺ ' + n.toLocaleString('tr-TR', { maximumFractionDigits: 2 });
        if (fmt === 'number')   return isNaN(n) ? String(rawVal) : n.toLocaleString('tr-TR', { maximumFractionDigits: 2 });
        if (fmt === 'percent')  return isNaN(n) ? String(rawVal) : n.toFixed(1).replace('.', ',') + '%';
        if (fmt === 'date') {
            try { var d = new Date(rawVal); return isNaN(d.getTime()) ? String(rawVal) : d.toLocaleDateString('tr-TR'); }
            catch (e) { return String(rawVal); }
        }
        if (fmt === 'text') return String(rawVal);
        // auto
        if (isNaN(n)) return String(rawVal);
        return Math.abs(n) >= 1000 ? n.toLocaleString('tr-TR', { maximumFractionDigits: 1 }) : String(n);
    }

    function escHtml(s) {
        return String(s == null ? '' : s)
            .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
    }

    // Conditional format → td innerHTML (string, esc'li)
    function condFormatCellHtml(condFormat, n, stats, fmttedEsc, alignCls) {
        var mode = condFormat.mode;
        if (mode === 'negativeRed') {
            var cls = (n < 0) ? ' text-red-600 font-semibold' : '';
            return '<td class="px-3 py-2 text-sm text-gray-700' + alignCls + cls + '">' + fmttedEsc + '</td>';
        }
        if (mode === 'iconUpDown') {
            var clr = (n >= 0) ? 'text-emerald-600' : 'text-red-600';
            var arrow = (n >= 0) ? '↑ ' : '↓ ';
            return '<td class="px-3 py-2 text-sm' + alignCls + '"><span class="inline-flex items-center gap-1 ' + clr + '">' + arrow + fmttedEsc + '</span></td>';
        }
        if (mode === 'colorScale' && stats) {
            var range = (stats.max - stats.min) || 1;
            var pct = Math.max(0, Math.min(1, (n - stats.min) / range));
            var hue = 120 - pct * 120;  // 120=yeşil, 0=kırmızı
            return '<td class="px-3 py-2 text-sm text-gray-700' + alignCls + '" style="background-color:hsl(' + hue + ', 70%, 90%);">' + fmttedEsc + '</td>';
        }
        if (mode === 'dataBar' && stats) {
            var range2 = (stats.max - stats.min) || 1;
            var pct2 = Math.max(0, Math.min(100, ((n - stats.min) / range2) * 100));
            var barColor = condFormat.color === 'green' ? 'rgba(16,185,129,0.18)'
                : condFormat.color === 'red' ? 'rgba(239,68,68,0.18)'
                : condFormat.color === 'amber' ? 'rgba(245,158,11,0.22)'
                : 'rgba(59,130,246,0.18)';
            return '<td class="px-3 py-2 text-sm text-gray-700 relative' + alignCls + '">' +
                '<div style="position:absolute; inset:4px auto 4px 4px; width:calc(' + pct2.toFixed(1) + '% - 8px); background:' + barColor + '; border-radius:2px; z-index:0;"></div>' +
                '<span style="position:relative; z-index:1;">' + fmttedEsc + '</span></td>';
        }
        return '<td class="px-3 py-2 text-sm text-gray-700' + alignCls + '">' + fmttedEsc + '</td>';
    }

    function computeColStats(data, cols) {
        var stats = {};
        cols.forEach(function (c) {
            if (!c.conditionalFormat) return;
            var mode = c.conditionalFormat.mode;
            if (mode !== 'dataBar' && mode !== 'colorScale') return;
            var vals = data.map(function (r) { return parseFloat(r[c.key]); }).filter(function (v) { return !isNaN(v); });
            stats[c.key] = vals.length ? { min: Math.min.apply(null, vals), max: Math.max.apply(null, vals) } : null;
        });
        return stats;
    }

    window.__builderV2.tableRenderMixin = function () {
        return {
            // Server `TableRenderer.Render` paritesi — bg-white kart + uppercase başlık + tablo gövdesi
            renderTableCard(comp, rs, isPreview) {
                if (!isPreview) {
                    return '<div class="bg-white rounded-xl border border-gray-200 shadow-sm h-full flex items-center justify-center text-gray-400 text-xs">tablo önizleme — Önizle moduna geçin</div>';
                }
                if (!rs || !rs.rows || rs.rows.length === 0) {
                    return '<div class="bg-white rounded-xl border border-gray-200 shadow-sm h-full flex items-center justify-center text-gray-400 text-xs">veri yok</div>';
                }

                var data = rs.rows;
                var opts = comp.tableOptions || {};
                var cols = (comp.columns && comp.columns.length > 0)
                    ? comp.columns
                    : (rs.columns || []).map(function (key) {
                        var firstVal = data[0] ? data[0][key] : null;
                        return { key: key, label: key, align: typeof firstVal === 'number' ? 'right' : 'left', format: 'auto' };
                    });

                var stats = computeColStats(data, cols);
                var rowsToShow = data.slice(0, ROW_LIMIT);

                var html = '<div class="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden h-full flex flex-col">';
                if (comp.title) {
                    html += '<div class="px-5 py-3 border-b border-gray-100"><h3 class="text-xs font-semibold text-gray-500 uppercase tracking-wide">' + escHtml(comp.title) + '</h3></div>';
                }
                html += '<div class="overflow-auto" style="flex:1; min-height:0;"><table class="w-full text-sm">';

                // thead
                var thStickyCls = opts.stickyHeader !== false ? ' sticky top-0 bg-gray-50 z-10' : ' bg-gray-50';
                html += '<thead><tr>';
                cols.forEach(function (c) {
                    var alignCls = c.align === 'right' ? ' text-right' : (c.align === 'center' ? ' text-center' : ' text-left');
                    html += '<th class="px-3 py-2 text-xs font-semibold text-gray-500 uppercase tracking-wider' + alignCls + thStickyCls + '">' + escHtml(c.label || c.key) + '</th>';
                });
                html += '</tr></thead><tbody>';

                // body
                rowsToShow.forEach(function (row, i) {
                    var stripeCls = (opts.stripe !== false && i % 2 === 1) ? ' bg-gray-50/50' : '';
                    html += '<tr class="border-t border-gray-100' + stripeCls + '">';
                    cols.forEach(function (c) {
                        var alignCls = c.align === 'right' ? ' text-right' : (c.align === 'center' ? ' text-center' : '');
                        var rawVal = row[c.key];
                        var fmtted = fmtCell(rawVal, c.format || 'auto');
                        var fmttedEsc = escHtml(fmtted);
                        var n = parseFloat(rawVal);

                        if (c.conditionalFormat && c.conditionalFormat.mode && c.conditionalFormat.mode !== 'none' && !isNaN(n)) {
                            html += condFormatCellHtml(c.conditionalFormat, n, stats[c.key], fmttedEsc, alignCls);
                        } else {
                            var colorCls = c.color ? ' text-' + c.color + '-600 font-semibold' : '';
                            html += '<td class="px-3 py-2 text-sm text-gray-700' + alignCls + colorCls + '">' + fmttedEsc + '</td>';
                        }
                    });
                    html += '</tr>';
                });

                // Total row (sayı kolonları için sum)
                if (opts.totalRow && data.length > 0) {
                    html += '<tr class="border-t-2 border-gray-300 bg-gray-50 font-semibold">';
                    cols.forEach(function (c, idx) {
                        var alignCls = c.align === 'right' ? ' text-right' : '';
                        if (idx === 0) {
                            html += '<td class="px-3 py-2 text-sm text-gray-900' + alignCls + '">Toplam</td>';
                        } else if (c.align === 'right') {
                            var vals = data.map(function (r) { return parseFloat(r[c.key]); }).filter(function (v) { return !isNaN(v); });
                            var sum = vals.reduce(function (a, b) { return a + b; }, 0);
                            html += '<td class="px-3 py-2 text-sm text-gray-900' + alignCls + '">' + escHtml(fmtCell(sum, c.format || 'auto')) + '</td>';
                        } else {
                            html += '<td class="px-3 py-2 text-sm text-gray-900' + alignCls + '"></td>';
                        }
                    });
                    html += '</tr>';
                }

                html += '</tbody></table></div>';
                if (data.length > ROW_LIMIT) {
                    html += '<div class="px-3 py-1.5 text-[10px] text-gray-400 border-t border-gray-100 bg-gray-50">' + ROW_LIMIT + ' / ' + data.length + ' satır gösteriliyor (Tam Önizle\'de hepsi)</div>';
                }
                html += '</div>';
                return html;
            }
        };
    };
})();
