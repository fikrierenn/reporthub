// builder-v2/builder-chart.js — chart widget X ekseni (labelColumn) + Y serileri (datasets) editör.
// Object.assign ile builder.js main IIFE'sinde compose edilir.
//
// Akıl: chart variant'ı seçilince RS kolonlarının tiplerine (sayı/tarih/metin) göre
// labelColumn + datasets BOŞSA otomatik doldurulur. Manuel override saygıyla korunur.
// Yetersiz kolon durumunda capacity warning string'i drawer'da gösterilir.

(function () {
    "use strict";
    window.__builderV2 = window.__builderV2 || {};

    // Variant başına alan ihtiyacı tablosu.
    //   needsLabel: labelColumn (X) gerekli mi
    //   datasetMin: en az kaç dataset (Y) zorunlu (auto-fill bunu doldurmaya çalışır)
    //   datasetMax: kullanıcı ekleyebileceği en fazla dataset (null = sınırsız)
    //   label: capacity warning'de variant adını gösterir
    var REQUIREMENTS = {
        bar:       { needsLabel: true,  datasetMin: 1, datasetMax: null, label: 'Sütun' },
        hbar:      { needsLabel: true,  datasetMin: 1, datasetMax: null, label: 'Yatay Sütun' },
        line:      { needsLabel: true,  datasetMin: 1, datasetMax: null, label: 'Çizgi' },
        area:      { needsLabel: true,  datasetMin: 1, datasetMax: null, label: 'Alan' },
        stacked:   { needsLabel: true,  datasetMin: 2, datasetMax: null, label: 'Yığılmış Sütun' },
        pie:       { needsLabel: true,  datasetMin: 1, datasetMax: 1,    label: 'Pasta' },
        doughnut:  { needsLabel: true,  datasetMin: 1, datasetMax: 1,    label: 'Halka' },
        polarArea: { needsLabel: true,  datasetMin: 1, datasetMax: 1,    label: 'Kutup Alan' },
        radar:     { needsLabel: true,  datasetMin: 1, datasetMax: null, label: 'Radar' },
        scatter:   { needsLabel: false, datasetMin: 1, datasetMax: null, label: 'Dağılım' }
    };

    var DATASET_PALETTE = ['blue', 'green', 'amber', 'violet', 'rose', 'slate', 'red'];

    window.__builderV2.chartMixin = function () {
        return {
            // ---- Variant requirements lookup ----
            chartFieldRequirements(variant) {
                return REQUIREMENTS[variant] || REQUIREMENTS.bar;
            },

            // ---- RS kolonlarını tipe göre grupla (sayı / tarih / metin) ----
            chartColumnsByKind() {
                var rs = this.selectedRs ? this.selectedRs() : null;
                if (!rs) return { 'sayı': [], 'tarih': [], 'metin': [] };
                var groups = { 'sayı': [], 'tarih': [], 'metin': [] };
                var self = this;
                (rs.columns || []).forEach(function (c) {
                    var kind = self.columnKind(rs, c);
                    if (groups[kind]) groups[kind].push(c);
                });
                return groups;
            },

            // X ekseni için aday kolonlar (önce tarih, sonra metin)
            chartLabelCandidates() {
                var g = this.chartColumnsByKind();
                return g['tarih'].concat(g['metin']);
            },

            // Y için aday sayı kolonlar (labelColumn dışında)
            chartDataCandidates() {
                var g = this.chartColumnsByKind();
                var c = this.selected;
                var label = c ? c.labelColumn : null;
                return g['sayı'].filter(function (n) { return n !== label; });
            },

            // Variant değişince auto-fill: labelColumn + datasets BOŞSA doldur, manuel seçimi koru.
            applyChartFieldDefaults() {
                var c = this.selected;
                if (!c || c.type !== 'chart') return;
                var variant = c.variant || 'bar';
                var req = this.chartFieldRequirements(variant);
                var groups = this.chartColumnsByKind();

                // X ekseni
                if (req.needsLabel && !c.labelColumn) {
                    var labels = groups['tarih'].concat(groups['metin']);
                    if (labels.length > 0) c.labelColumn = labels[0];
                } else if (!req.needsLabel) {
                    // scatter — labelColumn anlamsız, temizle
                    c.labelColumn = null;
                }

                // Y serileri — "logical empty" kontrolü: boş array veya hiç column'u dolu olmayan slot'lar
                var hasUsableDatasets = c.datasets && c.datasets.length > 0
                    && c.datasets.some(function (d) { return d && d.column; });
                if (!hasUsableDatasets) {
                    var nums = groups['sayı'].filter(function (n) { return n !== c.labelColumn; });
                    var count = Math.min(req.datasetMin, nums.length);
                    var datasets = [];
                    for (var i = 0; i < count; i++) {
                        datasets.push({
                            column: nums[i],
                            label: nums[i],
                            color: DATASET_PALETTE[i % DATASET_PALETTE.length]
                        });
                    }
                    c.datasets = datasets;
                }

                if (this.refreshAllWidgets) this.refreshAllWidgets();
                if (this.syncConfig) this.syncConfig();
            },

            // Yetersiz kolon uyarısı (drawer'da banner için)
            chartCapacityWarning() {
                var c = this.selected;
                if (!c || c.type !== 'chart') return null;
                var req = this.chartFieldRequirements(c.variant || 'bar');
                var groups = this.chartColumnsByKind();
                var numCount = groups['sayı'].length;
                var labelCount = groups['tarih'].length + groups['metin'].length;
                if (req.needsLabel && labelCount === 0)
                    return 'Bu veri setinde tarih/metin kolonu yok — X ekseni atanamaz.';
                if (numCount < req.datasetMin)
                    return req.label + ' için en az ' + req.datasetMin
                        + ' sayı kolonu gerekli, bu veri setinde ' + numCount + ' var.';
                return null;
            },

            // Variant set + auto-fill — drawer variant button'ı bunu çağırır.
            // setField('variant', v) ile aynı işi yapar ama sonrasında applyChartFieldDefaults() tetikler.
            setChartVariant(v) {
                if (this.setField) this.setField('variant', v);
                this.applyChartFieldDefaults();
            },

            // ---- labelColumn (X ekseni) ----
            setChartLabelColumn(col) {
                if (!this.selected) return;
                this.selected.labelColumn = col || null;
                if (this.refreshAllWidgets) this.refreshAllWidgets();
                if (this.syncConfig) this.syncConfig();
            },

            // ---- Dataset (Y serisi) CRUD ----
            setChartDatasetField(idx, field, val) {
                var c = this.selected;
                if (!c || !c.datasets || idx < 0 || idx >= c.datasets.length) return;
                c.datasets[idx][field] = val;
                if (this.refreshAllWidgets) this.refreshAllWidgets();
                if (this.syncConfig) this.syncConfig();
            },

            addChartDataset() {
                var c = this.selected;
                if (!c) return;
                if (!c.datasets) c.datasets = [];
                var req = this.chartFieldRequirements(c.variant || 'bar');
                if (req.datasetMax != null && c.datasets.length >= req.datasetMax) return;
                var available = this.chartDataCandidates().filter(function (n) {
                    return !c.datasets.some(function (d) { return d.column === n; });
                });
                var nextCol = available.length > 0 ? available[0] : '';
                c.datasets.push({
                    column: nextCol,
                    label: nextCol || 'Seri ' + (c.datasets.length + 1),
                    color: DATASET_PALETTE[c.datasets.length % DATASET_PALETTE.length]
                });
                if (this.refreshAllWidgets) this.refreshAllWidgets();
                if (this.syncConfig) this.syncConfig();
            },

            removeChartDataset(idx) {
                var c = this.selected;
                if (!c || !c.datasets || idx < 0 || idx >= c.datasets.length) return;
                c.datasets.splice(idx, 1);
                if (this.refreshAllWidgets) this.refreshAllWidgets();
                if (this.syncConfig) this.syncConfig();
            },

            canAddChartDataset() {
                var c = this.selected;
                if (!c || c.type !== 'chart') return false;
                var req = this.chartFieldRequirements(c.variant || 'bar');
                var current = c.datasets ? c.datasets.length : 0;
                return req.datasetMax == null || current < req.datasetMax;
            }
        };
    };
})();
