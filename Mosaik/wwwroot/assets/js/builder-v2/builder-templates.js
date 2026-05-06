// builder-v2/builder-templates.js — F-10 madde 56-57: 3 preset şablon + "Şablondan Seç" modal mixin.
// Object.assign ile builder.js main IIFE'sinde compose edilir.
//
// Preset apply davranışı:
//   - Mevcut canvas BOŞSA (components.length === 0) doğrudan yükle.
//   - Doluysa confirm() — kullanıcı evet derse override, hayır → no-op.
//   - syncConfig + dirty=true (Kaydet butonuyla DB'ye yazılır).
//
// "rs0/rs1" binding pattern V2 builder default; ResultContract'sız raporlarda da
// renderer "rsN" regex fallback ile çalışır (DashboardConfig.ResolveResultSet).

(function () {
    "use strict";
    window.__builderV2 = window.__builderV2 || {};

    var TEMPLATES = {
        kpiTrio: {
            label: 'KPI Üçlüsü',
            icon: 'fa-grip',
            description: 'Aynı veri setinden 3 KPI yan yana — özet metrikler.',
            config: {
                schemaVersion: 2,
                layout: 'standard',
                tabs: [{
                    title: 'Genel',
                    components: [
                        { type: 'kpi', variant: 'basic', title: 'Metrik 1',
                          result: 'rs0', span: 1, agg: 'first', icon: 'fa-hashtag', color: 'blue' },
                        { type: 'kpi', variant: 'basic', title: 'Metrik 2',
                          result: 'rs0', span: 1, agg: 'sum', icon: 'fa-chart-simple', color: 'green' },
                        { type: 'kpi', variant: 'basic', title: 'Metrik 3',
                          result: 'rs0', span: 1, agg: 'avg', icon: 'fa-percent', color: 'amber' }
                    ]
                }]
            }
        },

        trendChart: {
            label: 'Trend Grafik',
            icon: 'fa-chart-line',
            description: 'Çizgi grafik (4 sütun) + sağda delta KPI — zaman serisi takibi.',
            config: {
                schemaVersion: 2,
                layout: 'standard',
                tabs: [{
                    title: 'Trend',
                    components: [
                        { type: 'chart', variant: 'line', title: 'Trend Grafik',
                          result: 'rs0', span: 3, color: 'blue' },
                        { type: 'kpi', variant: 'delta', title: 'Değişim',
                          result: 'rs0', span: 1, agg: 'first', icon: 'fa-arrow-trend-up', color: 'green' }
                    ]
                }]
            }
        },

        detailTable: {
            label: 'Detay Tablosu',
            icon: 'fa-table',
            description: 'Tam genişlik tablo + üstte adet KPI — detay raporlar.',
            config: {
                schemaVersion: 2,
                layout: 'standard',
                tabs: [{
                    title: 'Detay',
                    components: [
                        { type: 'kpi', variant: 'basic', title: 'Toplam Kayıt',
                          result: 'rs0', span: 1, agg: 'count', icon: 'fa-list-ol', color: 'slate' },
                        { type: 'table', title: 'Detay',
                          result: 'rs0', span: 4 }
                    ]
                }]
            }
        }
    };

    window.__builderV2.templatesMixin = function () {
        return {
            templateModal: { open: false },
            templateOptions: TEMPLATES,

            openTemplateModal() {
                this.templateModal = { open: true };
            },

            closeTemplateModal() {
                this.templateModal = { open: false };
            },

            templateKeys() {
                return Object.keys(TEMPLATES);
            },

            applyTemplate(key) {
                var preset = TEMPLATES[key];
                if (!preset) {
                    this.pushToast && this.pushToast('Şablon bulunamadı: ' + key, 'error');
                    return;
                }

                var existingCount = (this.config && this.config.tabs && this.config.tabs[0]
                    && this.config.tabs[0].components ? this.config.tabs[0].components.length : 0);

                if (existingCount > 0) {
                    if (!confirm('Mevcut ' + existingCount + ' bileşen değiştirilecek. Devam edilsin mi?')) {
                        return;
                    }
                }

                // Deep clone (preset sabit kalsın, kullanıcı değişikliği refleksiyon yapmasın)
                var fresh = JSON.parse(JSON.stringify(preset.config));

                // Stabil id'ler ata (drag/sil işlemleri için zorunlu)
                var self = this;
                fresh.tabs.forEach(function (tab) {
                    (tab.components || []).forEach(function (c) {
                        if (!c.id) c.id = self.genId(c.type || 'x');
                    });
                });

                this.config = fresh;
                this.activeTab = 0;
                this.selectedId = null;

                if (this.grid) {
                    try { this.grid.removeAll(); } catch (e) { /* ignore */ }
                }
                if (this.mountExistingWidgets) this.mountExistingWidgets();
                if (this.refreshAllWidgets) this.refreshAllWidgets();
                this.syncConfig();

                this.closeTemplateModal();
                this.pushToast && this.pushToast('"' + preset.label + '" şablonu uygulandı.', 'success');
            }
        };
    };
})();
