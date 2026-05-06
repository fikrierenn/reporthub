// dashboard-builder/builder-templates.js — Hazir sablon presets
// M-11 F-7 alt-commit 1: placeholder + initial render trigger.
// F-10'da dolacak:
//   - 3 sablon (KPI Trio / Trend Grafik / Detay Tablo) JSON
//   - "Sablondan Sec" butonu modal -> canvas'a yukle
//   - Kbd shortcuts modal (?)

(function () {
    "use strict";

    if (!window.DB) return;

    DB.templates = {
        init: function () { /* F-10 */ },
        list: [],          /* F-10: KPI Trio, Trend Grafik, Detay Tablo */
        load: function () { /* F-10: canvas'a yukle */ }
    };

    // ---- Initial render trigger ----
    // 7 modul yuklendi (core/canvas/contract/list/drawer/preview/templates).
    // En son modul olarak initial render'i tetikle.
    if (DB.builderEl) {
        DB.requestRender();
    }
})();
