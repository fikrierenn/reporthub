// builder-v2/builder-chart-render.js — F09 Faz 2: gerçek Chart.js render canvas içinde.
// Object.assign ile builder.js main IIFE'sinde compose edilir.
//
// Server `EmitChartInit` (DashboardClientScripts.Chart.cs) JS paritesi:
// 10 variant (line/area/bar/hbar/stacked/pie/doughnut/radar/polarArea/scatter)
// Chart.js 4 native, plugin yok.
//
// Lifecycle:
//   - widgetCharts: Map<widgetId, Chart instance>
//   - mountChartWidget(comp, container): canvas element bul, Chart.js init
//   - destroyChartInstance(id): instance.destroy() + Map'ten sil
//   - destroyAllCharts(): refreshAllWidgets öncesi tümünü temizle
//   - mountAllCharts(): widgetInnerHtml innerHTML basıldıktan sonra hepsini yeniden init

(function () {
    "use strict";
    window.__builderV2 = window.__builderV2 || {};

    var PALETTE = [
        '#2563eb', '#059669', '#dc2626', '#f59e0b', '#475569',
        '#7c3aed', '#e11d48', '#0891b2', '#ea580c', '#65a30d'
    ];

    function fmtChart(v, numFormat) {
        var n = typeof v === 'number' ? v : parseFloat(v);
        if (isNaN(n)) return v;
        if (numFormat === 'currency') return '₺ ' + n.toLocaleString('tr-TR', { maximumFractionDigits: 0 });
        if (numFormat === 'currency-short') {
            if (Math.abs(n) >= 1e6) return '₺ ' + (n / 1e6).toFixed(1).replace('.', ',') + 'M';
            if (Math.abs(n) >= 1e3) return '₺ ' + (n / 1e3).toFixed(1).replace('.', ',') + 'K';
            return '₺ ' + n;
        }
        if (numFormat === 'percent') return n.toFixed(1).replace('.', ',') + '%';
        if (numFormat === 'decimal2') return n.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
        return Math.abs(n) >= 1000 ? (n / 1000).toLocaleString('tr-TR', { maximumFractionDigits: 1 }) + 'k' : n;
    }

    function buildChartConfig(comp, rs, paletteHexResolver) {
        var rows = (rs && rs.rows) || [];
        var labelCol = comp.labelColumn || '';
        var labels = rows.map(function (r) { return r[labelCol] != null ? r[labelCol] : ''; });
        var variant = comp.variant || 'bar';
        var axis = comp.axisOptions || {};
        var numFormat = comp.numberFormat || 'auto';

        var chartType = 'bar', extra = {}, patch = {};
        switch (variant) {
            case 'line':      chartType = 'line';      patch = { fill: false, tension: axis.smooth === false ? 0 : 0.3, pointRadius: 3 }; break;
            case 'area':      chartType = 'line';      patch = { fill: true,  tension: axis.smooth === false ? 0 : 0.3, pointRadius: 2 }; break;
            case 'bar':       chartType = 'bar';       patch = { borderRadius: 3, borderWidth: 0 }; break;
            case 'hbar':      chartType = 'bar';       extra.indexAxis = 'y'; patch = { borderRadius: 3, borderWidth: 0 }; break;
            case 'stacked':   chartType = 'bar';       patch = { borderRadius: 2, borderWidth: 0 }; extra.stacked = true; break;
            case 'pie':       chartType = 'pie'; break;
            case 'doughnut':  chartType = 'doughnut';  extra.cutout = '62%'; break;
            case 'radar':     chartType = 'radar';     patch = { fill: true, tension: 0.2 }; break;
            case 'polarArea': chartType = 'polarArea'; break;
            case 'scatter':   chartType = 'scatter'; break;
            default:          chartType = 'bar';
        }

        // Datasets
        var datasets;
        var compDatasets = comp.datasets || [];
        if (variant === 'scatter') {
            datasets = compDatasets.map(function (ds) {
                var hex = paletteHexResolver(ds.color) || PALETTE[0];
                return {
                    label: ds.label || ds.column,
                    data: rows.map(function (r) { return { x: parseFloat(r[labelCol]) || 0, y: parseFloat(r[ds.column]) || 0 }; }),
                    backgroundColor: hex,
                    borderColor: hex,
                    pointRadius: 5
                };
            });
        } else if (variant === 'pie' || variant === 'doughnut' || variant === 'polarArea') {
            var ds0 = compDatasets[0] || { column: (rs && rs.columns && rs.columns[0]) || 'v', label: '' };
            datasets = [{
                label: ds0.label || '',
                data: rows.map(function (r) { return parseFloat(r[ds0.column]) || 0; }),
                backgroundColor: rows.map(function (_, i) { return PALETTE[i % PALETTE.length]; }),
                borderWidth: 0
            }];
        } else {
            datasets = compDatasets.map(function (ds) {
                var hex = paletteHexResolver(ds.color) || PALETTE[0];
                var out = {
                    label: ds.label || ds.column,
                    data: rows.map(function (r) { return parseFloat(r[ds.column]) || 0; }),
                    borderColor: hex,
                    backgroundColor: hex + (variant === 'area' || variant === 'radar' ? '33' : '22')
                };
                for (var k in patch) out[k] = patch[k];
                return out;
            });
        }

        // Scales (dairesel olmayanlar)
        var isCircular = variant === 'pie' || variant === 'doughnut' || variant === 'polarArea' || variant === 'radar';
        var scales = {};
        if (!isCircular) {
            var yAxis = {
                beginAtZero: axis.beginAtZero === true,
                ticks: { color: '#9ca3af', callback: function (v) { return fmtChart(v, numFormat); } },
                grid: { color: axis.showGrid === false ? 'transparent' : '#f3f4f6' }
            };
            var xAxis = { ticks: { color: '#9ca3af' }, grid: { display: false } };
            if (variant === 'hbar') { scales = { x: yAxis, y: xAxis }; }
            else { scales = { x: xAxis, y: yAxis }; }
            if (extra.stacked) { scales.x.stacked = true; scales.y.stacked = true; }
        }

        var options = {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: { display: axis.showLegend === false ? false : true, labels: { color: '#6b7280' } },
                tooltip: {
                    enabled: axis.tooltip === false ? false : true,
                    callbacks: {
                        label: function (ctx) {
                            var lbl = ctx.dataset.label ? ctx.dataset.label + ': ' : '';
                            var val = variant === 'scatter'
                                ? ('(' + fmtChart(ctx.parsed.x, numFormat) + ', ' + fmtChart(ctx.parsed.y, numFormat) + ')')
                                : fmtChart(ctx.parsed.y != null ? ctx.parsed.y : ctx.parsed, numFormat);
                            return lbl + val;
                        }
                    }
                }
            },
            scales: scales
        };
        if (extra.indexAxis) options.indexAxis = extra.indexAxis;
        if (extra.cutout) options.cutout = extra.cutout;

        return { type: chartType, data: { labels: labels, datasets: datasets }, options: options };
    }

    window.__builderV2.chartRenderMixin = function () {
        return {
            widgetCharts: {},  // id -> Chart instance

            destroyChartInstance(id) {
                var inst = this.widgetCharts[id];
                if (inst) {
                    try { inst.destroy(); } catch (e) { /* ignore */ }
                    delete this.widgetCharts[id];
                }
            },

            destroyAllCharts() {
                for (var id in this.widgetCharts) {
                    if (this.widgetCharts.hasOwnProperty(id)) {
                        try { this.widgetCharts[id].destroy(); } catch (e) { /* ignore */ }
                    }
                }
                this.widgetCharts = {};
            },

            mountChartWidget(comp) {
                if (typeof window.Chart !== 'function') return;
                if (this.mode !== 'preview') return; // edit mode'da Chart.js render etme
                var rs = this.findResultSetForComp(comp);
                if (!rs || !rs.rows || rs.rows.length === 0) return;
                var canvas = this.$el.querySelector('[data-widget-id="' + comp.id + '"] canvas[data-chart-canvas]');
                if (!canvas) return;

                var self = this;
                var cfg = buildChartConfig(comp, rs, function (color) { return self.chartColorHex(color); });
                this.destroyChartInstance(comp.id);
                this.widgetCharts[comp.id] = new window.Chart(canvas, cfg);
            },

            mountAllCharts() {
                var self = this;
                this.components.forEach(function (c) {
                    if (c.type === 'chart') self.mountChartWidget(c);
                });
            },

            // F09 Faz 2: chart widget render — preview mode'da canvas element + mount,
            // edit mode'da SVG placeholder (mevcut renderChartPreviewSvg).
            renderChartContainer(comp, rs, isPreview) {
                if (isPreview && rs && rs.rows && rs.rows.length > 0) {
                    return '<div class="bg-white rounded-xl border border-gray-200 shadow-sm p-4 h-full flex flex-col">' +
                        (comp.title ? '<h3 class="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-2">' + this.esc(comp.title) + '</h3>' : '') +
                        '<div style="flex:1; min-height:0; position:relative;">' +
                        '<canvas data-chart-canvas></canvas>' +
                        '</div></div>';
                }
                if (!isPreview) {
                    // Edit mode'da SVG placeholder (Gridstack drag/resize sırasında flicker olmasın)
                    return '<div class="w-body" style="padding:8px 12px;">' +
                        this.renderChartPreviewSvg(rs || { rows: [] }, comp) +
                        '</div>';
                }
                return '<div class="w-body" style="align-items:center; justify-content:center; color:var(--ink-4); font-size:11px;">veri yok</div>';
            }
        };
    };
})();
