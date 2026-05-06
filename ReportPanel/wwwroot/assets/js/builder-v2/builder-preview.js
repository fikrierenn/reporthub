// builder-v2/builder-preview.js — F-9: Tam dashboard preview iframe modal mixin.
// Object.assign ile builder.js main IIFE'sinde compose edilir.
//
// `Tam Önizle` butonu → mevcut configJson + paramSchemaJson + paramOverrides ile
// POST /Admin/Reports/PreviewDashboardV2 → text/html → iframe srcdoc.
// Draft config kaydedilmeden çalıştırılabilir; admin-only + AntiForgery.

(function () {
    "use strict";
    window.__builderV2 = window.__builderV2 || {};

    window.__builderV2.previewMixin = function () {
        return {
            previewModal: { open: false, html: '', loading: false, error: '' },

            openPreview() {
                var meta = this.reportMeta || {};
                var configEl = document.getElementById('DashboardConfigJson');
                var configJson = configEl ? configEl.value : '';
                var dsKey = this.dataSourceKey || meta.dataSourceKey || '';
                var proc = this.procName || meta.procName || '';
                var schemaJson = this.paramSchemaJson || meta.paramSchemaJson || '';

                if (!dsKey || !proc) {
                    this.previewModal = { open: true, html: '', loading: false,
                        error: 'Veri kaynağı ve SP seçilmeden önizleme yapılamaz. Drawer Ayarlar tab\'ından seçin.' };
                    return;
                }
                if (!configJson) {
                    this.previewModal = { open: true, html: '', loading: false,
                        error: 'Pano yapılandırması boş — önce widget ekleyin.' };
                    return;
                }

                // AntiForgery token — sayfada @Html.AntiForgeryToken() ile DOM'da var
                var tokenEl = document.querySelector('input[name="__RequestVerificationToken"]');
                var token = tokenEl ? tokenEl.value : '';
                if (!token) {
                    this.previewModal = { open: true, html: '', loading: false,
                        error: 'AntiForgery token bulunamadı; sayfayı yenileyin.' };
                    return;
                }

                this.previewModal = { open: true, html: '', loading: true, error: '' };

                var defaults = this.buildParamDefaults ? this.buildParamDefaults() : {};
                var body = new URLSearchParams();
                body.set('dataSourceKey', dsKey);
                body.set('procName', proc);
                body.set('configJson', configJson);
                if (Object.keys(defaults).length > 0) body.set('paramsJson', JSON.stringify(defaults));
                if (schemaJson) body.set('paramSchemaJson', schemaJson);
                if (meta.reportId) body.set('reportId', String(meta.reportId));

                var self = this;
                fetch('/Admin/Reports/PreviewDashboardV2', {
                    method: 'POST',
                    credentials: 'same-origin',
                    headers: {
                        'Content-Type': 'application/x-www-form-urlencoded',
                        'RequestVerificationToken': token
                    },
                    body: body.toString()
                })
                    .then(function (r) {
                        if (r.ok) return r.text().then(function (html) { return { ok: true, html: html }; });
                        return r.text().then(function (msg) { return { ok: false, msg: msg || ('HTTP ' + r.status) }; });
                    })
                    .then(function (res) {
                        if (res.ok) {
                            self.previewModal = { open: true, html: res.html, loading: false, error: '' };
                        } else {
                            self.previewModal = { open: true, html: '', loading: false, error: res.msg };
                        }
                    })
                    .catch(function (err) {
                        self.previewModal = { open: true, html: '', loading: false,
                            error: 'Beklenmedik ağ hatası: ' + (err && err.message ? err.message : 'bilinmiyor') };
                    });
            },

            closePreview() {
                this.previewModal = { open: false, html: '', loading: false, error: '' };
            }
        };
    };
})();
