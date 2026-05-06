// builder-v2/builder-table.js — tablo widget Setup tab + hesaplı kolon mixin.
// Object.assign ile builder.js main IIFE'sinde compose edilir.
//
// Plan 05.A: kolon checkbox + reorder.
// Plan 05.B: hesaplı kolon ekle/düzenle/sil + ValidateFormula RPC.
// 4 Mayıs 2026: builder-drawer.js'ten ayrıldı (511 sat → hard-limit asimi).

(function () {
    "use strict";
    window.__builderV2 = window.__builderV2 || {};

    window.__builderV2.tableMixin = function () {
        return {
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
                if (!this.selected.columns || this.selected.columns.length === 0) {
                    var rs = this.selectedRs ? this.selectedRs() : null;
                    var rsCols = rs ? (rs.columns || []) : [];
                    var firstRow = rs && rs.rows && rs.rows.length > 0 ? rs.rows[0] : {};
                    this.selected.columns = rsCols.map(function (c) {
                        return { key: c, label: c, align: typeof firstRow[c] === 'number' ? 'right' : 'left', format: 'auto' };
                    });
                }
                var isEdit = !!f.editingKey;
                if (!isEdit && this.selected.columns.some(function (c) { return c.key === alias; })) {
                    f.error = 'Bu kolon adı zaten kullanılıyor.'; return;
                }
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
                        if (isEdit) {
                            var existing = self.selected.columns.find(function (c) { return c.key === f.editingKey; });
                            if (existing) {
                                existing.formula = formula;
                                existing.format = f.format || 'auto';
                                if (alias !== f.editingKey) {
                                    existing.key = alias;
                                    existing.label = alias;
                                }
                            }
                        } else {
                            self.selected.columns.push({
                                key: alias,
                                label: alias,
                                align: 'left',
                                format: f.format || 'auto',
                                formula: formula
                            });
                        }
                        self.cancelCalcColForm();
                        self.refreshAllWidgets();
                        self.syncConfig();
                    })
                    .catch(function (err) {
                        f.busy = false;
                        f.error = 'Sunucu doğrulamasında hata: ' + err.message;
                    });
            },

            editCalcColumn(colName) {
                if (!this.selected || !this.selected.columns) return;
                var col = this.selected.columns.find(function (c) { return c.key === colName; });
                if (!col || !col.formula) return;
                this.calcColForm = {
                    open: true,
                    alias: col.key,
                    formula: col.formula,
                    format: col.format || 'auto',
                    error: null,
                    errorPos: null,
                    busy: false,
                    editingKey: col.key
                };
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
            }
        };
    };
})();
