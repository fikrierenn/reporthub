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
                if (!sets.length) return null;
                if (comp.result) {
                    var byName = sets.find(function (rs, i) { return (rs.name || ('rs' + i)) === comp.result; });
                    if (byName) return byName;
                }
                if (typeof comp.resultSet === 'number') return sets[comp.resultSet] || sets[0];
                return sets[0];
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

            widgetInnerHtml(comp) {
                var typeLabel = this.typeLabel(comp.type);
                var typeChipClass = 'type-' + comp.type;
                // result-pill: hem RS adı hem bağlı kolon (örn: "Veri Seti 6 · Bolum")
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

                var html = '<div class="w-head">' +
                    '<span class="type-chip ' + typeChipClass + '">' + typeLabel + '</span>' +
                    '<span class="title">' + this.esc(comp.title || ('Yeni ' + typeLabel)) + '</span>' +
                    resultPill +
                    '<div class="w-actions">' +
                    '<button type="button" data-act="dup" title="Kopyala"><i class="fas fa-copy"></i></button>' +
                    '<button type="button" class="danger" data-act="del" title="Sil"><i class="fas fa-trash"></i></button>' +
                    '</div></div>';

                var rs = this.findResultSetForComp(comp);
                var isPreview = this.mode === 'preview';
                var boundChip = comp.column
                    ? '<div class="bound-chip" title="Bağlı veri kaynağı"><i class="fas fa-link" style="font-size:9px;"></i> ' + this.esc(comp.column) + '</div>'
                    : (isPreview ? '' : '<div class="bound-chip" style="background:var(--canvas); color:var(--ink-4); border-color:var(--line); border-style:dashed;" title="Henüz bağlı değil"><i class="fas fa-link-slash" style="font-size:9px;"></i> bağlanmadı</div>');

                if (comp.type === 'kpi') {
                    var val = isPreview && rs ? this.computeKpiValue(rs, comp) : '—';
                    var subtitle = comp.subtitle || (isPreview && rs ? '' : 'veri bekleniyor');
                    var iconHtml = comp.icon ? '<i class="fas ' + this.esc(comp.icon) + '" style="color:var(--accent); font-size:14px; margin-right:6px;"></i>' : '';
                    html += '<div class="w-body">' +
                        '<div class="kpi-label">' + iconHtml + this.esc(comp.title || 'KPI') + '</div>' +
                        '<div class="kpi-value">' + this.esc(val) + '</div>' +
                        '<div class="kpi-sub"><span class="kpi-desc">' + this.esc(subtitle) + '</span></div>' +
                        boundChip +
                        '</div>';
                } else if (comp.type === 'chart') {
                    if (isPreview && rs && rs.rows && rs.rows.length > 0) {
                        html += '<div class="w-body" style="padding:8px 12px;">' +
                            this.renderChartPreviewSvg(rs, comp) +
                            boundChip +
                            '</div>';
                    } else {
                        html += '<div class="w-body" style="align-items:center; justify-content:center; color:var(--ink-4); font-size:11px; gap:8px;">' +
                            '<span>' + (isPreview ? 'veri yok' : 'grafik önizleme — Önizle moduna geçin') + '</span>' +
                            boundChip +
                            '</div>';
                    }
                } else {
                    if (isPreview && rs && rs.rows && rs.rows.length > 0) {
                        html += '<div class="w-body" style="padding:0;">' +
                            this.renderTablePreview(rs, comp) +
                            '<div style="padding:6px 10px; background:#fafafa; border-top:1px solid var(--line-2);">' + boundChip + '</div>' +
                            '</div>';
                    } else {
                        html += '<div class="w-body" style="padding:0;"><div class="dt-wrap" style="margin:0; padding:8px 12px; color:var(--ink-4); font-size:11px;">' +
                            (isPreview ? 'veri yok' : 'tablo önizleme — Önizle moduna geçin') +
                            '<div style="margin-top:6px;">' + boundChip + '</div>' +
                            '</div></div>';
                    }
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
                this.components.forEach(function (c) {
                    var el = self.$el.querySelector('[data-widget-id="' + c.id + '"] .grid-stack-item-content');
                    if (el) el.innerHTML = self.widgetInnerHtml(c);
                });
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
