// builder-v2/builder-drawer.js — drawer interactions + modal + drag-drop mixin.
// Object.assign ile builder.js main IIFE'sinde compose edilir.
//
// İçerik: 1-tıkla bağla, icon picker (80 FA ikon), Result Contract inline edit,
// drawer field bindings, palette → canvas drag-drop, veri preview modal.

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

            // ---- Plan 05.A: Tablo widget Setup tab kolon yönetimi ----

            // Görüntülenecek kolon listesi: önce comp.Columns sırasındaki seçili kolonlar,
            // sonra RS'te olup henüz seçilmemiş kolonlar (sona).
            tableDisplayedColumns() {
                if (!this.selected || this.selected.type !== 'table') return [];
                var rs = this.selectedRs();
                if (!rs) return [];
                var rsCols = rs.columns || [];
                var selectedKeys = (this.selected.columns || []).map(function (c) { return c.key; });
                var unselected = rsCols.filter(function (c) { return selectedKeys.indexOf(c) < 0; });
                // Seçili kolon RS'te yoksa (eski config) yine listede tut, sonradan unbind opsiyonu görünsün.
                var orphans = selectedKeys.filter(function (k) { return rsCols.indexOf(k) < 0; });
                return selectedKeys.filter(function (k) { return rsCols.indexOf(k) >= 0; })
                    .concat(unselected)
                    .concat(orphans);
            },

            isTableColumnSelected(colName) {
                if (!this.selected || !this.selected.columns) return false;
                return this.selected.columns.some(function (c) { return c.key === colName; });
            },

            getTableColumnAlign(colName) {
                if (!this.selected || !this.selected.columns) return 'left';
                var col = this.selected.columns.find(function (c) { return c.key === colName; });
                return (col && col.align) || 'left';
            },

            toggleTableColumn(colName) {
                if (!this.selected || this.selected.type !== 'table') return;
                if (!this.selected.columns) this.selected.columns = [];
                var idx = this.selected.columns.findIndex(function (c) { return c.key === colName; });
                if (idx >= 0) {
                    this.selected.columns.splice(idx, 1);
                } else {
                    this.selected.columns.push({ key: colName, label: colName, align: 'left' });
                }
                this.refreshAllWidgets();
                this.syncConfig();
            },

            setTableColumnAlign(colName, align) {
                if (!this.selected || !this.selected.columns) return;
                var col = this.selected.columns.find(function (c) { return c.key === colName; });
                if (!col) return;
                col.align = align;
                this.refreshAllWidgets();
                this.syncConfig();
            },

            setTableColumnLabel(colName, label) {
                if (!this.selected || !this.selected.columns) return;
                var col = this.selected.columns.find(function (c) { return c.key === colName; });
                if (!col) return;
                col.label = (label && label.trim()) || colName;
                this.refreshAllWidgets();
                this.syncConfig();
            },

            // Drag-drop: sadece seçili kolonlar arasında reorder yapar.
            // colDragSrc state IIFE-level tanımlandı (builder.js).
            onColDragStart(colName, ev) {
                if (!this.isTableColumnSelected(colName)) {
                    ev.preventDefault();
                    return;
                }
                this.colDragSrc = colName;
                ev.dataTransfer.effectAllowed = 'move';
                ev.dataTransfer.setData('text/plain', colName);
            },

            onColDragOver(ev) {
                if (this.colDragSrc) ev.preventDefault();
            },

            onColDrop(targetColName, ev) {
                ev.preventDefault();
                var src = this.colDragSrc;
                this.colDragSrc = null;
                if (!src || src === targetColName) return;
                if (!this.selected || !this.selected.columns) return;
                if (!this.isTableColumnSelected(targetColName)) return; // sadece seçili → seçili
                var cols = this.selected.columns;
                var fromIdx = cols.findIndex(function (c) { return c.key === src; });
                var toIdx = cols.findIndex(function (c) { return c.key === targetColName; });
                if (fromIdx < 0 || toIdx < 0) return;
                var moved = cols.splice(fromIdx, 1)[0];
                cols.splice(toIdx, 0, moved);
                this.refreshAllWidgets();
                this.syncConfig();
            },

            // ---- Plan 05.B: Hesaplı kolon ekleme ----

            openCalcColForm() {
                this.calcColForm = { open: true, alias: '', formula: '', format: 'auto', error: null, errorPos: null, busy: false };
            },

            cancelCalcColForm() {
                this.calcColForm = { open: false, alias: '', formula: '', format: 'auto', error: null, errorPos: null, busy: false };
            },

            // Server-side validate (FormulaParser.TryParse) — textarea blur veya Kaydet öncesi.
            // Tek source-of-truth backend; client-side parser portu yok (build/test maliyeti).
            validateFormulaLive() {
                var f = this.calcColForm;
                if (!f.formula || !f.formula.trim()) { f.error = null; f.errorPos = null; return; }
                var token = document.querySelector('input[name="__RequestVerificationToken"]');
                if (!token) { f.error = 'AntiForgery token yok'; return; }
                f.busy = true;
                var self = this;
                var body = new URLSearchParams();
                body.append('formula', f.formula);
                fetch('/Admin/ValidateFormula', {
                    method: 'POST',
                    credentials: 'same-origin',
                    headers: {
                        'Content-Type': 'application/x-www-form-urlencoded',
                        'RequestVerificationToken': token.value
                    },
                    body: body.toString()
                })
                    .then(function (r) { return r.json().then(function (j) { return { ok: r.ok, body: j }; }); })
                    .then(function (res) {
                        f.busy = false;
                        if (res.body && res.body.valid) {
                            f.error = null; f.errorPos = null;
                        } else {
                            f.error = (res.body && res.body.error) || 'Doğrulama başarısız.';
                            f.errorPos = (res.body && res.body.position) || null;
                        }
                    })
                    .catch(function (err) {
                        f.busy = false;
                        f.error = 'Sunucu doğrulamasında hata: ' + err.message;
                    });
            },

            addCalcColumn() {
                var f = this.calcColForm;
                if (!this.selected || this.selected.type !== 'table') return;
                var alias = (f.alias || '').trim();
                var formula = (f.formula || '').trim();
                if (!alias) { f.error = 'Kolon adı gerekli.'; return; }
                if (!/^[a-zA-Z][a-zA-Z0-9_]*$/.test(alias)) {
                    f.error = 'Kolon adı harfle başlamalı, sadece harf/rakam/alt çizgi (örn: marj, kategoriEtiket).'; return;
                }
                if (!formula) { f.error = 'Formül gerekli.'; return; }
                if (!this.selected.columns) this.selected.columns = [];
                if (this.selected.columns.some(function (c) { return c.key === alias; })) {
                    f.error = 'Bu kolon adı zaten kullanılıyor.'; return;
                }
                // Server-side validate, sonra ekle
                var self = this;
                var token = document.querySelector('input[name="__RequestVerificationToken"]');
                if (!token) { f.error = 'AntiForgery token yok'; return; }
                f.busy = true;
                var body = new URLSearchParams();
                body.append('formula', formula);
                fetch('/Admin/ValidateFormula', {
                    method: 'POST',
                    credentials: 'same-origin',
                    headers: {
                        'Content-Type': 'application/x-www-form-urlencoded',
                        'RequestVerificationToken': token.value
                    },
                    body: body.toString()
                })
                    .then(function (r) { return r.json(); })
                    .then(function (res) {
                        f.busy = false;
                        if (!res.valid) {
                            f.error = res.error || 'Formül geçersiz.';
                            f.errorPos = res.position || null;
                            return;
                        }
                        self.selected.columns.push({
                            key: alias,
                            label: alias,
                            align: 'left',
                            format: f.format || 'auto',
                            formula: formula
                        });
                        self.cancelCalcColForm();
                        self.refreshAllWidgets();
                        self.syncConfig();
                    })
                    .catch(function (err) {
                        f.busy = false;
                        f.error = 'Sunucu doğrulamasında hata: ' + err.message;
                    });
            },

            removeColumn(colName) {
                if (!this.selected || !this.selected.columns) return;
                var idx = this.selected.columns.findIndex(function (c) { return c.key === colName; });
                if (idx < 0) return;
                this.selected.columns.splice(idx, 1);
                this.refreshAllWidgets();
                this.syncConfig();
            },

            // Bir kolon hesaplı mı (formula sahibi)?
            isComputedColumn(colName) {
                if (!this.selected || !this.selected.columns) return false;
                var col = this.selected.columns.find(function (c) { return c.key === colName; });
                return !!(col && col.formula);
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
