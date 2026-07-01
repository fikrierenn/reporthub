// Plan 40 (M6) Faz 3 — KVKK Process <-> SOP Document çapraz-modül bağ picker'ları.
// Alpine.data factory pattern (sop-advisor.js ile aynı) — IIFE wrap edilmiyor, x-data="kvkkSopPicker(...)".
// Junction sahibi KVKK modülü (ADR-002); bu dosya iki sayfada da cross-area fetch/POST yapar,
// hiçbir modülü compile-time referans etmez — sadece HTTP.

// KVKK Process Details "SOP" tab'ında kullanılır: SOP ara + bağla, bağlı SOP'ları listele + kaldır.
window.kvkkSopPicker = function (initial) {
    return {
        antiforgery: initial.antiforgery || '',
        processId: initial.processId,
        q: '', results: [], linked: [], loading: false, error: '',

        async init() { await this.loadLinked(); },

        async loadLinked() {
            try {
                var resp = await fetch('/Kvkk/Process/LinkedSopsJson?processId=' + this.processId);
                if (!resp.ok) { this.error = 'Bağlı SOP listesi yüklenemedi.'; this.linked = []; return; }
                this.linked = await resp.json();
            } catch (e) { this.error = 'Bağlantı hatası (liste).'; this.linked = []; }
        },

        async search() {
            var q = this.q.trim();
            if (q.length < 2) { this.results = []; return; }
            try {
                var resp = await fetch('/SOP/Sop/SearchJson?q=' + encodeURIComponent(q));
                if (!resp.ok) { this.error = 'Arama başarısız.'; this.results = []; return; }
                this.error = '';
                this.results = await resp.json();
            } catch (e) { this.error = 'Bağlantı hatası (arama).'; this.results = []; }
        },

        async link(sop) {
            var form = new FormData();
            form.append('processId', this.processId);
            form.append('sopDocumentId', sop.id);
            form.append('__RequestVerificationToken', this.antiforgery);
            this.loading = true;
            try {
                var resp = await fetch('/Kvkk/Process/LinkSop', { method: 'POST', body: form });
                if (resp.ok) { this.q = ''; this.results = []; this.error = ''; await this.loadLinked(); }
                else { this.error = await resp.text() || ('SOP bağlanamadı (HTTP ' + resp.status + ').'); }
            } catch (e) { this.error = 'Bağlantı hatası.'; }
            finally { this.loading = false; }
        },

        async unlink(linkId) {
            if (!window.confirm('SOP bağlantısını kaldırmak istediğinize emin misiniz?')) return;
            var form = new FormData();
            form.append('linkId', linkId);
            form.append('__RequestVerificationToken', this.antiforgery);
            try {
                var resp = await fetch('/Kvkk/Process/UnlinkSop', { method: 'POST', body: form });
                if (resp.ok) { this.error = ''; await this.loadLinked(); }
                else { this.error = await resp.text() || 'Bağlantı kaldırılamadı.'; }
            } catch (e) { this.error = 'Bağlantı hatası.'; }
        },

        async scan(sopId) {
            this.loading = true;
            try {
                var textResp = await fetch('/SOP/Sop/PlainTextJson/' + sopId);
                if (!textResp.ok) { this.error = 'SOP içeriği okunamadı.'; return; }
                var data = await textResp.json();
                if (!data.hasContent) { this.error = 'SOP\'un onaylı bir içeriği yok — tarama yapılamadı.'; return; }

                var form = new FormData();
                form.append('processId', this.processId);
                form.append('sopDocumentId', sopId);
                form.append('plainText', data.plainText || '');
                form.append('__RequestVerificationToken', this.antiforgery);
                var resp = await fetch('/Kvkk/Process/ScanSop', { method: 'POST', body: form });
                if (resp.ok) window.location.reload();
                else this.error = await resp.text() || ('Tarama başarısız (HTTP ' + resp.status + ').');
            } catch (e) { this.error = 'Tarama hatası.'; }
            finally { this.loading = false; }
        }
    };
};

// SOP Details sayfasında kullanılır: KVKK süreci ara + bağla, bağlı süreçleri listele + kaldır.
window.sopKvkkPicker = function (initial) {
    return {
        antiforgery: initial.antiforgery || '',
        sopDocumentId: initial.sopDocumentId,
        q: '', results: [], linked: [], loading: false, error: '',

        async init() { await this.loadLinked(); },

        async loadLinked() {
            try {
                var resp = await fetch('/Kvkk/Process/LinkedProcessesJson?sopDocumentId=' + this.sopDocumentId);
                if (!resp.ok) { this.error = 'Bağlı süreç listesi yüklenemedi.'; this.linked = []; return; }
                this.linked = await resp.json();
            } catch (e) { this.error = 'Bağlantı hatası (liste).'; this.linked = []; }
        },

        async search() {
            var q = this.q.trim();
            if (q.length < 2) { this.results = []; return; }
            try {
                var resp = await fetch('/Kvkk/Process/SearchJson?q=' + encodeURIComponent(q));
                if (!resp.ok) { this.error = 'Arama başarısız.'; this.results = []; return; }
                this.error = '';
                this.results = await resp.json();
            } catch (e) { this.error = 'Bağlantı hatası (arama).'; this.results = []; }
        },

        async link(process) {
            var form = new FormData();
            form.append('processId', process.id);
            form.append('sopDocumentId', this.sopDocumentId);
            form.append('__RequestVerificationToken', this.antiforgery);
            this.loading = true;
            try {
                var resp = await fetch('/Kvkk/Process/LinkSop', { method: 'POST', body: form });
                if (resp.ok) { this.q = ''; this.results = []; this.error = ''; await this.loadLinked(); }
                else { this.error = await resp.text() || ('Süreç bağlanamadı (HTTP ' + resp.status + ').'); }
            } catch (e) { this.error = 'Bağlantı hatası.'; }
            finally { this.loading = false; }
        },

        async unlink(linkId) {
            if (!window.confirm('Süreç bağlantısını kaldırmak istediğinize emin misiniz?')) return;
            var form = new FormData();
            form.append('linkId', linkId);
            form.append('__RequestVerificationToken', this.antiforgery);
            try {
                var resp = await fetch('/Kvkk/Process/UnlinkSop', { method: 'POST', body: form });
                if (resp.ok) { this.error = ''; await this.loadLinked(); }
                else { this.error = await resp.text() || 'Bağlantı kaldırılamadı.'; }
            } catch (e) { this.error = 'Bağlantı hatası.'; }
        }
    };
};
