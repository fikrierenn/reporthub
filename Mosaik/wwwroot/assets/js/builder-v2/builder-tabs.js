// builder-v2/builder-tabs.js — Multi-tab dashboard sayfa yönetimi mixin.
// Object.assign ile builder.js main IIFE'sinde compose edilir.
//
// Schema (DashboardConfig.tabs) zaten array; render tarafı (DashboardRenderer.Render) tab loop yapıyor.
// Bu mixin V2 builder UI'da eksik olan tab strip + activeTab switch + addTab/removeTab/setTabTitle eklerken
// Gridstack'i her sekme değişiminde rebuild eder (tek grid instance, çoklu component listesi).

(function () {
    "use strict";
    window.__builderV2 = window.__builderV2 || {};

    window.__builderV2.tabsMixin = function () {
        return {
            // Yeni sekme ekle, ona geç
            addTab() {
                if (!this.config.tabs) this.config.tabs = [];
                var idx = this.config.tabs.length;
                this.config.tabs.push({
                    title: 'Sekme ' + (idx + 1),
                    components: []
                });
                this.switchTab(idx);
                this.syncConfig();
            },

            // Sekmeyi sil — en az 1 sekme kalır
            removeTab(idx) {
                if (!this.config.tabs || this.config.tabs.length <= 1) return;
                if (idx < 0 || idx >= this.config.tabs.length) return;
                if (!confirm('"' + (this.config.tabs[idx].title || 'sekme') + '" sekmesi ve içindeki bileşenler silinsin mi?')) return;

                this.config.tabs.splice(idx, 1);
                // activeTab'ı güvenli bir aralığa çek
                var newActive = this.activeTab;
                if (newActive >= this.config.tabs.length) newActive = this.config.tabs.length - 1;
                if (newActive < 0) newActive = 0;
                this.switchTab(newActive);
                this.syncConfig();
            },

            // activeTab değiştir — Gridstack'i temizle, yeni tab'ın component'lerini mount et
            switchTab(idx) {
                if (idx < 0 || !this.config.tabs || idx >= this.config.tabs.length) return;
                this.activeTab = idx;
                this.selectedId = null;
                var self = this;
                // Gridstack DOM rebuild — Alpine reactive `components` getter activeTab'a bağlı,
                // ama Gridstack imperative; her switch'te grid'i sıfırlayıp yeniden kuruyoruz.
                this.$nextTick(function () {
                    if (self.grid) {
                        // removeAll(true) = grid engine'den + DOM'dan tüm widget'ları temizle.
                        // false ile DOM kalırdı → tab switch'lerde widget'lar birikir.
                        try { self.grid.removeAll(true); } catch (e) { /* edge: instance kayıp */ }
                    }
                    self.mountExistingWidgets();
                    self.refreshAllWidgets();
                });
            },

            // Sekme adını güncelle (inline edit)
            setTabTitle(idx, title) {
                if (!this.config.tabs || idx < 0 || idx >= this.config.tabs.length) return;
                var trimmed = (title || '').trim();
                if (!trimmed) return;
                this.config.tabs[idx].title = trimmed;
                this.syncConfig();
            }
        };
    };
})();
