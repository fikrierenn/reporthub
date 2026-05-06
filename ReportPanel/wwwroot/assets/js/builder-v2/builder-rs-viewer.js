// builder-v2/builder-rs-viewer.js — drawer Veri tab'ında inline RS preview paneli.
// Object.assign ile builder.js main IIFE'sinde compose edilir.
//
// Modal yerine drawer içinde collapsible panel: Veri Seti dropdown'ından sonra
// "Önizle / Gizle" toggle. Açıkken seçili widget'ın bağlı RS'inin ilk 10 satır
// × tüm kolonlarının kompakt tablosu. Workflow kırmaz, anlık (Alpine reactive,
// sadece 10 satır render — modal'ın 50×N hücre overhead'i yok).

(function () {
    "use strict";
    window.__builderV2 = window.__builderV2 || {};

    var ROW_LIMIT = 10;

    window.__builderV2.rsViewerMixin = function () {
        return {
            rsPanelOpen: false,

            toggleRsPanel() {
                this.rsPanelOpen = !this.rsPanelOpen;
            },

            // Seçili widget'ın bağlı RS — selectedRs() drawer-mixin'den geliyor.
            rsPanelRows() {
                var rs = this.selectedRs ? this.selectedRs() : null;
                if (!rs) return [];
                return (rs.rows || []).slice(0, ROW_LIMIT);
            },

            rsPanelColumns() {
                var rs = this.selectedRs ? this.selectedRs() : null;
                return rs ? (rs.columns || []) : [];
            },

            rsPanelTotalRows() {
                var rs = this.selectedRs ? this.selectedRs() : null;
                return rs ? (rs.rows || []).length : 0;
            },

            rsPanelCellValue(row, col) {
                if (!row || row[col] == null) return '—';
                var v = row[col];
                if (typeof v === 'string' && /^\d{4}-\d{2}-\d{2}T/.test(v)) return v.slice(0, 10);
                return v;
            },

            rsPanelCellAlign(row, col) {
                if (!row) return 'left';
                return typeof row[col] === 'number' ? 'right' : 'left';
            }
        };
    };
})();
