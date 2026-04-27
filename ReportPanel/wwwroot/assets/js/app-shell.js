// app-shell.js — M-13 Plan 03 Faz A
// Sidebar collapse toggle + Cmd/Ctrl+K search focus + mobile drawer + active nav state.

(function () {
    "use strict";

    var shell = document.querySelector('.app-shell');
    if (!shell) return;

    var btnCollapse = document.getElementById('btnSidebarCollapse');
    var btnMobile = document.getElementById('btnMobileMenu');

    // ---- localStorage state ----
    var STATE_KEY = 'reporthub.sidebar.collapsed';
    function loadState() {
        try {
            return localStorage.getItem(STATE_KEY) === '1';
        } catch (e) { return false; }
    }
    function saveState(collapsed) {
        try {
            localStorage.setItem(STATE_KEY, collapsed ? '1' : '0');
        } catch (e) { /* ignore */ }
    }

    // Apply initial state (mobile = always collapsed-via-overlay)
    if (window.innerWidth >= 1024 && loadState()) {
        shell.classList.add('collapsed');
    }

    // ---- Collapse toggle ----
    function toggleCollapse() {
        // Mobile: toggle drawer
        if (window.innerWidth < 1024) {
            shell.classList.toggle('mobile-open');
            return;
        }
        // Desktop: collapse to icon-only
        shell.classList.toggle('collapsed');
        saveState(shell.classList.contains('collapsed'));
    }

    if (btnCollapse) btnCollapse.addEventListener('click', toggleCollapse);
    if (btnMobile) btnMobile.addEventListener('click', function () {
        shell.classList.toggle('mobile-open');
    });

    // Mobile backdrop click → close drawer
    shell.addEventListener('click', function (e) {
        if (window.innerWidth >= 1024) return;
        if (e.target !== shell) return;
        shell.classList.remove('mobile-open');
    });

    // ---- Keyboard shortcuts ----
    document.addEventListener('keydown', function (e) {
        // Ctrl+B / Cmd+B → sidebar collapse (desktop only)
        if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'b') {
            // Skip if user typing in an input/textarea
            var t = e.target.tagName;
            if (t === 'INPUT' || t === 'TEXTAREA' || e.target.isContentEditable) return;
            e.preventDefault();
            toggleCollapse();
        }
        // Ctrl+K / Cmd+K → search focus
        if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') {
            var search = document.querySelector('.topbar .search input');
            if (search) {
                e.preventDefault();
                search.focus();
                search.select();
            }
        }
        // Esc → close mobile drawer
        if (e.key === 'Escape' && shell.classList.contains('mobile-open')) {
            shell.classList.remove('mobile-open');
        }
    });

    // ---- Window resize: clean mobile state when going desktop ----
    var resizeTimer;
    window.addEventListener('resize', function () {
        clearTimeout(resizeTimer);
        resizeTimer = setTimeout(function () {
            if (window.innerWidth >= 1024) {
                shell.classList.remove('mobile-open');
                if (loadState()) shell.classList.add('collapsed');
                else shell.classList.remove('collapsed');
            }
        }, 100);
    });
})();
