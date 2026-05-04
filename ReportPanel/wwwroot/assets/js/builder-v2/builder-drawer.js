// builder-v2/builder-drawer.js — drawer interactions + modal + drag-drop mixin.
// Object.assign ile builder.js main IIFE'sinde compose edilir.
//
// İçerik: 1-tıkla bağla, icon picker (80 FA ikon), Result Contract inline edit,
// drawer field bindings, palette → canvas drag-drop, veri preview modal.
//
// 4 Mayıs 2026: Tablo Setup tab + hesaplı kolon mixin builder-table.js'e
// taşındı (511 → ~250 sat, hard-limit 350 altı).

(function () {
    "use strict";
    window.__builderV2 = window.__builderV2 || {};

    var ICON_CHOICES = [
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
    ];

    window.__builderV2.drawerMixin = function () {
        return {
            iconChoices: ICON_CHOICES,

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

            // ---- Hızlı Bağla helper'ları (Plan 04: Setup tab'da basit cascading dropdown) ----
            // Seçili widget'ın bağlı RS index'i ('rsN' formatından sayıya).
            selectedRsIdx() {
                if (!this.selected || !this.selected.result) return -1;
                var m = String(this.selected.result).match(/^rs(\d+)$/);
                return m ? parseInt(m[1], 10) : -1;
            },

            selectedRs() {
                var i = this.selectedRsIdx();
                if (i < 0) return null;
                var sets = this.resultSets();
                return sets[i] || null;
            },

            selectedRsColumns() {
                var rs = this.selectedRs();
                return rs ? (rs.columns || []) : [];
            },

            columnKindFor(col) {
                var rs = this.selectedRs();
                return rs ? this.columnKind(rs, col) : '';
            },

            columnSampleFor(col) {
                var rs = this.selectedRs();
                return rs ? this.columnSample(rs, col) : '';
            },

            // RS değiştir — column ve agg sıfırla (yanlış kolon bağı kalmasın)
            setBoundResult(rsKey) {
                if (!this.selected) return;
                this.selected.result = rsKey || null;
                this.selected.column = null;
                this.selected.agg = null;
                this.refreshAllWidgets();
                this.syncConfig();
            },

            // Kolon değiştir — agg auto-detect, başlık default kolon adına çek
            setBoundColumn(col) {
                if (!this.selected) return;
                this.selected.column = col || null;
                if (col) {
                    var rs = this.selectedRs();
                    if (rs) this.selected.agg = this.autoAggregation(rs, col);
                    if (!this.selected.title || /^Yeni /.test(this.selected.title)) {
                        this.selected.title = col;
                    }
                }
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

            setField(field, val) {
                var c = this.selected; if (!c) return;
                if (field === 'span') val = parseInt(val, 10);
                // Icon: 'fa-users' → 'fas fa-users' (FontAwesome prefix gerekli, renderer plain class kullanır)
                if (field === 'icon' && val) {
                    if (!/^(fas|far|fab|fa-solid|fa-regular|fa-brands)\s/.test(val) && /^fa-/.test(val)) {
                        val = 'fas ' + val;
                    }
                }
                c[field] = val;
                // Alpine x-for içinden çağrılınca this.$el button'u gösterir;
                // root yerine document üzerinden ara (builder-v2 tek instance).
                var widgetEl = document.querySelector('.builder-v2 [data-widget-id="' + c.id + '"]');
                if (field === 'title' && widgetEl) {
                    var titleEl = widgetEl.querySelector('.w-head .title');
                    if (titleEl) titleEl.textContent = val;
                }
                if (field === 'span' && this.grid && widgetEl) {
                    this.grid.update(widgetEl, { w: val * 3 });
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

            // ---- Veri preview modal ----
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

            // Widget seçilince → drawer Veri tab'ında bağlı RS/kolona scroll + pulse highlight
            focusBoundDataInDrawer(comp) {
                if (!comp || !comp.column) return;
                var rsIdx = 0;
                if (comp.result && /^rs\d+$/.test(comp.result)) {
                    rsIdx = parseInt(comp.result.slice(2), 10) || 0;
                }
                this.drawerTab = 'veri';
                this.colSearchTerm = ''; // arama filtresini temizle, kart görünsün
                var self = this;
                this.$nextTick(function () {
                    var card = self.$el.querySelector(
                        '[data-rs-idx="' + rsIdx + '"] [data-col-name="' + (comp.column || '').replace(/"/g, '\\"') + '"]'
                    );
                    if (!card) return;
                    card.scrollIntoView({ behavior: 'smooth', block: 'center' });
                    card.classList.add('focus-pulse');
                    setTimeout(function () { card.classList.remove('focus-pulse'); }, 1500);
                });
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
            }
        };
    };
})();
