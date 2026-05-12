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

// Plan 25.2 — Global AntiForgery helper. POST fetch'lerinde her dosyada
// `document.querySelector('input[name="__RequestVerificationToken"]')` duplicate
// edilmesin diye burada (her sayfada yüklü) tek source.
//
// Cache stratejisi: SADECE truthy değer cache edilir. Bir önceki impl boş string'i
// kalıcı cache'liyordu — token element DOM'da yokken (Login öncesi / async render)
// `''` set olunca tüm POST'lar 400 alıyordu. Şimdi her çağrıda token bulunana kadar
// DOM lookup tekrar edilir, bulunduktan sonra cache'lenir.
// `_AppLayout.cshtml` her sayfada global `@Html.AntiForgeryToken()` render eder.
(function () {
    "use strict";
    var __aftCache = '';
    window.getAntiForgeryToken = function () {
        if (__aftCache) return __aftCache;
        var el = document.querySelector('input[name="__RequestVerificationToken"]');
        __aftCache = el ? el.value : '';
        return __aftCache;
    };
    // Form swap / HTMX sonrası cache reset için açık API.
    window.invalidateAntiForgeryToken = function () { __aftCache = ''; };
})();

// Plan 23 — Sidebar collapsible group Alpine factory.
// _AppLayout.cshtml her <div class="side-group"> için x-data="sidebarGroup('<key>')" tanımlar.
// localStorage'a 'mosaik.sidebar.group.<key>' = '0'|'1' yazar (default açık).
// Mobile (<1024px) drawer'da stored state ignore, hep açık başlar.
window.sidebarGroup = function (groupKey) {
    return {
        key: groupKey,
        open: true,
        init: function () {
            try {
                var stored = localStorage.getItem('mosaik.sidebar.group.' + this.key);
                if (stored !== null && window.innerWidth >= 1024) {
                    this.open = stored !== '0';
                }
            } catch (e) { /* ignore */ }
        },
        toggle: function () {
            this.open = !this.open;
            try {
                localStorage.setItem('mosaik.sidebar.group.' + this.key, this.open ? '1' : '0');
            } catch (e) { /* ignore */ }
        }
    };
};
