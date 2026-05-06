// dashboard-builder/builder-preview.js — Live preview iframe (preview = Reports/Run)
// M-11 F-7 alt-commit 1: placeholder. F-9'da dolacak:
//   - "Onizle" butonu -> POST /Admin/DashboardPreview { configOverride } -> srcdoc HTML
//   - Throttle 300ms (form change -> re-fetch)
//   - Validation banner (drawer)
//   - Dirty chip (topbar)
//   - Toast (sag alt)
//   - Geri Al snapshot

(function () {
    "use strict";

    if (!window.DB) return;

    DB.preview = {
        init: function () { /* F-9 */ },
        refresh: function () { /* F-9: POST DashboardPreview */ },
        markDirty: function () { /* F-9: dirty state + chip */ }
    };
})();
