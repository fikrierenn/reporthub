// builder-v2/builder-sp.js — SP fetch + param-bar mixin.
// Object.assign ile builder.js main IIFE'sinde compose edilir.
//
// İçerik: ParamSchema parse, param-bar chip values + override state,
// fetchSpPreview (RunJsonV2 / RunJsonV2Preview path seçimi).

(function () {
    "use strict";
    window.__builderV2 = window.__builderV2 || {};

    window.__builderV2.spMixin = function () {
        return {
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

            // Tüm override'ları sıfırla → default'lara dön
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

            fetchSpPreview() {
                var meta = this.reportMeta;
                if (!meta) return;
                var defaults = this.buildParamDefaults();
                // EditReportV2: reportId var → V1 Run path'iyle aynı SP çağrısı (RunJsonV2).
                // CreateReportV2: reportId yok → RunJsonV2Preview (admin-only, ParamSchema-based,
                // SpPreview default-doldurma bug'ı bypass — SP kendi default'unu kullanır).
                var url;
                if (meta.reportId) {
                    url = '/Reports/RunJsonV2/' + meta.reportId;
                    if (Object.keys(defaults).length > 0) {
                        url += '?paramsJson=' + encodeURIComponent(JSON.stringify(defaults));
                    }
                } else {
                    if (!meta.dataSourceKey || !meta.procName) return;
                    url = '/Reports/RunJsonV2Preview?dataSourceKey=' + encodeURIComponent(meta.dataSourceKey)
                        + '&procName=' + encodeURIComponent(meta.procName);
                    if (meta.paramSchemaJson) {
                        url += '&paramSchemaJson=' + encodeURIComponent(meta.paramSchemaJson);
                    }
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

            resultSets() { return (this.spPreview && this.spPreview.resultSets) || []; }
        };
    };
})();
