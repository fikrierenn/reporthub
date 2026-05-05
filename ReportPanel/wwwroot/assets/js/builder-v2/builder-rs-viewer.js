// builder-v2/builder-rs-viewer.js — SP çıktısının ham önizleme modalı.
// Object.assign ile builder.js main IIFE'sinde compose edilir.
//
// Akıl: spPreview (init'te /Admin/SpPreview ile çekilen) RS'lerini tab tab göster.
// Her tab içinde tam kolon × ilk 50 satır tablo. resultContract isimleri varsa
// tab adı olarak kullan ("kpi · Veri Seti 3"), yoksa "Veri Seti N".

(function () {
    "use strict";
    window.__builderV2 = window.__builderV2 || {};

    var ROW_LIMIT = 50;

    window.__builderV2.rsViewerMixin = function () {
        return {
            rsModal: { open: false, activeIdx: 0 },

            openRsModal(initialIdx) {
                var idx = (typeof initialIdx === 'number' && initialIdx >= 0) ? initialIdx : 0;
                this.rsModal = { open: true, activeIdx: idx };
            },

            closeRsModal() {
                this.rsModal = { open: false, activeIdx: 0 };
            },

            selectRsTab(idx) {
                if (this.rsModal && this.rsModal.open) this.rsModal.activeIdx = idx;
            },

            // ResultContract'tan name veya RS.name veya "Veri Seti N"
            rsTabLabel(rs, idx) {
                var contract = (this.config && this.config.resultContract) || {};
                // Reverse lookup: contract'ta resultSet=idx olan key var mı
                for (var key in contract) {
                    if (contract.hasOwnProperty(key) && contract[key] && contract[key].resultSet === idx) {
                        return key;
                    }
                }
                if (rs && rs.name && rs.name !== ('rs' + idx)) return rs.name;
                return 'Veri Seti ' + (idx + 1);
            },

            // Aktif RS'in row preview (limit=50)
            rsActiveRows() {
                var sets = this.spPreview && this.spPreview.resultSets;
                if (!sets || !sets[this.rsModal.activeIdx]) return [];
                var rs = sets[this.rsModal.activeIdx];
                return (rs.rows || []).slice(0, ROW_LIMIT);
            },

            rsActiveColumns() {
                var sets = this.spPreview && this.spPreview.resultSets;
                if (!sets || !sets[this.rsModal.activeIdx]) return [];
                var rs = sets[this.rsModal.activeIdx];
                return rs.columns || [];
            },

            rsActiveTotalRows() {
                var sets = this.spPreview && this.spPreview.resultSets;
                if (!sets || !sets[this.rsModal.activeIdx]) return 0;
                var rs = sets[this.rsModal.activeIdx];
                return (rs.rows || []).length;
            },

            rsCellValue(row, col) {
                if (!row || row[col] == null) return '—';
                var v = row[col];
                // ISO datetime kısalt: "2026-05-05T00:00:00" → "2026-05-05"
                if (typeof v === 'string' && /^\d{4}-\d{2}-\d{2}T/.test(v)) {
                    return v.slice(0, 10);
                }
                return v;
            },

            rsCellAlign(row, col) {
                if (!row) return 'left';
                var v = row[col];
                if (typeof v === 'number') return 'right';
                return 'left';
            }
        };
    };
})();
