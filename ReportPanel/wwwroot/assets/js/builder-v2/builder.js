// builder-v2/builder.js — Alpine.js pattern + Gridstack imperative.
// Plan 04 (Alpine adoption) ile tutarlı: tüm state Alpine reactive store'da, DOM template binding view'da.
// Gridstack drag-resize için imperative API kullanılır (Alpine reactive ile sync edilir).
//
// Kullanım: View'da `<div class="builder-v2" x-data="builderV2()">` ile mount.

(function () {
    "use strict";

    function buildAlpineComponent() {
        return function builderV2() {
            return {
                // ---- Reactive state ----
                config: { schemaVersion: 2, tabs: [{ title: 'Genel', components: [] }] },
                activeTab: 0,
                selectedId: null,
                drawerTab: 'veri', // 'veri' | 'gorunum' | 'ayarlar'
                spPreview: null,
                grid: null,
                mode: 'edit', // 'edit' | 'preview' — topbar mode-seg ile $store.builderV2Mode senkron
                showAdvanced: false, // İleri ayar collapsible (Aggregation override)
                colSearchTerm: '', // Drawer Veri tab kolon arama
                iconSearchTerm: '', // Drawer Görünüm tab icon arama
                iconCustom: '', // Manuel girilen ikon (fa-XYZ doğrudan)
                paramOverrides: {}, // Param-bar kullanıcının değiştirdiği değerler ({ Tarih: "2026-04-28" })
                paramEditing: null, // Hangi chip inline-edit modunda (field.name)
                paramDirty: false, // Kullanıcı param değiştirdi ama Çalıştır'a basmadı
                dataModal: { open: false, rsIdx: null, comp: null }, // Çift-tıkla veri modal'ı
                iconChoices: [
                    // Veri / metrik
                    'fa-chart-column', 'fa-chart-line', 'fa-chart-pie', 'fa-chart-area',
                    'fa-chart-bar', 'fa-chart-simple', 'fa-percent', 'fa-arrow-trend-up',
                    'fa-arrow-trend-down', 'fa-arrow-up', 'fa-arrow-down', 'fa-equals',
                    // Kişi / kullanıcı
                    'fa-users', 'fa-user', 'fa-user-tie', 'fa-user-clock',
                    'fa-user-check', 'fa-user-plus', 'fa-user-minus', 'fa-user-shield',
                    'fa-id-card', 'fa-id-badge', 'fa-address-book', 'fa-people-group',
                    // Zaman
                    'fa-calendar', 'fa-calendar-day', 'fa-calendar-check', 'fa-calendar-week',
                    'fa-clock', 'fa-stopwatch', 'fa-hourglass', 'fa-business-time',
                    // Mekan / lokasyon
                    'fa-store', 'fa-shop', 'fa-building', 'fa-warehouse',
                    'fa-house', 'fa-map-pin', 'fa-location-dot', 'fa-globe',
                    // Para / finans
                    'fa-coins', 'fa-money-bill', 'fa-money-bill-trend-up', 'fa-cash-register',
                    'fa-receipt', 'fa-credit-card', 'fa-wallet', 'fa-piggy-bank',
                    // Durum / uyarı
                    'fa-circle-check', 'fa-circle-xmark', 'fa-triangle-exclamation', 'fa-circle-info',
                    'fa-bell', 'fa-flag', 'fa-star', 'fa-heart',
                    'fa-thumbs-up', 'fa-thumbs-down', 'fa-fire', 'fa-bolt',
                    // Operasyon / iş
                    'fa-box', 'fa-boxes-stacked', 'fa-truck', 'fa-pallet',
                    'fa-cart-shopping', 'fa-bag-shopping', 'fa-tag', 'fa-tags',
                    'fa-clipboard-list', 'fa-file-invoice', 'fa-file-lines', 'fa-folder',
                    // Genel
                    'fa-gear', 'fa-wrench', 'fa-magnifying-glass', 'fa-eye',
                    'fa-database', 'fa-server', 'fa-cloud', 'fa-link'
                ],

                filteredIcons() {
                    var q = (this.iconSearchTerm || '').trim().toLowerCase();
                    if (!q) return this.iconChoices;
                    return this.iconChoices.filter(function (ic) {
                        return ic.toLowerCase().indexOf(q) >= 0;
                    });
                },

                applyCustomIcon() {
                    var v = (this.iconCustom || '').trim();
                    if (!v) return;
                    // Kullanıcı "fa-xyz" veya "fas fa-xyz" yazabilir; normalize et
                    if (v.indexOf('fa-') !== 0) v = 'fa-' + v;
                    this.setField('icon', v);
                    this.iconCustom = '';
                },

                // ---- Init ----
                init() {
                    var self = this;
                    var configEl = document.getElementById('DashboardConfigJson');
                    if (configEl) {
                        try {
                            var raw = configEl.value.trim();
                            if (raw) {
                                var parsed = JSON.parse(raw);
                                if (parsed && parsed.tabs) self.config = parsed;
                            }
                        } catch (e) { /* invalid */ }
                    }
                    self.config.tabs[self.activeTab].components.forEach(function (c) {
                        if (!c.id) c.id = self.genId(c.type || 'x');
                    });

                    // Gridstack init — DOM hazır olduktan sonra
                    self.$nextTick(function () {
                        self.initGridstack();
                        self.mountExistingWidgets();
                        self.fetchSpPreview();

                        // Mode toggle — topbar mode-seg → $store.builderV2Mode → bu component
                        if (window.Alpine && Alpine.store && Alpine.store('builderV2Mode')) {
                            // İlk değeri oku
                            self.mode = Alpine.store('builderV2Mode').value;
                            self.applyModeChange();
                            // Watch — $store değiştiğinde refresh
                            self.$watch(function () { return Alpine.store('builderV2Mode').value; }, function (v) {
                                self.mode = v;
                                self.applyModeChange();
                            });
                        }
                    });

                    // SP preview event listener (sp-helper.js'ten gelirse)
                    document.addEventListener('spPreviewReady', function (ev) {
                        self.spPreview = ev.detail;
                        // Eğer önizleme modundaysak widget'ları yeniden render et
                        if (self.mode === 'preview') self.refreshAllWidgets();
                    });
                },

                applyModeChange() {
                    var rootEl = this.$el;
                    if (rootEl) rootEl.classList.toggle('preview-mode', this.mode === 'preview');
                    // Drag-resize preview'da kapalı, edit'te açık
                    if (this.grid) {
                        this.grid.enableMove(this.mode === 'edit');
                        this.grid.enableResize(this.mode === 'edit');
                    }
                    this.refreshAllWidgets();
                },

                refreshAllWidgets() {
                    var self = this;
                    this.components.forEach(function (c) {
                        var el = self.$el.querySelector('[data-widget-id="' + c.id + '"] .grid-stack-item-content');
                        if (el) el.innerHTML = self.widgetInnerHtml(c);
                    });
                },

                // ---- Helpers ----
                genId(type) { return 'w_' + type + '_' + Math.random().toString(16).slice(2, 10); },
                esc(s) {
                    if (s == null) return '';
                    return String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
                },

                // ---- Reactive getters ----
                get components() { return this.config.tabs[this.activeTab].components; },
                get selected() {
                    var id = this.selectedId;
                    return id ? this.components.find(function (c) { return c.id === id; }) : null;
                },
                get reportMeta() { return window.__reportMeta || {}; },

                typeLabel(t) { return t === 'kpi' ? 'KPI' : t === 'chart' ? 'Grafik' : 'Tablo'; },

                // ---- Sync: config → hidden textarea (form POST için) ----
                syncConfig() {
                    var el = document.getElementById('DashboardConfigJson');
                    if (el) el.value = JSON.stringify(this.config);
                    if (window.Alpine && Alpine.store && Alpine.store('builder')) {
                        Alpine.store('builder').dirty = true;
                    }
                },

                // ---- Gridstack ----
                initGridstack() {
                    if (typeof GridStack === 'undefined') {
                        console.warn('[builder-v2] Gridstack not loaded');
                        return;
                    }
                    var el = this.$el.querySelector('.grid-stack');
                    if (!el) return;
                    var self = this;
                    this.grid = GridStack.init({
                        column: 12,
                        cellHeight: 80,
                        margin: 4,
                        float: false,
                        animate: true,
                        handle: '.w-head',
                        resizable: { handles: 'e,se,s,sw,w' }
                    }, el);
                    this.grid.on('change', function () { self.saveLayoutFromGrid(); });
                },

                mountExistingWidgets() {
                    if (!this.grid) return;
                    var self = this;
                    this.components.forEach(function (c) {
                        var w = c.w || (c.type === 'kpi' ? 3 : c.type === 'chart' ? 8 : 4);
                        var h = c.h || (c.type === 'kpi' ? 2 : 4);
                        var hasPos = typeof c.x === 'number' && typeof c.y === 'number';
                        var opts = hasPos
                            ? { w: w, h: h, x: c.x, y: c.y, content: '' }
                            : { w: w, h: h, autoPosition: true, content: '' };
                        var el = self.grid.addWidget(opts);
                        el.setAttribute('data-widget-id', c.id);
                        el.classList.add('widget');
                        var cc = el.querySelector('.grid-stack-item-content') || el;
                        cc.innerHTML = self.widgetInnerHtml(c);
                        self.attachWidgetEvents(el);
                    });
                    this.saveLayoutFromGrid();
                },

                saveLayoutFromGrid() {
                    if (!this.grid) return;
                    var nodes = this.grid.engine.nodes;
                    var self = this;
                    nodes.forEach(function (n) {
                        var id = n.el && n.el.getAttribute('data-widget-id');
                        if (!id) return;
                        var comp = self.components.find(function (c) { return c.id === id; });
                        if (comp) { comp.x = n.x; comp.y = n.y; comp.w = n.w; comp.h = n.h; }
                    });
                    this.syncConfig();
                },

                widgetInnerHtml(comp) {
                    var typeLabel = this.typeLabel(comp.type);
                    var typeChipClass = 'type-' + comp.type;
                    // result-pill: hem RS adı hem bağlı kolon (örn: "Veri Seti 6 · Bolum")
                    var resultPillContent = '';
                    if (comp.result || comp.column) {
                        var rsLabel = comp.result || '';
                        // RS adını rsN'den okunur isime çevir (admin override veya server name)
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
                    // Bound chip'i hem edit hem preview'da göster — kullanıcı widget'a bakınca
                    // "Bu hangi kolona bağlı?" cevabını anında görmeli
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

                formatNum(n) {
                    if (typeof n !== 'number' || isNaN(n)) return String(n);
                    if (Math.abs(n) >= 1000000) return (n / 1000000).toFixed(1) + 'M';
                    if (Math.abs(n) >= 1000) return (n / 1000).toFixed(1) + 'K';
                    if (Number.isInteger(n)) return n.toLocaleString('tr-TR');
                    return n.toFixed(2).replace('.', ',');
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
                    var labelCol = comp.labelColumn || (rs.columns || [])[0];
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

                // ---- Widget add/remove/duplicate ----
                addWidget(type) {
                    if (!this.grid) return;
                    var w = type === 'kpi' ? 3 : type === 'chart' ? 8 : 4;
                    var h = type === 'kpi' ? 2 : 4;
                    var comp = {
                        id: this.genId(type),
                        type: type,
                        title: 'Yeni ' + this.typeLabel(type),
                        span: type === 'kpi' ? 1 : type === 'chart' ? 3 : 2
                    };
                    this.components.push(comp);
                    var el = this.grid.addWidget({ w: w, h: h, autoPosition: true, content: '' });
                    el.setAttribute('data-widget-id', comp.id);
                    el.classList.add('widget');
                    var cc = el.querySelector('.grid-stack-item-content') || el;
                    cc.innerHTML = this.widgetInnerHtml(comp);
                    this.attachWidgetEvents(el);
                    this.selectedId = comp.id;
                    this.refreshSelection();
                    this.syncConfig();
                },

                removeWidget(id, el) {
                    if (this.grid && el) this.grid.removeWidget(el);
                    var idx = this.components.findIndex(function (c) { return c.id === id; });
                    if (idx >= 0) this.components.splice(idx, 1);
                    if (this.selectedId === id) this.selectedId = null;
                    this.syncConfig();
                },

                duplicateWidget(id) {
                    var c = this.components.find(function (x) { return x.id === id; });
                    if (!c) return;
                    var copy = JSON.parse(JSON.stringify(c));
                    copy.id = this.genId(copy.type);
                    copy.title = c.title + ' (kopya)';
                    delete copy.x; delete copy.y;
                    this.components.push(copy);
                    if (this.grid) {
                        var el = this.grid.addWidget({ w: c.w || 3, h: c.h || 2, autoPosition: true, content: '' });
                        el.setAttribute('data-widget-id', copy.id);
                        el.classList.add('widget');
                        var cc = el.querySelector('.grid-stack-item-content') || el;
                        cc.innerHTML = this.widgetInnerHtml(copy);
                        this.attachWidgetEvents(el);
                    }
                    this.syncConfig();
                },

                // ---- Drag-drop palette → canvas ----
                onPaletteDragStart(ev, type) {
                    ev.dataTransfer.setData('text/plain', type);
                    ev.dataTransfer.effectAllowed = 'copy';
                },
                onCanvasDragOver(ev) {
                    if (ev.dataTransfer.types.indexOf('text/plain') >= 0) {
                        ev.preventDefault();
                        ev.currentTarget.classList.add('drag-over');
                    }
                },
                onCanvasDragLeave(ev) { ev.currentTarget.classList.remove('drag-over'); },
                onCanvasDrop(ev) {
                    ev.currentTarget.classList.remove('drag-over');
                    var type = ev.dataTransfer.getData('text/plain');
                    if (type === 'kpi' || type === 'chart' || type === 'table') {
                        ev.preventDefault();
                        this.addWidget(type);
                    }
                },

                // ---- SP preview ----
                // ParamSchema fields'ını parse et — UI render için (param-bar chip'leri)
                paramFields() {
                    var meta = this.reportMeta;
                    if (!meta || !meta.paramSchemaJson) return [];
                    try {
                        var schema = JSON.parse(meta.paramSchemaJson);
                        return (schema && schema.fields) || [];
                    } catch (e) { return []; }
                },

                // Bir field için default değer (tip-bazlı)
                paramDefaultValue(f) {
                    if (!f) return '';
                    var todayStr = new Date().toISOString().slice(0, 10);
                    var dv = f.defaultValue !== undefined ? f.defaultValue : f.default;
                    if (typeof dv === 'string' && dv.toLowerCase() === 'today') return todayStr;
                    if (dv != null && dv !== '') return String(dv);
                    if ((f.type || '').toLowerCase() === 'date') return todayStr;
                    return '';
                },

                // Aktif değer = paramOverrides[name] (varsa) || default
                paramValue(f) {
                    if (!f || !f.name) return '';
                    if (this.paramOverrides && this.paramOverrides[f.name] !== undefined) {
                        return this.paramOverrides[f.name];
                    }
                    return this.paramDefaultValue(f);
                },

                // SpPreview/RunJsonV2 için tüm aktif param'ların map'i
                buildParamDefaults() {
                    var defaults = {};
                    var self = this;
                    this.paramFields().forEach(function (f) {
                        if (!f || !f.name) return;
                        var v = self.paramValue(f);
                        if (v !== '' && v !== null && v !== undefined) defaults[f.name] = v;
                    });
                    return defaults;
                },

                // Chip'i inline-edit için aç/kapa
                toggleParamEdit(name) {
                    if (this.paramEditing === name) this.paramEditing = null;
                    else this.paramEditing = name;
                },

                // Chip değerini değiştir (input/select onChange)
                setParamOverride(name, value) {
                    if (!this.paramOverrides) this.paramOverrides = {};
                    this.paramOverrides[name] = value;
                    this.paramDirty = true;
                },

                // Tüm override'ları sıfırla → default'lara dön
                // Veri modal'ını aç — hem widget çift-tıkla hem de palette RS satır tıkla
                openDataModal(rsIdx, comp) {
                    this.dataModal = { open: true, rsIdx: rsIdx, comp: comp || null };
                },

                closeDataModal() {
                    this.dataModal = { open: false, rsIdx: null, comp: null };
                },

                // Modal için aktif RS — boundResultSet veya seçili rsIdx
                modalRs() {
                    if (this.dataModal.rsIdx == null) return null;
                    return this.resultSets()[this.dataModal.rsIdx] || null;
                },

                modalRsTitle() {
                    if (this.dataModal.rsIdx == null) return '';
                    var rs = this.modalRs();
                    return rs ? this.resultSetTitle(rs, this.dataModal.rsIdx) : '';
                },

                // Widget çift-tıkla → bağlı RS için modal aç
                openWidgetData(comp) {
                    if (!comp) return;
                    var sets = this.resultSets();
                    if (!sets.length) return;
                    var idx = 0;
                    if (comp.result) {
                        var found = sets.findIndex(function (rs, i) { return (rs.name || ('rs' + i)) === comp.result || ('rs' + i) === comp.result; });
                        if (found >= 0) idx = found;
                    } else if (typeof comp.resultSet === 'number') {
                        idx = comp.resultSet;
                    }
                    this.openDataModal(idx, comp);
                },

                resetParams() {
                    this.paramOverrides = {};
                    this.paramDirty = false;
                    this.fetchSpPreview();
                },

                // Çalıştır → mevcut override'larla SP'yi tekrar çalıştır
                runWithCurrentParams() {
                    this.paramDirty = false;
                    this.paramEditing = null;
                    this.fetchSpPreview();
                },

                // Görüntüleme: kısa value (Tarih için DD.MM.YYYY, diğeri olduğu gibi)
                paramDisplayValue(f) {
                    var v = this.paramValue(f);
                    if (!v) return '—';
                    if ((f.type || '').toLowerCase() === 'date') {
                        // ISO YYYY-MM-DD → DD.MM.YYYY
                        var m = String(v).match(/^(\d{4})-(\d{2})-(\d{2})/);
                        if (m) return m[3] + '.' + m[2] + '.' + m[1];
                    }
                    return String(v);
                },

                fetchSpPreview() {
                    var meta = this.reportMeta;
                    if (!meta) return;
                    var defaults = this.buildParamDefaults();
                    // EditReportV2: reportId varsa V1 Run path'iyle aynı SP çağrısı (RunJsonV2).
                    // CreateReportV2'de henüz reportId yok → SpPreview fallback (param-aware).
                    var url;
                    if (meta.reportId) {
                        url = '/Reports/RunJsonV2/' + meta.reportId;
                        if (Object.keys(defaults).length > 0) {
                            url += '?paramsJson=' + encodeURIComponent(JSON.stringify(defaults));
                        }
                    } else {
                        if (!meta.dataSourceKey || !meta.procName) return;
                        url = '/Admin/SpPreview?dataSourceKey=' + encodeURIComponent(meta.dataSourceKey)
                            + '&procName=' + encodeURIComponent(meta.procName)
                            + '&maxRows=50';
                        if (Object.keys(defaults).length > 0) {
                            url += '&paramsJson=' + encodeURIComponent(JSON.stringify(defaults));
                        }
                    }
                    var self = this;
                    fetch(url, { credentials: 'same-origin' })
                        .then(function (r) { return r.json(); })
                        .then(function (data) {
                            if (data && data.success) {
                                // Kolonlar object {name, type} formatından string array'e normalize et
                                (data.resultSets || []).forEach(function (rs) {
                                    rs.columns = (rs.columns || []).map(function (c) {
                                        if (typeof c === 'string') return c;
                                        if (c && typeof c === 'object') return c.name || c.Name || c.column || JSON.stringify(c);
                                        return String(c);
                                    });
                                });
                                self.spPreview = data;
                                // Mevcut widget'ları yeni veri ile yenile (preview mode'daysa değerler değişir)
                                self.refreshAllWidgets();
                                document.dispatchEvent(new CustomEvent('spPreviewReady', { detail: data }));
                            }
                        })
                        .catch(function () { /* sessizce */ });
                },

                resultSets() { return (this.spPreview && this.spPreview.resultSets) || []; },

                // Result set için kullanıcı dilinde başlık. Önce config.resultContract'a bakar,
                // yoksa heuristik (kolon adı + satır sayısı) ile tahmin eder.
                // Result set başlığı. Sektör-özel tahmin yok — anlamlı isimlendirmeyi
                // admin Drawer Veri tab'ında yapıyor (config.resultContract → DB).
                // Default: "Veri Seti N". Server `rs.name` doluysa onu da kabul eder.
                resultSetTitle(rs, i) {
                    var contract = this.config.resultContract || {};
                    var rsKey = 'rs' + i;
                    if (contract[rsKey]) return contract[rsKey]; // admin override (DB)
                    if (rs.name && rs.name !== rsKey) return rs.name; // server fallback
                    return 'Veri Seti ' + (i + 1);
                },

                // Kolon listesi preview (alt-yazı için, "Sube · Bolum · Personel" gibi)
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

                // 1-tıkla bağla — kolonu seçili widget'a değer kaynağı yap.
                // Widget seçili değilse: yeni KPI ekle ve ona bağla.
                bindColumn(rsIdx, colName) {
                    var rs = this.resultSets()[rsIdx];
                    if (!rs) return;
                    var c = this.selected;
                    if (!c) {
                        // Widget yok → yeni KPI ekle, bu kolona bağla
                        this.addWidget('kpi');
                        c = this.selected;
                        if (!c) return;
                    }
                    c.result = 'rs' + rsIdx;
                    c.column = colName;
                    c.agg = this.autoAggregation(rs, colName);
                    if (!c.title || c.title.indexOf('Yeni ') === 0) c.title = colName;
                    this.refreshAllWidgets();
                    this.syncConfig();
                },

                unbindColumn() {
                    var c = this.selected;
                    if (!c) return;
                    c.column = null;
                    c.result = null;
                    this.refreshAllWidgets();
                    this.syncConfig();
                },

                // Result Contract inline edit — admin başlığı override eder
                setContractName(rsIdx, name) {
                    if (!this.config.resultContract) this.config.resultContract = {};
                    var key = 'rs' + rsIdx;
                    if (name && name.trim()) this.config.resultContract[key] = name.trim();
                    else delete this.config.resultContract[key];
                    this.syncConfig();
                },

                // ---- Drawer field bindings ----
                setField(field, val) {
                    var c = this.selected; if (!c) return;
                    if (field === 'span') val = parseInt(val, 10);
                    c[field] = val;
                    if (field === 'title') {
                        var el = this.$el.querySelector('[data-widget-id="' + c.id + '"] .w-head .title');
                        if (el) el.textContent = val;
                    }
                    if (field === 'span' && this.grid) {
                        var w = val * 3;
                        var el = this.$el.querySelector('[data-widget-id="' + c.id + '"]');
                        if (el) this.grid.update(el, { w: w });
                    }
                    this.syncConfig();
                },

                applyColumnSuggest(rsIndex, col) {
                    var c = this.selected; if (!c) return;
                    c.column = col;
                    var rs = this.resultSets()[rsIndex];
                    if (rs) c.result = rs.name || ('rs' + rsIndex);
                    this.syncConfig();
                },

                // ---- Sample rows for drawer Veri tab ----
                sampleRows() {
                    var rs = this.resultSets()[0];
                    if (!rs || !rs.rows) return [];
                    return rs.rows.slice(0, 3);
                }
            };
        };
    }

    // Alpine register — hem Alpine yüklenmeden önce hem sonra çalışır
    if (window.Alpine) {
        window.Alpine.data('builderV2', buildAlpineComponent());
    } else {
        document.addEventListener('alpine:init', function () {
            window.Alpine.data('builderV2', buildAlpineComponent());
        });
    }
})();
