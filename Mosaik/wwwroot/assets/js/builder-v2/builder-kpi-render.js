// builder-v2/builder-kpi-render.js — KPI kart varyant render mixin'i.
// Object.assign ile builder.js main IIFE'sinde compose edilir.
//
// İçerik: renderKpiCard dispatch + 4 varyant (basic/delta/sparkline/progress).
// Server KpiRenderer paritesi (F09.1).

(function () {
    "use strict";
    window.__builderV2 = window.__builderV2 || {};

    window.__builderV2.kpiRenderMixin = function () {
        return {
            // F09.1: KPI brand kart — server KpiRenderer paritesi (basic/delta/sparkline/progress)
            renderKpiCard(comp, rs, isPreview) {
                var variant = comp.variant || 'basic';
                if (variant === 'delta') return this.renderKpiDelta(comp, rs, isPreview);
                if (variant === 'sparkline') return this.renderKpiSparkline(comp, rs, isPreview);
                if (variant === 'progress') return this.renderKpiProgress(comp, rs, isPreview);
                return this.renderKpiBasic(comp, rs, isPreview);
            },

            renderKpiBasic(comp, rs, isPreview) {
                var c = this.kpiColorClass(comp.color);
                var val = (isPreview && rs) ? this.computeKpiValue(rs, comp) : '—';
                var icon = comp.icon ? this.esc(comp.icon) : 'fas fa-chart-bar';
                return '<div class="bg-white rounded-xl border border-gray-200 shadow-sm p-5 h-full flex flex-col">' +
                    '<div class="flex items-center justify-between mb-3">' +
                    '<h3 class="text-xs font-semibold text-gray-500 uppercase tracking-wide">' + this.esc(comp.title || 'KPI') + '</h3>' +
                    '<div class="w-9 h-9 ' + c.bg + ' rounded-lg flex items-center justify-center">' +
                    '<i class="' + icon + ' text-white text-sm"></i>' +
                    '</div></div>' +
                    '<div class="text-3xl font-bold ' + c.text + '">' + this.esc(val) + '</div>' +
                    (comp.subtitle ? '<div class="text-xs text-gray-400 mt-1">' + this.esc(comp.subtitle) + '</div>' : '') +
                    '</div>';
            },

            renderKpiDelta(comp, rs, isPreview) {
                var c = this.kpiColorClass(comp.color);
                var val = (isPreview && rs) ? this.computeKpiValue(rs, comp) : '—';
                var icon = comp.icon ? this.esc(comp.icon) : 'fas fa-arrow-trend-up';
                var compareLabel = (comp.delta && comp.delta.compareLabel) || 'vs önceki';
                var deltaHtml = '<div class="text-xs font-semibold text-slate-400">—</div>';
                if (isPreview && rs && comp.delta && comp.delta.compareColumn) {
                    var aRaw = this.computeKpiValue(rs, comp);
                    var bRaw = this.computeAggFromCol(rs, comp.delta.compareColumn, comp.agg || 'first');
                    var a = parseFloat(aRaw), b = parseFloat(bRaw);
                    if (!isNaN(a) && !isNaN(b) && b !== 0) {
                        var pct = ((a - b) / Math.abs(b)) * 100;
                        var up = pct >= 0;
                        var arrow = up ? '↑' : '↓';
                        var clr = up ? 'text-emerald-600' : 'text-red-600';
                        deltaHtml = '<div class="text-xs font-semibold ' + clr + '">' + arrow + ' ' + Math.abs(pct).toFixed(1).replace('.', ',') + '%</div>';
                    }
                }
                return '<div class="bg-white rounded-xl border border-gray-200 shadow-sm p-5 h-full flex flex-col">' +
                    '<div class="flex items-center justify-between mb-3">' +
                    '<h3 class="text-xs font-semibold text-gray-500 uppercase tracking-wide">' + this.esc(comp.title || 'KPI') + '</h3>' +
                    '<div class="w-9 h-9 ' + c.bg + ' rounded-lg flex items-center justify-center">' +
                    '<i class="' + icon + ' text-white text-sm"></i>' +
                    '</div></div>' +
                    '<div class="flex items-baseline gap-2">' +
                    '<div class="text-3xl font-bold ' + c.text + '">' + this.esc(val) + '</div>' +
                    deltaHtml +
                    '</div>' +
                    '<div class="text-xs text-gray-400 mt-1">' + this.esc(compareLabel) + '</div>' +
                    '</div>';
            },

            renderKpiSparkline(comp, rs, isPreview) {
                var c = this.kpiColorClass(comp.color);
                var val = (isPreview && rs) ? this.computeKpiValue(rs, comp) : '—';
                var icon = comp.icon ? this.esc(comp.icon) : 'fas fa-chart-line';
                var hex = this.chartColorHex(comp.color);
                var sparkSvg = '<svg width="80" height="26" viewBox="0 0 80 26"></svg>';
                if (isPreview && rs && comp.trend && comp.trend.valueColumn) {
                    var pts = (rs.rows || []).map(function (r) { return parseFloat(r[comp.trend.valueColumn]) || 0; });
                    if (pts.length >= 2) {
                        var min = Math.min.apply(null, pts), max = Math.max.apply(null, pts);
                        var range = (max - min) || 1, w = 80, h = 26, step = w / (pts.length - 1);
                        var coords = pts.map(function (v, i) { return [(i * step).toFixed(1), (h - ((v - min) / range) * (h - 4) - 2).toFixed(1)]; });
                        var lineD = 'M' + coords.map(function (a) { return a[0] + ',' + a[1]; }).join(' L');
                        var fillD = lineD + ' L' + w + ',' + h + ' L0,' + h + ' Z';
                        sparkSvg = '<svg width="80" height="26" viewBox="0 0 80 26">' +
                            '<path d="' + fillD + '" fill="' + hex + '22" stroke="none"/>' +
                            '<path d="' + lineD + '" fill="none" stroke="' + hex + '" stroke-width="1.5"/></svg>';
                    }
                }
                return '<div class="bg-white rounded-xl border border-gray-200 shadow-sm p-5 h-full flex flex-col">' +
                    '<div class="flex items-center justify-between mb-3">' +
                    '<h3 class="text-xs font-semibold text-gray-500 uppercase tracking-wide">' + this.esc(comp.title || 'KPI') + '</h3>' +
                    '<div class="w-9 h-9 ' + c.bg + ' rounded-lg flex items-center justify-center">' +
                    '<i class="' + icon + ' text-white text-sm"></i>' +
                    '</div></div>' +
                    '<div class="flex items-end justify-between gap-3">' +
                    '<div class="text-3xl font-bold ' + c.text + '">' + this.esc(val) + '</div>' +
                    sparkSvg +
                    '</div>' +
                    (comp.subtitle ? '<div class="text-xs text-gray-400 mt-1">' + this.esc(comp.subtitle) + '</div>' : '') +
                    '</div>';
            },

            renderKpiProgress(comp, rs, isPreview) {
                var c = this.kpiColorClass(comp.color);
                var val = (isPreview && rs) ? this.computeKpiValue(rs, comp) : '—';
                var icon = comp.icon ? this.esc(comp.icon) : 'fas fa-battery-half';
                var hex = this.chartColorHex(comp.color);
                var pctText = '—', pctWidth = '0%', targetText = 'hedef yok';
                if (isPreview && rs && comp.progress) {
                    var nVal = parseFloat(val);
                    var target = comp.progress.targetValue != null
                        ? parseFloat(comp.progress.targetValue)
                        : (comp.progress.targetColumn ? parseFloat(this.computeAggFromCol(rs, comp.progress.targetColumn, 'first')) : NaN);
                    if (!isNaN(nVal) && !isNaN(target) && target !== 0) {
                        var pct = Math.max(0, Math.min(100, (nVal / target) * 100));
                        pctText = pct.toFixed(0) + '%';
                        pctWidth = pct.toFixed(1) + '%';
                        targetText = this.formatNum(nVal) + ' / ' + this.formatNum(target);
                    }
                }
                return '<div class="bg-white rounded-xl border border-gray-200 shadow-sm p-5 h-full flex flex-col">' +
                    '<div class="flex items-center justify-between mb-3">' +
                    '<h3 class="text-xs font-semibold text-gray-500 uppercase tracking-wide">' + this.esc(comp.title || 'KPI') + '</h3>' +
                    '<div class="w-9 h-9 ' + c.bg + ' rounded-lg flex items-center justify-center">' +
                    '<i class="' + icon + ' text-white text-sm"></i>' +
                    '</div></div>' +
                    '<div class="flex items-baseline justify-between">' +
                    '<div class="text-3xl font-bold ' + c.text + '">' + pctText + '</div>' +
                    '<div class="text-xs text-gray-500">' + targetText + '</div>' +
                    '</div>' +
                    '<div class="w-full h-1.5 bg-gray-200 rounded-full mt-2 overflow-hidden">' +
                    '<div class="h-full rounded-full" style="width:' + pctWidth + '; background:' + hex + ';"></div>' +
                    '</div>' +
                    (comp.subtitle ? '<div class="text-xs text-gray-400 mt-1">' + this.esc(comp.subtitle) + '</div>' : '') +
                    '</div>';
            }
        };
    };
})();
