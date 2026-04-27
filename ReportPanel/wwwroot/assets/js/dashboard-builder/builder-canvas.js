// dashboard-builder/builder-canvas.js — Gridstack canvas wrapper
// M-11 F-7 alt-commit 1: placeholder. Gridstack entegrasyonu alt-commit 3'te (CDN + init).
// Mevcut alt-1 fonksiyonel parite: list-based UI korunuyor (builder-list.js).
// Bu modul alt-3'te asagidaki API'yi saglayacak:
//   - DB.canvas.init(gridstackOptions)
//   - DB.canvas.loadLayout(state)
//   - DB.canvas.saveLayout() -> { id, x, y, w, h }[]
//   - DB.canvas.destroy()

(function () {
    "use strict";

    if (!window.DB) return;

    DB.canvas = {
        init: function () { /* alt-3: GridStack.init(...) */ },
        loadLayout: function () { /* alt-3 */ },
        saveLayout: function () { return []; /* alt-3 */ },
        destroy: function () { /* alt-3 */ }
    };
})();
