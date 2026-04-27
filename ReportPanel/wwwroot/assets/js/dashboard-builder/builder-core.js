// dashboard-builder/builder-core.js — namespace + state + helpers + render orchestration
// M-11 F-7 alt-commit 1: 775 satir tek dosyadan 7 modul split. Fonksiyonel parite.
// Yukleme sirasi: core -> canvas -> contract -> list -> drawer -> preview -> templates

(function () {
    "use strict";

    var configEl = document.getElementById("DashboardConfigJson");
    var builderEl = document.getElementById("dashboardBuilder");
    if (!configEl || !builderEl) return;

    // ---- State ----
    var state = {
        config: { schemaVersion: 1, layout: "standard", resultContract: {}, tabs: [{ title: "Genel", components: [] }] },
        activeTab: 0,
        editIndex: -1,
        jsonViewOpen: false
    };

    // Try parse existing config
    try {
        var raw = configEl.value.trim();
        if (raw) {
            var parsed = JSON.parse(raw);
            if (parsed && parsed.tabs && parsed.tabs.length > 0) state.config = parsed;
        }
    } catch (e) { }

    // Forward-compat
    if (!state.config.resultContract || typeof state.config.resultContract !== 'object') state.config.resultContract = {};
    if (!state.config.schemaVersion) state.config.schemaVersion = 1;

    // ---- Constants ----
    var constants = {
        colors: [
            { value: "blue", label: "Mavi", hex: "#3b82f6" },
            { value: "green", label: "Yesil", hex: "#10b981" },
            { value: "red", label: "Kirmizi", hex: "#ef4444" },
            { value: "yellow", label: "Sari", hex: "#f59e0b" },
            { value: "gray", label: "Gri", hex: "#6b7280" },
            { value: "indigo", label: "Indigo", hex: "#6366f1" },
            { value: "purple", label: "Mor", hex: "#a855f7" }
        ],
        icons: [
            "fas fa-users", "fas fa-chart-bar", "fas fa-chart-line", "fas fa-chart-pie",
            "fas fa-check-circle", "fas fa-times-circle", "fas fa-clock", "fas fa-calendar",
            "fas fa-database", "fas fa-table", "fas fa-list", "fas fa-th",
            "fas fa-user", "fas fa-store", "fas fa-building", "fas fa-cog",
            "fas fa-exclamation-triangle", "fas fa-info-circle", "fas fa-star", "fas fa-fire",
            "fas fa-arrow-up", "fas fa-arrow-down", "fas fa-percent", "fas fa-coins"
        ],
        aggOptions: [
            { value: "count", label: "Satir Sayisi" },
            { value: "countWhere", label: "Filtreli Sayim" },
            { value: "sum", label: "Toplam" },
            { value: "avg", label: "Ortalama" },
            { value: "min", label: "Minimum" },
            { value: "max", label: "Maksimum" },
            { value: "first", label: "Ilk Deger" }
        ],
        shapes: ["row", "table"]
    };

    // ---- Helpers ----
    function sync() {
        configEl.value = JSON.stringify(state.config, null, 2);
    }

    function val(id) {
        var el = document.getElementById(id);
        return el ? el.value.trim() : '';
    }

    function esc(s) {
        if (!s) return '';
        return s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
    }

    // Widget id format: w_<type>_<hash8>. Stabil, auto-gen on first save, immutable.
    function genWidgetId(type) {
        var hash = Math.random().toString(16).slice(2, 10);
        if (hash.length < 8) hash = (hash + "00000000").slice(0, 8);
        return "w_" + (type || "x") + "_" + hash;
    }

    function colorSelect(name, selected) {
        return '<select id="' + name + '" class="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm">' +
            constants.colors.map(function (c) {
                return '<option value="' + c.value + '"' + (c.value === selected ? ' selected' : '') +
                    ' style="color:' + c.hex + '">' + c.label + '</option>';
            }).join('') + '</select>';
    }

    function iconSelect(name, selected) {
        return '<select id="' + name + '" class="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm">' +
            constants.icons.map(function (ic) {
                return '<option value="' + ic + '"' + (ic === selected ? ' selected' : '') + '>' + ic.replace('fas fa-', '') + '</option>';
            }).join('') + '</select>';
    }

    function shapeSelect(selected, dataKey) {
        var html = '<select class="w-full px-2 py-1 border border-gray-300 rounded text-sm" data-rc-key="' + dataKey + '" onchange="window._dbUpdateContractField(this, \'shape\')">';
        html += '<option value=""' + (!selected ? ' selected' : '') + '>— yok —</option>';
        constants.shapes.forEach(function (s) {
            html += '<option value="' + s + '"' + (selected === s ? ' selected' : '') + '>' + s + '</option>';
        });
        html += '</select>';
        return html;
    }

    // ---- Render orchestrator ----
    // Modul-spesifik render fonksiyonlarini DB.contract / DB.list / DB.drawer'dan cagirir.
    function render() {
        var html = '';

        // ---- F-7 alt-2 split-pane shell: palette (sol 320px) + canvas (orta) + drawer (sag 380px) ----
        // Layout selector (compact, palette ust kosesinde)
        var layoutBar = '<div class="flex items-center gap-2 mb-3 text-xs">';
        layoutBar += '<span class="font-semibold text-gray-500 uppercase">Layout:</span>';
        ['standard', 'compact', 'wide'].forEach(function (l) {
            var labels = { standard: '4', compact: '2', wide: 'Tam' };
            var active = state.config.layout === l ? ' bg-blue-100 text-blue-700 border-blue-300' : ' bg-white text-gray-600 border-gray-200 hover:bg-gray-50';
            layoutBar += '<button type="button" class="px-2 py-1 font-semibold rounded border' + active + '" onclick="window._dbSetLayout(\'' + l + '\')" title="' + l + '">' + labels[l] + '</button>';
        });
        layoutBar += '</div>';

        // ---- BUILDER SHELL (3-column: palette | canvas | drawer) ----
        html += '<div class="builder-shell grid grid-cols-1 lg:grid-cols-[320px_1fr_380px] gap-3 min-h-[640px]">';

        // ===== SOL: PALETTE =====
        html += '<aside class="builder-palette bg-white border border-gray-200 rounded-lg p-3 overflow-auto">';
        html += layoutBar;
        html += DB.contract.renderContract();
        html += '<div class="mb-2 mt-3 flex items-center justify-between">';
        html += '<h4 class="text-xs font-semibold text-gray-600 uppercase tracking-wide"><i class="fas fa-list mr-1"></i> Bilesenler (' + state.config.tabs[state.activeTab].components.length + ')</h4>';
        if (state.config.tabs[state.activeTab].components.length > 1) {
            html += '<span class="text-[10px] text-gray-400"><i class="fas fa-grip-vertical mr-1"></i>suruklenebilir</span>';
        }
        html += '</div>';
        html += DB.list.renderTabs();
        html += DB.list.renderComponentList();
        html += '</aside>';

        // ===== ORTA: CANVAS (alt-3a placeholder, alt-3b'de Gridstack ile dolacak) =====
        html += '<main class="builder-canvas">';
        html += '<div class="empty-hint">';
        html += '<div class="box">';
        html += '<div class="icon"><i class="fas fa-th-large"></i></div>';
        html += '<h3>Boş tuval</h3>';
        html += '<p>Bileşen yönetimi sol palette\'ten yapılır. Drag-resize canvas (Gridstack) F-7 alt-3b\'de aktifleşecek.</p>';
        html += '</div>';
        html += '</div>';
        html += '</main>';

        // ===== SAG: DRAWER (Veri | Gorunum tab strip + form) =====
        html += '<aside class="builder-drawer bg-white border border-gray-200 rounded-lg p-3 overflow-auto">';
        // Tab strip — F-8'de Gorunum tab\'i aktiflesecek
        html += '<div class="flex border-b border-gray-200 mb-3 -mx-3 px-3">';
        html += '<button type="button" class="px-3 py-2 text-xs font-bold uppercase text-blue-600 border-b-2 border-blue-600">Veri</button>';
        html += '<button type="button" class="px-3 py-2 text-xs font-bold uppercase text-gray-300 border-b-2 border-transparent cursor-not-allowed" title="F-8\'de aktif: variant picker + chart gallery + swatch/icon grid + span toggle">Gorunum</button>';
        html += '</div>';

        // Form banner (edit mode highlight)
        var formBg = state.editIndex >= 0 ? 'bg-blue-50 border-blue-200' : 'bg-gray-50 border-gray-200';
        html += '<div id="editingBanner" class="rounded-lg border p-3 transition-all ' + formBg + '">';
        if (state.editIndex >= 0) {
            var editingComp = state.config.tabs[state.activeTab].components[state.editIndex];
            var editingTitle = editingComp && editingComp.title ? editingComp.title : ('#' + (state.editIndex + 1));
            html += '<div class="flex items-center justify-between mb-2">';
            html += '<h4 class="text-sm font-bold text-blue-800"><i class="fas fa-pen mr-1"></i> Duzenleniyor: ' + esc(editingTitle) + '</h4>';
            html += '<button type="button" class="text-xs text-gray-500 hover:text-gray-700 underline" onclick="window._dbCancelEdit()">Iptal</button>';
            html += '</div>';
        } else {
            html += '<h4 class="text-sm font-bold text-gray-700 mb-2"><i class="fas fa-plus-circle mr-1 text-blue-600"></i> Yeni Bilesen</h4>';
        }
        html += DB.drawer.renderForm();
        html += '</div>';
        html += '</aside>';

        html += '</div>'; // end builder-shell

        // ---- Footer: JSON view toggle (debug + copy/paste) ----
        html += '<div class="mt-3 flex justify-end">';
        html += '<button type="button" id="dbJsonToggleBtn" class="text-xs text-gray-600 hover:text-gray-800 font-semibold" onclick="window._dbToggleJsonView()">' + (state.jsonViewOpen ? '<i class="fas fa-eye-slash mr-1"></i>JSON Gizle' : '<i class="fas fa-code mr-1"></i>JSON Goster') + '</button>';
        html += '</div>';
        html += '<div id="dbJsonView" class="mb-4' + (state.jsonViewOpen ? '' : ' hidden') + '">';
        html += '<textarea readonly class="w-full h-64 px-3 py-2 border border-gray-300 rounded-lg text-xs font-mono bg-gray-50" placeholder="Config JSON"></textarea>';
        html += '<p class="text-[10px] text-gray-500 mt-1">Salt-okunur snapshot. Degisiklikler UI\'dan yapilir.</p>';
        html += '</div>';

        builderEl.innerHTML = html;

        // Post-render hooks (each module attaches its listeners)
        DB.list.attachDragDrop();
        DB.drawer.attachListAttribute();
        sync();

        // JSON view refresh
        if (state.jsonViewOpen) {
            var ta = document.querySelector('#dbJsonView textarea');
            if (ta) ta.value = JSON.stringify(state.config, null, 2);
        }

        // Event bus: render completed
        document.dispatchEvent(new CustomEvent('db:rendered', { detail: { state: state } }));
    }

    // ---- Public API: window.DB namespace ----
    window.DB = {
        state: state,
        constants: constants,
        helpers: {
            sync: sync,
            val: val,
            esc: esc,
            genWidgetId: genWidgetId,
            colorSelect: colorSelect,
            iconSelect: iconSelect,
            shapeSelect: shapeSelect
        },
        builderEl: builderEl,
        configEl: configEl,
        requestRender: render,
        emit: function (eventName, detail) {
            document.dispatchEvent(new CustomEvent(eventName, { detail: detail }));
        }
    };

    // ---- Core globals ----
    window._dbSetLayout = function (l) { state.config.layout = l; render(); };
    window._dbToggleJsonView = function () {
        state.jsonViewOpen = !state.jsonViewOpen;
        render();
    };
})();
