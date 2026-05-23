// Plan 34.1 Faz 4 A-21 — SOP RAG advisor Alpine factory.
// IIFE wrap edilmiyor — Alpine.data global olarak tanımlanır + x-data="sopAdvisor(...)" çağrılır.
// XSS koruması: tüm dinamik metin x-text / textContent ile render (innerHTML yok).
window.sopAdvisor = function (initial) {
    return {
        antiforgery: initial.antiforgery || '',
        question: '',
        loading: false,
        latestAnswer: null,
        lastError: '',
        history: (initial.history || []).map(function (h) {
            return {
                id: h.id,
                question: h.question,
                answer: h.answer,
                createdAt: h.createdAt,
                feedback: h.feedback
            };
        }),

        async ask() {
            var q = this.question.trim();
            if (!q || this.loading) return;

            this.loading = true;
            this.lastError = '';

            try {
                var form = new FormData();
                form.append('question', q);
                form.append('__RequestVerificationToken', this.antiforgery);

                var resp = await fetch('/SOP/Advisor/Ask', {
                    method: 'POST',
                    body: form
                });

                if (!resp.ok) {
                    this.lastError = 'Sunucu hatası (HTTP ' + resp.status + ').';
                    return;
                }

                var data = await resp.json();
                if (!data.success) {
                    this.lastError = data.error || 'Bilinmeyen hata.';
                    return;
                }

                this.latestAnswer = {
                    conversationId: data.conversationId,
                    answer: data.answer,
                    noHits: data.noHits,
                    sources: data.sources || [],
                    feedback: 0
                };

                // Geçmişe ekle (en üste).
                this.history.unshift({
                    id: Date.now(),
                    question: q,
                    answer: data.answer,
                    createdAt: new Date().toISOString(),
                    feedback: 0
                });
                if (this.history.length > 10) this.history.length = 10;

                this.question = '';
            } catch (err) {
                this.lastError = 'Bağlantı hatası: ' + (err && err.message ? err.message : 'bilinmiyor');
            } finally {
                this.loading = false;
            }
        },

        // Plan 34.1 Faz 5 A-26 — feedback POST.
        async sendFeedback(target, value) {
            if (!target || !target.conversationId) return;
            if (target.feedback === value) return;                          // aynı tıklama no-op

            var note = '';
            if (value === 2) {                                              // ThumbsDown → opsiyonel not
                note = window.prompt('Neden bu cevap iyi değil? (opsiyonel, max 500 karakter)', '') || '';
                if (note.length > 500) note = note.substring(0, 500);
            }

            try {
                var form = new FormData();
                form.append('conversationId', target.conversationId);
                form.append('feedback', value === 1 ? 'up' : 'down');
                if (note) form.append('note', note);
                form.append('__RequestVerificationToken', this.antiforgery);

                var resp = await fetch('/SOP/Advisor/Feedback', {
                    method: 'POST',
                    body: form
                });
                var data = await resp.json();
                if (data.success) {
                    target.feedback = value;
                } else {
                    this.lastError = data.error || 'Geri bildirim kaydedilemedi.';
                }
            } catch (err) {
                this.lastError = 'Bağlantı hatası: ' + (err && err.message ? err.message : 'bilinmiyor');
            }
        }
    };
};
