// builder-v2/builder-settings.js — Drawer Ayarlar tab (rapor metadata + SP seçim) mixin.
// V1 sp-helper.js'in Alpine reactive port'u: DataSource → SpList → ProcName → ProcParams → ParamSchemaJson.
//
// State'ler reactive — hidden form input'lara x-bind:value ile bağlanır → form POST'a doğru değer gider.

(function () {
    "use strict";
    window.__builderV2 = window.__builderV2 || {};

    window.__builderV2.settingsMixin = function () {
        return {
            // ---- Reactive settings state ----
            title: '',
            dataSourceKey: '',
            procName: '',
            paramSchemaJson: '',
            description: '',
            isActive: true,
            availableRoles: [],
            selectedRoleIds: [],
            availableCategories: [],
            selectedCategoryIds: [],
            availableDataSources: [],
            spList: [],
            procParams: [],

            // Init — main builder.js init() içinden çağrılır
            initSettings() {
                var meta = window.__reportMeta || {};
                this.title = meta.title || '';
                this.dataSourceKey = meta.dataSourceKey || '';
                this.procName = meta.procName || '';
                this.paramSchemaJson = meta.paramSchemaJson || '';
                this.description = meta.description || '';
                this.isActive = meta.isActive !== undefined ? meta.isActive : true;
                this.availableRoles = meta.availableRoles || [];
                this.selectedRoleIds = meta.selectedRoleIds ? Array.from(meta.selectedRoleIds) : [];
                this.availableCategories = meta.availableCategories || [];
                this.selectedCategoryIds = meta.selectedCategoryIds ? Array.from(meta.selectedCategoryIds) : [];
                this.availableDataSources = window.__availableDataSources || [];
                if (this.dataSourceKey) this.loadSpList();
            },

            toggleRole(roleId) {
                var idx = this.selectedRoleIds.indexOf(roleId);
                if (idx === -1) this.selectedRoleIds.push(roleId);
                else this.selectedRoleIds.splice(idx, 1);
            },

            toggleCategory(catId) {
                var idx = this.selectedCategoryIds.indexOf(catId);
                if (idx === -1) this.selectedCategoryIds.push(catId);
                else this.selectedCategoryIds.splice(idx, 1);
            },

            // DataSource değişti → SP listesi yenilenir, mevcut ProcName temizlenir
            onDataSourceChange() {
                this.spList = [];
                this.procParams = [];
                this.procName = '';
                this.paramSchemaJson = '';
                if (this.dataSourceKey) this.loadSpList();
            },

            // ProcName seçildi/yazıldı → SP parametreleri çekilir, ParamSchemaJson üretilir
            onProcNameChange() {
                if (!this.dataSourceKey || !this.procName) {
                    this.procParams = [];
                    return;
                }
                this.loadProcParams();
            },

            loadSpList() {
                var self = this;
                fetch('/Admin/SpList?dataSourceKey=' + encodeURIComponent(this.dataSourceKey),
                    { credentials: 'same-origin' })
                    .then(function (r) { return r.ok ? r.json() : null; })
                    .then(function (data) {
                        self.spList = (data && data.procedures) || [];
                    })
                    .catch(function () { self.spList = []; });
            },

            loadProcParams() {
                var self = this;
                fetch('/Admin/ProcParams?dataSourceKey=' + encodeURIComponent(this.dataSourceKey)
                    + '&procName=' + encodeURIComponent(this.procName),
                    { credentials: 'same-origin' })
                    .then(function (r) { return r.ok ? r.json() : null; })
                    .then(function (data) {
                        self.procParams = (data && data.fields) || [];
                        self.paramSchemaJson = self.buildParamSchemaJson(self.procParams);
                        // Yeni schema → SP preview'i yeniden çek (CreateReportV2'de zero-data fix akışı)
                        if (self.fetchSpPreview) self.fetchSpPreview();
                    })
                    .catch(function () { self.procParams = []; });
            },

            // ProcParams [{name, type}] → ParamSchemaJson string ({fields: [...]} formatı)
            // Default: date → 'today', diğer tipler için boş
            buildParamSchemaJson(fields) {
                if (!fields || fields.length === 0) return '';
                var schema = {
                    fields: fields.map(function (p) {
                        var f = {
                            name: p.name,
                            label: p.name,
                            type: p.type || 'text',
                            required: false
                        };
                        if (f.type === 'date') f.default = 'today';
                        return f;
                    })
                };
                return JSON.stringify(schema, null, 2);
            }
        };
    };
})();
