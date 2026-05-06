// builder-v2/builder-kpi-fields.js — F09 Faz 3: KPI variant-spesifik form alanları + capacity warning.
// Object.assign ile builder.js main IIFE'sinde compose edilir.
//
// Drawer Veri tab'ında KPI variant değiştiğinde gerekli alanlar (compareColumn, trend.valueColumn,
// progress.target...) UI'da koşullu görünür ve auto-fill ile akıllı doldurulur.
//
// Server validator (DashboardConfigValidator.ValidateKpiVariantRequirements) variant başına
// alan zorunluluğu kontrol ediyor; UI o alanları sağlamadığı için kayıt sırasında hata vermez ama
// renderer "—" basıyordu — bu mixin onu kapatır.

(function () {
    "use strict";
    window.__builderV2 = window.__builderV2 || {};

    // Variant -> gerekli alan field gereklilikleri
    var REQS = {
        basic:     { needsBaseColumn: true,  needsCompareColumn: false, needsTrendValue: false, needsTarget: false, label: 'Temel' },
        delta:     { needsBaseColumn: true,  needsCompareColumn: true,  needsTrendValue: false, needsTarget: false, label: 'Değişim' },
        sparkline: { needsBaseColumn: true,  needsCompareColumn: false, needsTrendValue: true,  needsTarget: false, label: 'Mini Grafik' },
        progress:  { needsBaseColumn: true,  needsCompareColumn: false, needsTrendValue: false, needsTarget: true,  label: 'İlerleme' }
    };

    window.__builderV2.kpiFieldsMixin = function () {
        return {
            kpiFieldRequirements(variant) {
                return REQS[variant] || REQS.basic;
            },

            // KPI için sayı kolonları — bağlı RS'ten
            kpiNumberColumns() {
                var rs = this.selectedRs ? this.selectedRs() : null;
                if (!rs) return [];
                var self = this;
                return (rs.columns || []).filter(function (c) {
                    return self.columnKind(rs, c) === 'sayı';
                });
            },

            // Yetersiz veri durumunda banner uyarısı
            kpiCapacityWarning() {
                var c = this.selected;
                if (!c || c.type !== 'kpi') return null;
                var req = this.kpiFieldRequirements(c.variant || 'basic');
                var nums = this.kpiNumberColumns();

                if (req.needsCompareColumn && nums.length < 2)
                    return req.label + ' için en az 2 sayı kolonu gerekli (değer + karşılaştırma); bu RS\'de ' + nums.length + ' var.';
                if (req.needsTrendValue && nums.length < 1)
                    return req.label + ' için bir sayı kolonu gerekli (trend değeri); bu RS\'de yok.';
                if (req.needsTarget) {
                    var hasTarget = c.progress && (c.progress.targetValue != null || c.progress.targetColumn);
                    if (!hasTarget) return req.label + ' için hedef belirleyin (sabit değer veya kolon).';
                }
                return null;
            },

            // Variant değiştiğinde akıllı dolum: ilgili alanlar boşsa uygun varsayılan ata, manuel saygı
            applyKpiFieldDefaults() {
                var c = this.selected;
                if (!c || c.type !== 'kpi') return;
                var variant = c.variant || 'basic';
                var nums = this.kpiNumberColumns();

                // delta: compareColumn boşsa, ana column dışındaki ilk sayı kolonu
                if (variant === 'delta') {
                    if (!c.delta) c.delta = {};
                    if (!c.delta.compareColumn) {
                        var fallback = nums.filter(function (n) { return n !== c.column; })[0];
                        if (fallback) c.delta.compareColumn = fallback;
                    }
                    if (!c.delta.compareLabel) c.delta.compareLabel = 'vs önceki';
                }

                // sparkline: trend.valueColumn boşsa, ana column'u kullan
                if (variant === 'sparkline') {
                    if (!c.trend) c.trend = {};
                    if (!c.trend.valueColumn) c.trend.valueColumn = c.column || nums[0] || '';
                }

                // progress: hedef boşsa Sabit 100 default
                if (variant === 'progress') {
                    if (!c.progress) c.progress = {};
                    var hasTarget = c.progress.targetValue != null || c.progress.targetColumn;
                    if (!hasTarget) c.progress.targetValue = 100;
                }

                if (this.refreshAllWidgets) this.refreshAllWidgets();
                if (this.syncConfig) this.syncConfig();
            },

            // Variant butonu bunu çağırır (drawer KPI variant grid'i)
            setKpiVariant(v) {
                if (this.setField) this.setField('variant', v);
                this.applyKpiFieldDefaults();
            },

            // delta.compareColumn / delta.compareLabel setter
            setDeltaField(field, val) {
                var c = this.selected;
                if (!c) return;
                if (!c.delta) c.delta = {};
                c.delta[field] = val || null;
                if (this.refreshAllWidgets) this.refreshAllWidgets();
                if (this.syncConfig) this.syncConfig();
            },

            // trend.valueColumn / trend.labelColumn setter
            setTrendField(field, val) {
                var c = this.selected;
                if (!c) return;
                if (!c.trend) c.trend = {};
                c.trend[field] = val || null;
                if (this.refreshAllWidgets) this.refreshAllWidgets();
                if (this.syncConfig) this.syncConfig();
            },

            // progress.targetValue / progress.targetColumn — biri set edilince diğeri temizlenir
            setProgressField(field, val) {
                var c = this.selected;
                if (!c) return;
                if (!c.progress) c.progress = {};
                if (field === 'targetValue') {
                    c.progress.targetValue = (val === '' || val == null) ? null : parseFloat(val);
                    if (c.progress.targetValue != null) c.progress.targetColumn = null;
                } else if (field === 'targetColumn') {
                    c.progress.targetColumn = val || null;
                    if (c.progress.targetColumn) c.progress.targetValue = null;
                }
                if (this.refreshAllWidgets) this.refreshAllWidgets();
                if (this.syncConfig) this.syncConfig();
            },

            // Progress hedef tipi — Sabit Değer mi Kolon mu?
            progressTargetMode() {
                var c = this.selected;
                if (!c || !c.progress) return 'value';
                if (c.progress.targetColumn) return 'column';
                return 'value';
            },

            switchProgressTargetMode(mode) {
                var c = this.selected;
                if (!c) return;
                if (!c.progress) c.progress = {};
                if (mode === 'column') {
                    c.progress.targetValue = null;
                    if (!c.progress.targetColumn) {
                        var nums = this.kpiNumberColumns();
                        c.progress.targetColumn = nums[0] || '';
                    }
                } else {
                    c.progress.targetColumn = null;
                    if (c.progress.targetValue == null) c.progress.targetValue = 100;
                }
                if (this.refreshAllWidgets) this.refreshAllWidgets();
                if (this.syncConfig) this.syncConfig();
            }
        };
    };
})();
