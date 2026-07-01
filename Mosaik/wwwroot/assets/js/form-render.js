// Plan 41 Faz 1 — survey-core (MIT) render + Alpine glue + fetch submit.
// Alpine.data factory pattern (kvkk-sop-link.js/sop-advisor.js ile aynı) — IIFE wrap edilmiyor.
// XSS: survey-core kendi DOM render'ını yapar (textContent-eşdeğeri); bizim tarafımızda
// innerHTML/eval yok. Tüm dinamik değerler window.__mosaikForm* script-context globals üzerinden
// (Render.cshtml, encoder pinli) — x-data HTML attribute'una GÖMÜLMEZ (JSON içindeki çift-tırnak
// attribute'u kırardı, preview'de yakalanan gerçek bug).
window.mosaikFormRender = function () {
    return {
        submitUrl: window.__mosaikFormSubmitUrl || '',
        submittedUrl: window.__mosaikFormSubmittedUrl || '',
        antiforgery: document.querySelector('input[name="__RequestVerificationToken"]')?.value || '',
        error: '',
        survey: null,

        init() {
            var schemaJson = window.__mosaikFormSchemaJson;
            if (!schemaJson) { this.error = 'Form şeması yüklenemedi.'; return; }

            var schema;
            try { schema = JSON.parse(schemaJson); }
            catch (e) { console.error(e); this.error = 'Form şeması bozuk.'; return; }

            try {
                this.survey = new Survey.Model(schema);
                var self = this;
                this.survey.onComplete.add(function (sender) { self.submit(sender.data); });
                this.survey.render(document.getElementById('surveyContainer'));
            } catch (e) {
                console.error(e);
                this.error = 'Form yüklenemedi — şema uyumsuz olabilir.';
            }
        },

        async submit(data) {
            this.error = '';
            var form = new FormData();
            for (var key in data) {
                if (Object.prototype.hasOwnProperty.call(data, key)) {
                    var val = data[key];
                    form.append(key, (val === null || val === undefined) ? '' : String(val));
                }
            }
            form.append('__RequestVerificationToken', this.antiforgery);

            try {
                var resp = await fetch(this.submitUrl, { method: 'POST', body: form });
                if (resp.ok) {
                    window.location.href = this.submittedUrl;
                    return;
                }

                var contentType = resp.headers.get('content-type') || '';
                if (contentType.indexOf('application/json') !== -1) {
                    // Alan-bazlı hata dict'i (fieldKey→mesaj) — survey-core'un kendi alan-altı
                    // hata gösterimine bağla (silent-failure-hunter HIGH fix).
                    var fieldErrors = await resp.json();
                    var unmatched = [];
                    for (var fk in fieldErrors) {
                        if (!Object.prototype.hasOwnProperty.call(fieldErrors, fk)) continue;
                        var q = this.survey ? this.survey.getQuestionByName(fk) : null;
                        if (q) q.addError(fieldErrors[fk]);
                        else unmatched.push(fieldErrors[fk]);
                    }
                    if (unmatched.length) this.error = unmatched.join(' ');
                } else {
                    this.error = await resp.text() || ('Gönderilemedi (HTTP ' + resp.status + ').');
                }
            } catch (e) {
                console.error(e);
                this.error = 'Bağlantı hatası — form gönderilemedi.';
            }
        }
    };
};
