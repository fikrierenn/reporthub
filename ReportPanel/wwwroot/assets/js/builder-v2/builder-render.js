// builder-v2/builder-render.js — widget render + computation mixin.
// Object.assign ile builder.js main IIFE'sinde compose edilir.
//
// İçerik: widget HTML emit, KPI/chart/tablo render, kolon kind/sample/agg,
// result set başlık + preview helper'ları. Tüm method'lar `this` üzerinden
// state'e erişir (mixin pattern).

(function () {
    "use strict";
    window.__builderV2 = window.__builderV2 || {};

    window.__builderV2.renderMixin = function () {
        return {
            typeLabel(t) { return t === 'kpi' ? 'KPI' : t === 'chart' ? 'Grafik' : 'Tablo'; },

            formatNum(n) {
                if (typeof n !== 'number' || isNaN(n)) return String(n);
                if (Math.abs(n) >= 1000000) return (n / 1000000).toFixed(1) + 'M';
                if (Math.abs(n) >= 1000) return (n / 1000).toFixed(1) + 'K';
                if (Number.isInteger(n)) return n.toLocaleString('tr-TR');
                return n.toFixed(2).replace('.', ',');
            },

            findResultSetForComp(comp) {
                var sets = (this.spPreview && this.spPreview.resultSets) || [];
                if (!sets.length || !comp || !comp.result) return null;
                var r = String(comp.result);
                // 1. V2 builder default "rsN" pattern
                var m = r.match(/^rs(\d+)$/);
                if (m) {
                    var idx = parseInt(m[1], 10);
                    return sets[idx] || null;
                }
                // 2. Named binding (M-10 Faz 6 standart) — ResultContract'tan int index'e çöz
                var contract = (this.config && this.config.resultContract) ? this.config.resultContract[r] : null;
                if (contract && typeof contract.resultSet === 'number') return sets[contract.resultSet] || null;
                // 3. RS.name eşleşmesi (manuel set edilmiş ad)
                var byName = sets.find(function (rs, i) { return (rs.name || ('rs' + i)) === r; });
                return byName || null;
            },

            computeKpiValue(rs, comp) {
                var rows = rs.rows || [];
                if (rows.length === 0) return '—';

                // 2-kolon formül: comp.formulaA op comp.formulaB
                if (comp.formulaA && comp.formulaB && comp.formulaOp) {
                    var aVal = this.computeAggFromCol(rs, comp.formulaA, comp.aggA || 'first');
                    var bVal = this.computeAggFromCol(rs, comp.formulaB, comp.aggB || 'first');
                    if (typeof aVal !== 'number' || typeof bVal !== 'number') return '—';
                    var result;
                    switch (comp.formulaOp) {
                        case '+': result = aVal + bVal; break;
                        case '-': result = aVal - bVal; break;
                        case '*': result = aVal * bVal; break;
                        case '/': result = bVal === 0 ? 0 : aVal / bVal; break;
                        case '%': result = bVal === 0 ? 0 : (aVal / bVal) * 100; break; // yüzde
                        default: result = 0;
                    }
                    return this.formatNum(result);
                }

                var col = comp.column;
                var agg = comp.agg || 'count';
                if (agg === 'count') return this.formatNum(rows.length);
                if (!col) return this.formatNum(rows.length);
                var nums = rows.map(function (r) { var v = r[col]; return v == null ? null : Number(v); }).filter(function (v) { return v != null && !isNaN(v); });
                if (nums.length === 0) {
                    var first = rows[0][col];
                    return first == null ? '—' : String(first);
                }
                var v;
                switch (agg) {
                    case 'sum': v = nums.reduce(function (a, b) { return a + b; }, 0); break;
                    case 'avg': v = nums.reduce(function (a, b) { return a + b; }, 0) / nums.length; break;
                    case 'min': v = Math.min.apply(null, nums); break;
                    case 'max': v = Math.max.apply(null, nums); break;
                    case 'first': v = nums[0]; break;
                    default: v = nums.length;
                }
                return this.formatNum(v);
            },

            // Verilen RS + kolon + agg için sayısal değer hesaplar (formül için reusable)
            computeAggFromCol(rs, colName, agg) {
                var rows = rs.rows || [];
                if (rows.length === 0) return null;
                if (agg === 'count') return rows.length;
                if (!colName) return rows.length;
                var nums = rows.map(function (r) { var v = r[colName]; return v == null ? null : Number(v); }).filter(function (v) { return v != null && !isNaN(v); });
                if (nums.length === 0) return null;
                switch (agg) {
                    case 'sum': return nums.reduce(function (a, b) { return a + b; }, 0);
                    case 'avg': return nums.reduce(function (a, b) { return a + b; }, 0) / nums.length;
                    case 'min': return Math.min.apply(null, nums);
                    case 'max': return Math.max.apply(null, nums);
                    case 'first': return nums[0];
                    default: return nums.length;
                }
            },

            renderTablePreview(rs, comp) {
                var cols = (comp.columns && comp.columns.length > 0)
                    ? comp.columns
                    : (rs.columns || []).slice(0, 6).map(function (c) { return { key: c, label: c, align: 'left' }; });
                var rows = (rs.rows || []).slice(0, 12);
                var self = this;
                var html = '<div class="dt-wrap" style="margin:0;"><table class="dt"><thead><tr>';
                cols.forEach(function (c) {
                    html += '<th' + (c.align === 'right' ? ' class="num"' : '') + '>' + self.esc(c.label || c.key) + '</th>';
                });
                html += '</tr></thead><tbody>';
                rows.forEach(function (r) {
                    html += '<tr>';
                    cols.forEach(function (c) {
                        var v = r[c.key];
                        var disp = v == null ? '' : String(v);
                        html += '<td' + (c.align === 'right' ? ' class="num"' : '') + '>' + self.esc(disp) + '</td>';
                    });
                    html += '</tr>';
                });
                html += '</tbody></table></div>';
                return html;
            },

            renderChartPreviewSvg(rs, comp) {
                // Basit line/area SVG — gerçek Chart.js için F-9'da iframe preview yapılacak
                var rows = rs.rows || [];
                var dataCol = (comp.datasets && comp.datasets[0] && comp.datasets[0].column) || (rs.columns || []).find(function (c) {
                    return rows.some(function (r) { return typeof r[c] === 'number'; });
                });
                if (!dataCol) return '<div style="color:var(--ink-4); font-size:11px; text-align:center; padding-top:30px;">Sayı kolonu bulunamadı</div>';

                var values = rows.map(function (r) { var v = Number(r[dataCol]); return isNaN(v) ? 0 : v; });
                if (values.length < 2) return '<div style="color:var(--ink-4); font-size:11px; text-align:center; padding-top:30px;">Yeterli veri yok</div>';

                var max = Math.max.apply(null, values), min = Math.min.apply(null, values);
                var range = max - min || 1;
                var w = 600, h = 220;
                var pts = values.map(function (v, i) {
                    var x = (i / (values.length - 1)) * w;
                    var y = h - ((v - min) / range) * (h - 30) - 10;
                    return x.toFixed(1) + ',' + y.toFixed(1);
                });
                var areaPath = 'M' + pts[0] + ' L' + pts.slice(1).join(' L') + ' L' + w + ',' + h + ' L0,' + h + ' Z';
                var linePath = 'M' + pts[0] + ' L' + pts.slice(1).join(' L');
                var gradId = 'gd_' + comp.id;
                return '<svg width="100%" height="100%" viewBox="0 0 ' + w + ' ' + h + '" preserveAspectRatio="none">' +
                    '<defs><linearGradient id="' + gradId + '" x1="0" x2="0" y1="0" y2="1">' +
                    '<stop offset="0%" stop-color="#dc2626" stop-opacity=".24"/>' +
                    '<stop offset="100%" stop-color="#dc2626" stop-opacity="0"/></linearGradient></defs>' +
                    '<path d="' + areaPath + '" fill="url(#' + gradId + ')"/>' +
                    '<path d="' + linePath + '" fill="none" stroke="#dc2626" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"/>' +
                    '</svg>';
            },

            // F09.D: Designer ↔ Run görsel parite — V2 palet (server ColorMap genişlemesinden bağımsız)
            kpiColorClass(color) {
                var palette = {
                    red:    { bg: 'bg-red-600',     text: 'text-red-600' },
                    blue:   { bg: 'bg-blue-600',    text: 'text-blue-600' },
                    green:  { bg: 'bg-emerald-600', text: 'text-emerald-600' },
                    amber:  { bg: 'bg-amber-500',   text: 'text-amber-600' },
                    violet: { bg: 'bg-violet-600',  text: 'text-violet-600' },
                    rose:   { bg: 'bg-rose-500',    text: 'text-rose-600' },
                    slate:  { bg: 'bg-slate-600',   text: 'text-slate-600' }
                };
                return palette[color] || palette.blue;
            },

            // V2 paleti chart hex — Chart.js renderer için
            chartColorHex(color) {
                var palette = {
                    red: '#dc2626', blue: '#2563eb', green: '#059669',
                    amber: '#f59e0b', violet: '#7c3aed', rose: '#e11d48', slate: '#475569'
                };
                return palette[color] || palette.blue;
            },

            // F09.2: Edit-only overlay — preview mode'da CSS ile gizlenir (.builder-v2.preview-mode .w-edit-overlay)
            renderEditOverlay(comp) {
                var typeLabel = this.typeLabel(comp.type);
                var typeChipClass = 'type-' + comp.type;
                var resultPillContent = '';
                if (comp.result || comp.column) {
                    var rsLabel = comp.result || '';
                    if (rsLabel.match(/^rs\d+$/)) {
                        var rsIdx = parseInt(rsLabel.slice(2), 10);
                        var rsObj = this.resultSets()[rsIdx];
                        if (rsObj) rsLabel = this.resultSetTitle(rsObj, rsIdx);
                    }
                    var parts = [];
                    if (rsLabel) parts.push(this.esc(rsLabel));
                    if (comp.column) parts.push(this.esc(comp.column));
                    resultPillContent = parts.join(' · ');
                }
                var resultPill = resultPillContent
                    ? '<span class="result-pill" title="Bağlı veri kaynağı">' + resultPillContent + '</span>'
                    : '<span class="result-pill" style="background:var(--canvas); color:var(--ink-4); border-style:dashed;" title="Henüz bağlı değil">bağlanmadı</span>';

                return '<div class="w-edit-overlay"><div class="w-head">' +
                    '<span class="type-chip ' + typeChipClass + '">' + typeLabel + '</span>' +
                    '<span class="title">' + this.esc(comp.title || ('Yeni ' + typeLabel)) + '</span>' +
                    resultPill +
                    '<div class="w-actions">' +
                    '<button type="button" data-act="dup" title="Kopyala"><i class="fas fa-copy"></i></button>' +
                    '<button type="button" class="danger" data-act="del" title="Sil"><i class="fas fa-trash"></i></button>' +
                    '</div></div></div>';
            },

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
            },

            widgetInnerHtml(comp) {
                var rs = this.findResultSetForComp(comp);
                var isPreview = this.mode === 'preview';
                var html = this.renderEditOverlay(comp);
                var boundChip = comp.column
                    ? '<div class="bound-chip" title="Bağlı veri kaynağı"><i class="fas fa-link" style="font-size:9px;"></i> ' + this.esc(comp.column) + '</div>'
                    : (isPreview ? '' : '<div class="bound-chip" style="background:var(--canvas); color:var(--ink-4); border-color:var(--line); border-style:dashed;" title="Henüz bağlı değil"><i class="fas fa-link-slash" style="font-size:9px;"></i> bağlanmadı</div>');

                if (comp.type === 'kpi') {
                    html += '<div class="w-content">' + this.renderKpiCard(comp, rs, isPreview) + '</div>';
                } else if (comp.type === 'chart') {
                    // F09 Faz 2: chartRenderMixin.renderChartContainer (preview'da canvas + Chart.js mount, edit'te SVG)
                    html += '<div class="w-content">' + this.renderChartContainer(comp, rs, isPreview) + '</div>';
                } else if (comp.type === 'table') {
                    // F09 Faz 2: Tablo brand kart shell (server TableRenderer paritesi, conditional format Faz 4)
                    if (isPreview && rs && rs.rows && rs.rows.length > 0) {
                        html += '<div class="w-content">' +
                            '<div class="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden h-full flex flex-col">' +
                            (comp.title ? '<div class="px-5 py-3 border-b border-gray-100"><h3 class="text-xs font-semibold text-gray-500 uppercase tracking-wide">' + this.esc(comp.title) + '</h3></div>' : '') +
                            '<div class="overflow-auto" style="flex:1; min-height:0;">' +
                            this.renderTablePreview(rs, comp) +
                            '</div></div></div>';
                    } else {
                        html += '<div class="w-content">' +
                            '<div class="bg-white rounded-xl border border-gray-200 shadow-sm h-full flex items-center justify-center text-gray-400 text-xs">' +
                            (isPreview ? 'veri yok' : 'tablo önizleme — Önizle moduna geçin') +
                            '</div></div>';
                    }
                } else {
                    // Bilinmeyen tip — placeholder (RemovedWidget gibi)
                    html += '<div class="w-content"><div class="bg-yellow-50 border border-yellow-200 rounded-xl p-4 h-full flex items-center justify-center text-yellow-700 text-xs">Bilinmeyen tip: ' + this.esc(comp.type) + '</div></div>';
                }
                return html;
            },

            attachWidgetEvents(el) {
                var self = this;
                el.addEventListener('click', function (e) {
                    var actBtn = e.target.closest('[data-act]');
                    if (actBtn) {
                        e.stopPropagation();
                        var id = el.getAttribute('data-widget-id');
                        if (actBtn.dataset.act === 'del') self.removeWidget(id, el);
                        if (actBtn.dataset.act === 'dup') self.duplicateWidget(id);
                        return;
                    }
                    self.selectedId = el.getAttribute('data-widget-id');
                    self.refreshSelection();
                    // Widget seçilince drawer Veri tab'ında bağlı RS/kolona scroll + pulse
                    var comp = self.components.find(function (c) { return c.id === self.selectedId; });
                    if (comp && self.focusBoundDataInDrawer) self.focusBoundDataInDrawer(comp);
                });
                // Çift-tıkla → arkadaki ham veriyi modal'da göster
                el.addEventListener('dblclick', function (e) {
                    if (e.target.closest('[data-act]')) return;
                    var id = el.getAttribute('data-widget-id');
                    var comp = self.components.find(function (c) { return c.id === id; });
                    if (comp) self.openWidgetData(comp);
                });
            },

            refreshSelection() {
                var id = this.selectedId;
                this.$el.querySelectorAll('.grid-stack-item').forEach(function (n) {
                    n.classList.toggle('selected', n.getAttribute('data-widget-id') === id);
                });
            },

            refreshAllWidgets() {
                var self = this;
                // F09 Faz 2: Chart.js instance'ları yeniden render öncesi temizle (memory leak guard)
                if (this.destroyAllCharts) this.destroyAllCharts();
                this.components.forEach(function (c) {
                    var el = self.$el.querySelector('[data-widget-id="' + c.id + '"] .grid-stack-item-content');
                    if (el) el.innerHTML = self.widgetInnerHtml(c);
                });
                // innerHTML basıldıktan sonra Chart.js mount (canvas DOM'a girince)
                if (this.mountAllCharts) {
                    this.$nextTick(function () { self.mountAllCharts(); });
                }
            },

            // Result set başlığı. Sektör-özel tahmin yok — anlamlı isimlendirmeyi
            // admin Drawer Veri tab'ında yapıyor (config.resultContract → DB).
            resultSetTitle(rs, i) {
                var contract = this.config.resultContract || {};
                var rsKey = 'rs' + i;
                if (contract[rsKey]) return contract[rsKey]; // admin override (DB)
                if (rs.name && rs.name !== rsKey) return rs.name; // server fallback
                return 'Veri Seti ' + (i + 1);
            },

            resultSetPreviewCols(rs) {
                var cols = rs.columns || [];
                if (cols.length === 0) return '';
                return cols.slice(0, 3).join(' · ') + (cols.length > 3 ? ' · …' : '');
            },

            // Kolonun veri tipi rozeti — sayı/metin/tarih
            columnKind(rs, colName) {
                var rows = rs.rows || [];
                if (rows.length === 0) return 'metin';
                var v = rows[0][colName];
                if (typeof v === 'number') return 'sayı';
                if (typeof v === 'string' && /\d{4}-\d{2}-\d{2}/.test(v)) return 'tarih';
                return 'metin';
            },

            // Kolonun ilk satır değerinden örnek (kart üzerinde gösterim için)
            columnSample(rs, colName) {
                var rows = rs.rows || [];
                if (rows.length === 0) return '';
                var v = rows[0][colName];
                if (v == null) return '—';
                var s = String(v);
                if (s.length > 22) s = s.slice(0, 19) + '…';
                return s;
            },

            // Otomatik aggregation tespiti — kolon tipi + RS satır sayısına göre
            autoAggregation(rs, colName) {
                var kind = this.columnKind(rs, colName);
                if (kind === 'sayı') {
                    return (rs.rows || []).length === 1 ? 'first' : 'sum';
                }
                if (kind === 'tarih') return 'first';
                return 'count';
            },

            sampleRows() {
                var rs = this.resultSets()[0];
                if (!rs || !rs.rows) return [];
                return rs.rows.slice(0, 3);
            }
        };
    };
})();
