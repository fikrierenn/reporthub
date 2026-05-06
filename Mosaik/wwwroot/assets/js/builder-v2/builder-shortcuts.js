// builder-v2/builder-shortcuts.js — F-10 madde 58-59: keyboard shortcuts + ? help modal mixin.
// Object.assign ile builder.js main IIFE'sinde compose edilir.
//
// Bağlanan kısayollar (her zaman builder root görünürken aktif):
//   Ctrl+S / ⌘S  → form submit (Kaydet)
//   Ctrl+P / ⌘P  → Tam Önizle (previewMixin.openPreview)
//   Esc          → seçili widget'ı bırak (selectedId=null) veya açık modal'ı kapat
//   Delete       → seçili widget'ı sil (input/textarea içindeyken çalışmaz)
//   Shift+?      → kısayol yardım modal'ını aç/kapat
//
// initShortcuts() init() içinde bir kez çağrılır; document.keydown listener.
// Form input/textarea/select'lerde tipik kısayollar (Ctrl+S hariç) bypass edilir.

(function () {
    "use strict";
    window.__builderV2 = window.__builderV2 || {};

    function isTypingInField(target) {
        if (!target) return false;
        var tag = (target.tagName || '').toUpperCase();
        return tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT' || target.isContentEditable;
    }

    window.__builderV2.shortcutsMixin = function () {
        return {
            shortcutsModal: { open: false },

            openShortcutsHelp() { this.shortcutsModal = { open: true }; },
            closeShortcutsHelp() { this.shortcutsModal = { open: false }; },

            initShortcuts() {
                var self = this;
                document.addEventListener('keydown', function (e) {
                    var typing = isTypingInField(e.target);

                    // Ctrl/Cmd+S → Kaydet (form submit)
                    if ((e.ctrlKey || e.metaKey) && e.key === 's') {
                        e.preventDefault();
                        var form = document.getElementById('reportEditFormV2')
                                || document.getElementById('reportCreateFormV2');
                        if (form) {
                            if (window.Alpine && Alpine.store && Alpine.store('builder')) {
                                Alpine.store('builder').dirty = false;
                            }
                            form.submit();
                        }
                        return;
                    }

                    // Ctrl/Cmd+P → Tam Önizle
                    if ((e.ctrlKey || e.metaKey) && e.key === 'p') {
                        e.preventDefault();
                        if (self.openPreview) self.openPreview();
                        return;
                    }

                    // Form alanında ise diğer kısayolları yutma (Esc hariç — modal kapatma)
                    if (typing && e.key !== 'Escape') return;

                    // Esc → seçili widget'ı bırak veya açık modal'ı kapat
                    if (e.key === 'Escape') {
                        if (self.previewModal && self.previewModal.open) {
                            self.closePreview && self.closePreview();
                        } else if (self.shortcutsModal && self.shortcutsModal.open) {
                            self.closeShortcutsHelp();
                        } else if (self.templateModal && self.templateModal.open) {
                            self.closeTemplateModal();
                        } else if (self.dataModal && self.dataModal.open) {
                            self.dataModal = { open: false, rsIdx: null, comp: null };
                        } else if (self.selectedId) {
                            self.selectedId = null;
                        }
                        return;
                    }

                    // Delete → seçili widget'ı sil
                    if (e.key === 'Delete' && self.selectedId) {
                        e.preventDefault();
                        var id = self.selectedId;
                        var el = document.querySelector('[data-widget-id="' + id + '"]');
                        if (self.removeWidget) self.removeWidget(id, el);
                        self.selectedId = null;
                        return;
                    }

                    // Shift+? (US klavyede '?') → yardım modal toggle
                    if (e.key === '?' && !e.ctrlKey && !e.metaKey && !e.altKey) {
                        e.preventDefault();
                        if (self.shortcutsModal && self.shortcutsModal.open) self.closeShortcutsHelp();
                        else self.openShortcutsHelp();
                        return;
                    }
                });
            }
        };
    };
})();
