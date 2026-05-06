// builder-v2/builder-feedback.js — F-9 madde 51/52/53: validation banner + toast + Geri Al.
// Object.assign ile builder.js main IIFE'sinde compose edilir.
//
// Sorumluluk alanı:
//  - validationBanner: drawer üstünde kırmızı/sarı banner, hata + uyarı listesi
//  - runValidation(): POST /Admin/Reports/DashboardValidate, JSON sonucu banner state'e yaz
//  - toasts: sağ alt köşe, her toast 3sn auto-dismiss (üstten override edilebilir)
//  - lastSaveSnapshot + restoreSnapshot: page load anında + her save sonrası snapshot al,
//    Geri Al butonu config'i son save'e döndürür (dirty=false)

(function () {
    "use strict";
    window.__builderV2 = window.__builderV2 || {};

    var TOAST_AUTODISMISS_MS = 3000;
    var nextToastId = 1;

    window.__builderV2.feedbackMixin = function () {
        return {
            // --- state ---
            validationBanner: { visible: false, errors: [], warnings: [] },
            toasts: [],
            lastSaveSnapshot: null,

            // --- snapshot (Geri Al) ---
            captureSnapshot() {
                try {
                    this.lastSaveSnapshot = JSON.stringify(this.config);
                } catch (e) {
                    this.lastSaveSnapshot = null;
                }
            },

            restoreSnapshot() {
                if (!this.lastSaveSnapshot) {
                    this.pushToast('Geri alınacak kayıtlı yapılandırma yok.', 'warn');
                    return;
                }
                if (!confirm('Son kaydedilen hâle geri dönülecek. Devam edilsin mi?')) return;
                try {
                    var restored = JSON.parse(this.lastSaveSnapshot);
                    this.config = restored;
                    if (this.grid) {
                        try { this.grid.removeAll(); } catch (e) { /* ignore */ }
                    }
                    this.components = (restored.tabs && restored.tabs[this.activeTabIdx])
                        ? (restored.tabs[this.activeTabIdx].components || []) : [];
                    if (this.mountExistingWidgets) this.mountExistingWidgets();
                    this.syncConfig();
                    if (window.Alpine && Alpine.store && Alpine.store('builder')) {
                        Alpine.store('builder').dirty = false;
                    }
                    this.validationBanner = { visible: false, errors: [], warnings: [] };
                    this.pushToast('Yapılandırma son kayda döndürüldü.', 'success');
                } catch (e) {
                    this.pushToast('Geri alma başarısız: ' + (e && e.message ? e.message : 'snapshot bozuk'), 'error');
                }
            },

            // --- validation ---
            runValidation() {
                var configEl = document.getElementById('DashboardConfigJson');
                var configJson = configEl ? configEl.value : '';
                if (!configJson) {
                    this.validationBanner = { visible: true, errors: ['Pano yapılandırması boş.'], warnings: [] };
                    return Promise.resolve(false);
                }

                var tokenEl = document.querySelector('input[name="__RequestVerificationToken"]');
                var token = tokenEl ? tokenEl.value : '';
                if (!token) {
                    this.pushToast('AntiForgery token bulunamadı; sayfayı yenileyin.', 'error');
                    return Promise.resolve(false);
                }

                var body = new URLSearchParams();
                body.set('configJson', configJson);

                var self = this;
                return fetch('/Admin/Reports/DashboardValidate', {
                    method: 'POST',
                    credentials: 'same-origin',
                    headers: {
                        'Content-Type': 'application/x-www-form-urlencoded',
                        'RequestVerificationToken': token
                    },
                    body: body.toString()
                })
                    .then(function (r) { return r.json(); })
                    .then(function (data) {
                        var errs = data.errors || [];
                        var warns = data.warnings || [];
                        var hasAny = errs.length > 0 || warns.length > 0;
                        self.validationBanner = { visible: hasAny, errors: errs, warnings: warns };
                        if (data.success && warns.length === 0) {
                            self.pushToast('Yapılandırma geçerli.', 'success');
                        } else if (errs.length > 0) {
                            self.pushToast(errs.length + ' hata bulundu — banner\'a bakın.', 'error');
                        } else if (warns.length > 0) {
                            self.pushToast(warns.length + ' uyarı bulundu.', 'warn');
                        }
                        return data.success === true;
                    })
                    .catch(function (err) {
                        self.pushToast('Doğrulama servisi yanıt vermedi: ' + (err && err.message ? err.message : 'bilinmiyor'), 'error');
                        return false;
                    });
            },

            dismissValidationBanner() {
                this.validationBanner = { visible: false, errors: [], warnings: [] };
            },

            // --- toast ---
            pushToast(msg, type, autoDismissMs) {
                var t = {
                    id: nextToastId++,
                    msg: String(msg || ''),
                    type: type || 'info'   // info / success / warn / error
                };
                this.toasts.push(t);
                var ms = typeof autoDismissMs === 'number' ? autoDismissMs : TOAST_AUTODISMISS_MS;
                if (ms > 0) {
                    var self = this;
                    setTimeout(function () { self.dismissToast(t.id); }, ms);
                }
            },

            dismissToast(id) {
                this.toasts = this.toasts.filter(function (t) { return t.id !== id; });
            }
        };
    };
})();
