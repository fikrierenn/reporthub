// Mosaik focus-trap utility — Plan 39 follow-up (dialog a11y).
// API: MosaikFocusTrap.activate(rootEl) / .deactivate(rootEl)
// - rootEl must be the dialog container.
// - On activate: saves currently focused element, focuses first tabbable inside root,
//   intercepts Tab/Shift+Tab to cycle focus inside root.
// - On deactivate: removes listener, restores prior focus.
// Caller handles ESC (close action) — utility only manages focus.
(function () {
    'use strict';

    var FOCUSABLE_SELECTOR = [
        'a[href]',
        'area[href]',
        'button:not([disabled])',
        'input:not([disabled]):not([type="hidden"])',
        'select:not([disabled])',
        'textarea:not([disabled])',
        'iframe',
        '[tabindex]:not([tabindex="-1"])',
        '[contenteditable="true"]'
    ].join(',');

    var traps = new Map();

    function isVisible(el) {
        if (!el) return false;
        if (el.hidden) return false;
        var style = window.getComputedStyle(el);
        return style.display !== 'none' && style.visibility !== 'hidden';
    }

    function getFocusable(root) {
        if (!root) return [];
        var nodes = root.querySelectorAll(FOCUSABLE_SELECTOR);
        var out = [];
        for (var i = 0; i < nodes.length; i++) {
            if (isVisible(nodes[i])) out.push(nodes[i]);
        }
        return out;
    }

    function activate(root) {
        if (!root || traps.has(root)) return;

        var prev = document.activeElement;
        var nodes = getFocusable(root);

        function handler(e) {
            if (e.key !== 'Tab') return;
            var current = getFocusable(root);
            if (current.length === 0) {
                e.preventDefault();
                root.focus();
                return;
            }
            var first = current[0];
            var last = current[current.length - 1];
            if (e.shiftKey && document.activeElement === first) {
                e.preventDefault();
                last.focus();
            } else if (!e.shiftKey && document.activeElement === last) {
                e.preventDefault();
                first.focus();
            }
        }

        root.addEventListener('keydown', handler);
        traps.set(root, { prev: prev, handler: handler });

        var initial = nodes[0] || root;
        setTimeout(function () {
            try { initial.focus(); } catch (e) { /* element gone */ }
        }, 0);
    }

    function deactivate(root) {
        if (!root || !traps.has(root)) return;
        var t = traps.get(root);
        root.removeEventListener('keydown', t.handler);
        traps.delete(root);
        if (t.prev && typeof t.prev.focus === 'function' && document.contains(t.prev)) {
            try { t.prev.focus(); } catch (e) { /* element gone */ }
        }
    }

    window.MosaikFocusTrap = { activate: activate, deactivate: deactivate };
})();
